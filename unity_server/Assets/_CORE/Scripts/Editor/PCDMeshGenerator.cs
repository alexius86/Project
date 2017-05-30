using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu]
public class PCDMeshGenerator : ScriptableObject
{
  public static readonly List<PCDMeshGenerator> Generators = new List<PCDMeshGenerator> ();
  public static readonly List<string> SupportedExtensions = new List<string>()
  {
    ".off",
    ".pcd"
  };

  public string WatchDirectory = "PCD";
  public string OutputDirectory = "PCD Imported";
  public string AssetBundleDirectory = "AssetBundles";
  public Material DefaultMaterial;
  public float Scale = 1;
  public bool InvertYZ = false;
  public bool DontBundle = false;

  //Point Cloud
  private GameObject pcdObj;

  private int numPoints;
  private int numPointGroups;
  private Vector3[] points;
  private Color[] colors;
  private Vector3 minValue;
  private int pointsLimit = 65000;
  private string datatype; //ascii or binary
  private float progress = 0;

  static Queue<string> conversionQueue = new Queue<string>();

  void OnEnable()
  {
    Generators.Add(this);

    EnsureDirectory (WatchDirectory);
  }

  void OnDisable()
  {
    Generators.Remove(this);
  }

  public void Update()
  {
    while (conversionQueue.Count > 0)
    {
      PCDToAsset (conversionQueue.Dequeue(), OutputDirectory);
    }
  }

  /// <summary>
  /// Enqueues PCDToAsset conversion method. For use from a AssetProcessor.
  /// Use PCDToAsset if you want immediate conversion
  /// </summary>
  /// <returns><c>true</c>, if PCD to asset was enqueued, <c>false</c> otherwise.</returns>
  /// <param name="assetPath">Path relative to Assets/</param>
  public bool EnqueuePCDToAsset(string assetPath)
  {
    string watchDirectory = WatchDirectory.Trim (new char[] { '/' });
    watchDirectory += "/";

    if (assetPath.StartsWith (watchDirectory))
    {
      conversionQueue.Enqueue (assetPath);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Converts PCD file formats to assets based on generator settings
  /// Paths relative to Assets/
  /// </summary>
  public void PCDToAsset(string sourceFilePath, string outputDirectory, string fileName = null)
  {
    // trim slashes
    sourceFilePath = Application.dataPath + "/" + sourceFilePath.TrimStart(new char[] { '/' });
    outputDirectory = outputDirectory.Trim (new char[] { '/' });
    fileName = fileName ?? Path.GetFileNameWithoutExtension (sourceFilePath);
    string extension = Path.GetExtension (sourceFilePath);
    string absoluteOutputDir = Application.dataPath + "/" + outputDirectory + "/" + fileName;

    if (!File.Exists (sourceFilePath))
    {
      Debug.LogWarning ("No file at given path - " + sourceFilePath);
      return;
    }

    // setup directories
    if (!Directory.Exists (absoluteOutputDir))
    {
      EnsureDirectory(outputDirectory + "/" + fileName);
    }
    else
    {
      // we cleanup the destination folder if found
      string[] files = Directory.GetFiles (absoluteOutputDir);

      foreach (string file in files)
      {
        string filePath = "Assets/" + outputDirectory + "/" + fileName + "/" + Path.GetFileNameWithoutExtension(file);
        AssetDatabase.DeleteAsset (filePath);
      }
    }

    EnsureDirectory(AssetBundleDirectory);

    string prefabPath = "";

    if (extension.ToLower () == ".off")
    {
      prefabPath = CreateOFF (sourceFilePath, outputDirectory, fileName);
    }
    else if (extension.ToLower () == ".pcd")
    {
      prefabPath = CreatePCD (sourceFilePath, outputDirectory, fileName);
    }
    else
    {
      Debug.LogWarning("File type not supported - " + sourceFilePath);
    }

    if (!string.IsNullOrEmpty(prefabPath) && !DontBundle)
    {
      //Create asset bundle
      AssetBundleUtility.BuildAssetBundle (AssetBundleDirectory, fileName, new string[] { prefabPath });
    }
  }

  Mesh CreateMesh(int id, int nPoints, int pointsLimit)
  {
    Mesh mesh = new Mesh();
    Vector3[] myPoints = new Vector3[nPoints];
    int[] myIndexes = new int[nPoints];
    Color[] myColors = new Color[nPoints];

    for(int i=0; i<nPoints; ++i)
    {
      myPoints[i] = points[id * pointsLimit + i] - minValue;
      myIndexes[i] = i;
      myColors[i] = colors[id * pointsLimit+ i];
    }

    mesh.vertices = myPoints;
    mesh.colors = myColors;
    mesh.SetIndices(myIndexes, MeshTopology.Points, 0);
    mesh.uv = new Vector2[nPoints];
    mesh.normals = new Vector3[nPoints];
    return mesh;
  }

  void InstantiateMesh(int meshIndex, int nPoints, string outputDirectory, string fileName)
  {
    //Create Mesh
    GameObject pointGroup = new GameObject(fileName + meshIndex);
    pointGroup.AddComponent<MeshFilter>();
    pointGroup.AddComponent<MeshRenderer>();
    pointGroup.GetComponent<Renderer>().material = DefaultMaterial;

    pointGroup.GetComponent<MeshFilter> ().mesh = CreateMesh (meshIndex, nPoints, pointsLimit);
    pointGroup.transform.parent = pcdObj.transform;

    // Store Mesh
    string meshPath = "Assets/" + outputDirectory + "/" + fileName + "/" + fileName + meshIndex + ".asset";
    UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter> ().sharedMesh, meshPath);
    UnityEditor.AssetDatabase.SaveAssets ();
  }

  string CreateOFF(string sourcePath, string outputDirectory, string fileName = null)
  {
    var stopwatch = new System.Diagnostics.Stopwatch();
    stopwatch.Start ();

    fileName = fileName ?? Path.GetFileNameWithoutExtension (sourcePath);
    //Read file
    StreamReader sr = new StreamReader(sourcePath);
    sr.ReadLine(); //skip the first line
    //Debug.Log("Readline :\n" + sr.ReadLine());

    string[] buffer = sr.ReadLine().Split(); //numVertices numFaces numEdges
    numPoints = int.Parse(buffer[0]);
//    Debug.Log(fileName + " numPoints: " + numPoints);
    points = new Vector3[numPoints];
    colors = new Color[numPoints];
    minValue = new Vector3();

    for(int i = 0; i<numPoints; i++)
    {
      buffer = sr.ReadLine().Split();
      //TODO: invertYZ
      if (!InvertYZ)
      {
        points[i] = new Vector3 (float.Parse (buffer[0])*Scale, float.Parse (buffer[1])*Scale,float.Parse (buffer[2])*Scale) ;
      }
      else
        points[i] = new Vector3 (float.Parse (buffer[0])*Scale, float.Parse (buffer[2])*Scale,float.Parse (buffer[1])*Scale) ;

      if (buffer.Length >= 6)
        //ignoring alpha value
        colors[i] = new Color (int.Parse (buffer[3])/255.0f,int.Parse (buffer[4])/255.0f,int.Parse (buffer[5])/255.0f);
      else
        colors[i] = Color.cyan;

//      if (i == 0)
//      {
//        Debug.Log("color[0]: " + colors[0]);
//      }

      //GUI
      progress = i * 1.0f / (numPoints - 1) * 1.0f;

      //show every 5% is done            
      if (i%Mathf.FloorToInt(numPoints/20) == 0){
        string progressInfo = i.ToString() + " out of " + numPoints.ToString() + " loaded";
        EditorUtility.DisplayProgressBar ("OFF Import - " + fileName, progressInfo, progress);
      }

    }

    //Instantiate Points Groups
    numPointGroups = Mathf.CeilToInt(numPoints * 1.0f / pointsLimit * 1.0f);

    pcdObj = new GameObject(fileName);

    for(int i =0; i < numPointGroups - 1; i++)
    {
      InstantiateMesh(i, pointsLimit, outputDirectory, fileName);

      if (i%Mathf.FloorToInt(numPointGroups/10) == 0){
        string loadingInfo = i.ToString() + " out of " + numPointGroups.ToString() + " point groups loaded";
        EditorUtility.DisplayProgressBar ("PCD Import - " + fileName, loadingInfo, progress);
      }

    }
    //the last mesh is with different point limit
    InstantiateMesh (numPointGroups-1, numPoints- (numPointGroups-1) * pointsLimit, outputDirectory, fileName);

    //Store pcd to prefab
    string prefabPath = "Assets/" + outputDirectory + "/" + fileName + "/" + fileName + ".prefab";
    PrefabUtility.CreatePrefab (prefabPath, pcdObj);

    //Delete object from scene
    GameObject.DestroyImmediate(pcdObj);

    EditorUtility.ClearProgressBar ();

    stopwatch.Stop ();
    Debug.Log("Processed " + sourcePath + "\n Number of points: " + numPoints + ", Processing time: " + stopwatch.Elapsed.ToString());

    return prefabPath;
  }

  string CreatePCD(string sourcePath, string outputDirectory, string fileName = null)
  {
    var stopwatch = new System.Diagnostics.Stopwatch();
    stopwatch.Start ();

    fileName = fileName ?? Path.GetFileNameWithoutExtension (sourcePath);

    //Read file
    StreamReader sr = new StreamReader(sourcePath);
    sr.ReadLine(); //skip the first line
    //Debug.Log("Readline :\n" + sr.ReadLine());
    string[] buffer = sr.ReadLine().Split(); //numVertices numFaces numEdges
    //if(buffer[0] == "POINTS")

    //This function will loop through the first 11 lines of the file to find POINTS and DATA type
    //Assume POINTS and DATA are the last 2 lines before the points x, y, z
    for(int i=0; i<11; ++i)
    {
      buffer = sr.ReadLine().Split(); //numVertices numFaces numEdges
//      Debug.Log(buffer[0]);
      if (buffer[0] == "POINTS")
      {
//        Debug.Log("FOUND POINTS: " + buffer[1]);
        numPoints = int.Parse(buffer[1]);
        buffer = sr.ReadLine().Split();
        if (buffer[0] == "DATA")
        {
//          Debug.Log("FOUND DATA: " + buffer[1]);
          datatype = buffer[1];
        }
        break;
      }
    }

    points = new Vector3[numPoints];
    colors = new Color[numPoints];
    minValue = new Vector3();
    for (int i = 0; i < numPoints; i++)
    {
      string line = sr.ReadLine();
      if (line == null || line == "")
      {
//        Debug.Log("Found null line " + (i + 12));
        points[i] = new Vector3(float.Parse("0") * Scale, float.Parse("0") * Scale, float.Parse("0") * Scale);
        colors[i] = Color.cyan;
      }
      else
      {
        //buffer = sr.ReadLine().Split();
        buffer = line.Split();
        if (buffer[0] == "")
        {
          //Debug.Log("Found null buffer " + (i + 12));
          points[i] = new Vector3(float.Parse("0") * Scale, float.Parse("0") * Scale, float.Parse("0") * Scale);
          colors[i] = Color.cyan;
        }
        else if (buffer[0] == "0" && buffer[1] == "0" && buffer[2] == "0")
        {

          //Debug.Log("Found all 0s line " + (i + 12));
          points[i] = new Vector3(float.Parse("0") * Scale, float.Parse("0") * Scale, float.Parse("0") * Scale);
          colors[i] = Color.cyan;
        }
        else
        {
          if (!InvertYZ)
          {
            points[i] = new Vector3(float.Parse(buffer[0]) * Scale, float.Parse(buffer[1]) * Scale, float.Parse(buffer[2]) * Scale);
          }
          else
            points[i] = new Vector3(float.Parse(buffer[0]) * Scale, float.Parse(buffer[2]) * Scale, float.Parse(buffer[1]) * Scale);

          //if (buffer.Length >= 6)
          //             //ignoring alpha value
          //  colors[i] = new Color (int.Parse (buffer[3])/255.0f,int.Parse (buffer[4])/255.0f,int.Parse (buffer[5])/255.0f);
          //else
          colors[i] = Color.cyan;

        }
      }

//      if (i == 0)
//      {
//        Debug.Log("color[0]: " + colors[0]);
//
//      }
      //            Debug.Log("start progress check");
      //GUI
      progress = i * 1.0f / (numPoints - 1) * 1.0f;
      //            Debug.Log("Progress: " + progress);
      //            Debug.Log("i: " + i);

      //show every 5% is done            
      if (i % Mathf.FloorToInt(numPoints / 20) == 0)
      {
        string progressInfo = i.ToString() + " out of " + numPoints.ToString() + " loaded";
        EditorUtility.DisplayProgressBar ("PCD Import - " + fileName, progressInfo, progress);
      }

      //Debug.Log("Done Processing line: " + (i + 12));
      //yield return null;

    }
//    Debug.Log("Initiate Points groups");
    //Instantiate Points Groups
    numPointGroups = Mathf.CeilToInt(numPoints * 1.0f / pointsLimit * 1.0f);
//    Debug.Log("numPointGroups: " + numPointGroups);
    pcdObj = new GameObject(fileName);

    for(int i =0; i < numPointGroups - 1; i++)
    {
      InstantiateMesh(i, pointsLimit, outputDirectory, fileName);

      int temp = Mathf.FloorToInt(numPointGroups / 10);
      //check divide by zero error 
      if (temp !=0 && i%Mathf.FloorToInt(numPointGroups/10) == 0){
        string progressInfo = i.ToString() + " out of " + numPointGroups.ToString() + " point groups loaded";

        EditorUtility.DisplayProgressBar ("PCD Import - " + fileName, progressInfo, progress);
      }

    }
    //the last mesh is with different point limit
    InstantiateMesh (numPointGroups-1, numPoints- (numPointGroups-1) * pointsLimit, outputDirectory, fileName);

    //Store pcd to prefab
    string prefabPath = "Assets/" + outputDirectory + "/" + fileName + "/" + fileName + ".prefab";
    PrefabUtility.CreatePrefab (prefabPath, pcdObj);

    //Delete object from scene
    GameObject.DestroyImmediate(pcdObj);

    EditorUtility.ClearProgressBar ();

    stopwatch.Stop ();
    Debug.Log("Processed " + sourcePath + "\n Number of points: " + numPoints + ", Processing time: " + stopwatch.Elapsed);

    return prefabPath;
  }

  void EnsureDirectory(string relativePath)
  {
    // Using asset database methods to create directories 
    string[] directoryTree = relativePath.Trim(new char[] { '/' }).Split ('/');
    string currentPath = "Assets";
    string projectRoot = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"));

    for (int i = 0; i < directoryTree.Length; i++)
    {
      string dirName = directoryTree [i];
      if (!Directory.Exists (projectRoot + "/" + currentPath + "/" + dirName))
      {
        AssetDatabase.CreateFolder (currentPath, dirName);
      }

      currentPath += "/" + dirName;
    }

    //    string directory = Application.dataPath + "/" + relativePath.Trim (new char[] { '/' });
    //    if (!Directory.Exists (directory))
    //    {
    //      Directory.CreateDirectory (directory);
    //    }

  }
}
