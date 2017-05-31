using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LeveledScanMode
{
  ReferencePlane,
  Camber
}

// use this to store leveling information, positions are relative to the source object
[System.Serializable]
public class LevelingData
{
  public Vector3 refPlanePosition;
  public Vector3 refPlaneRot;
  public Vector3 refPlaneScale;

  public bool camberMode;
  public Vector3 camberPlanePos;
  public Vector3 camberPlaneRot;
  public Vector3 camberPlaneScale;
}

public class LeveledScan
{
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="sourceScanObject">Source scan object.</param>
	public LeveledScan (GameObject sourceScanObject) {
		sourceObject = sourceScanObject;
	}

	public LevelingData levelingData;

	public int scanId;
	public GameObject sourceObject;
	public LeveledScanMode mode;

	public Transform planeTransform;
	public Transform camberPlaneTransform;
	public Transform camberVolumeTransform;

	public Renderer[] renderers;
	public MeshFilter planeMeshFilter;
	public MeshFilter camberPlaneMeshFilter;
	public MeshFilter camberVolFilter; // object that renders to the Height Map layer and camera to be used by the camberVolumeMaterial

	public Material levelingPlaneMaterial;
	public Material camberPlaneMaterial;
	public Material camberVolumeMaterial;
	public GameObject edgeLinePrefab;

	public Material currentMaterial;
	public Material currentCamberVolMaterial;

	private Bounds currentCombinedBounds;
	private Vector3[] camberVertices = new Vector3[12];

  	public void SetMode (LeveledScanMode _mode) {
	
		mode = _mode;

		if (mode == LeveledScanMode.ReferencePlane) {
			ShowReferencePlane ();
			HideCamberPlane ();
		}
		else {
			ShowReferencePlane ();
			ShowCamberPlane ();
		}
 	}

  public void ShowReferencePlane()
  {
    if (planeTransform != null)
    {
      planeTransform.gameObject.SetActive (true);
    }
	else if (sourceObject != null) 
    {
      renderers = sourceObject.GetComponentsInChildren<Renderer> ();
      currentCombinedBounds = new Bounds();
      bool foundBounds = false;

      for (int i = 0; i < renderers.Length; i++) {

        Renderer r = renderers [i];
        if (r.enabled) {

          if (!foundBounds && r.bounds.size != Vector3.zero) {
            currentCombinedBounds = r.bounds;
            foundBounds = true;
          }
          else currentCombinedBounds.Encapsulate (r.bounds);
        }
      }

      // Create new plane GameObject.
      GameObject planeGo = new GameObject("Reference Plane " + sourceObject.name);
      planeTransform = planeGo.transform;

      // Position reference plane at center of scan content.
      planeTransform.position = currentCombinedBounds.center;
      planeTransform.localScale = new Vector3(currentCombinedBounds.size.x, 1, currentCombinedBounds.size.z);

      if (levelingData != null)
      {
        planeTransform.position = sourceObject.transform.position + levelingData.refPlanePosition;
        planeTransform.rotation = Quaternion.Euler(levelingData.refPlaneRot);
        planeTransform.localScale = levelingData.refPlaneScale;
      }
      else
      {
        levelingData = new LevelingData ();
        levelingData.refPlanePosition = planeTransform.position - sourceObject.transform.position;
        levelingData.refPlaneScale = planeTransform.localScale;

        levelingData.camberPlanePos = currentCombinedBounds.center - sourceObject.transform.position;
        levelingData.camberPlaneScale = new Vector3 (4.0f, 1.0f, 4.0f);
      }

      // Set material of plane renderer.
      MeshRenderer meshRenderer = planeGo.AddComponent<MeshRenderer> ();
      meshRenderer.material = levelingPlaneMaterial;

			// Disable shadows because transparency of material causes a lot of artifacting.
			meshRenderer.receiveShadows = false;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

      // let's create a 1x1 plane
      Vector3[] vertices = new Vector3[4];
      vertices [0] = new Vector3 (-0.5f, 0, -0.5f);
      vertices [1] = new Vector3 (-0.5f, 0, 0.5f);
      vertices [2] = new Vector3 (0.5f, 0, 0.5f);
      vertices [3] = new Vector3 (0.5f, 0, -0.5f);

      // Reference Plane.
      planeMeshFilter = planeGo.AddComponent<MeshFilter>();
      planeMeshFilter.mesh = new Mesh();
      planeMeshFilter.mesh.vertices = vertices;
      // add quad triangles clockwise and counterclockwise to get a double-faced plane
      // 1-----2
      // | \   |
      // |   \ |
      // 0-----3
      planeMeshFilter.mesh.triangles = new int[]
      {
        0, 1, 3,
        3, 1, 2,
        0, 3, 1, // both faces
        3, 2, 1
      };

      planeMeshFilter.mesh.uv = new Vector2[vertices.Length];
      planeMeshFilter.mesh.uv[0] = new Vector2(0, 0);
      planeMeshFilter.mesh.uv[1] = new Vector2(0, 1);
      planeMeshFilter.mesh.uv[2] = new Vector2(1, 1);
      planeMeshFilter.mesh.uv[3] = new Vector2(1, 0);

      // Create 3D border around plane. Border pieces are children of the reference plane.
      CreateEdgeLineBorder(planeMeshFilter, planeTransform, edgeLinePrefab);
    }
  }

	public void HideReferencePlane () {

		if (planeTransform != null) {
			planeTransform.gameObject.SetActive (false);	
		}
	}

  public void ShowCamberPlane () {
		
    if (camberVolFilter == null) {
			
      // Create new plane GameObject and cache transform.
      GameObject camberPlaneGo = new GameObject ("Camber Plane " + sourceObject.name);
      camberPlaneTransform = camberPlaneGo.transform;

      // Position camber plane at center of scan content.
      camberPlaneTransform.position = sourceObject.transform.position + levelingData.camberPlanePos;
      camberPlaneTransform.rotation = Quaternion.Euler(levelingData.camberPlaneRot);
      camberPlaneTransform.localScale = levelingData.camberPlaneScale;

      // Set material of plane renderer.
      MeshRenderer camberRenderer = camberPlaneGo.AddComponent<MeshRenderer> ();
      camberRenderer.material = camberPlaneMaterial;

			// Disable shadows because transparency of material causes a lot of artifacting.
			camberRenderer.receiveShadows = false;
			camberRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

      #region Mesh Generation
      // let's create a 1x1 plane
      Vector3[] vertices = new Vector3[4];
      vertices [0] = new Vector3 (-0.5f, 0, -0.5f);
      vertices [1] = new Vector3 (-0.5f, 0, 0.5f);
      vertices [2] = new Vector3 (0.5f, 0, 0.5f);
      vertices [3] = new Vector3 (0.5f, 0, -0.5f);

      camberPlaneMeshFilter = camberPlaneGo.AddComponent<MeshFilter> ();
      camberPlaneMeshFilter.mesh = new Mesh ();
      camberPlaneMeshFilter.mesh.vertices = vertices;
      // add quad triangles clockwise and counterclockwise to get a double-faced plane
      // 1-----2
      // | \   |
      // |   \ |
      // 0-----3
      camberPlaneMeshFilter.mesh.triangles = new int[] {
        0, 1, 3,
        3, 1, 2,
        0, 3, 1, // both faces
        3, 2, 1
      };

      camberPlaneMeshFilter.mesh.uv = new Vector2[vertices.Length];
      camberPlaneMeshFilter.mesh.uv [0] = new Vector2 (0, 0);
      camberPlaneMeshFilter.mesh.uv [1] = new Vector2 (0, 1);
      camberPlaneMeshFilter.mesh.uv [2] = new Vector2 (1, 1);
      camberPlaneMeshFilter.mesh.uv [3] = new Vector2 (1, 0);
      #endregion

      // Create 3D border around plane. Border pieces are children of the camber plane.
      CreateEdgeLineBorder (camberPlaneMeshFilter, camberPlaneTransform, edgeLinePrefab);

      CreateCamberVolume ();
    }
    else {
      camberPlaneTransform.gameObject.SetActive (true);
    }
  }

	public void HideCamberPlane () {
		
		if (camberPlaneTransform != null) {
			camberPlaneTransform.gameObject.SetActive (false);
		}
	}

  public void SetMaterial(Material mat)
  {
    	currentMaterial = new Material (mat);
   
		if (renderers != null) {
			for (int i = 0; i < renderers.Length; i++) {

				if (renderers [i] != null) {
					renderers [i].material = currentMaterial;
				}
			}
		}
  }

  public void UpdateCamberVolume()
  {
    camberVolFilter.transform.position = Vector3.zero;

    camberVertices [0] = camberPlaneMeshFilter.transform.TransformPoint (camberPlaneMeshFilter.mesh.vertices [0]);
    camberVertices [1] = camberPlaneMeshFilter.transform.TransformPoint (camberPlaneMeshFilter.mesh.vertices [1]);
    camberVertices [2] = camberPlaneMeshFilter.transform.TransformPoint (camberPlaneMeshFilter.mesh.vertices [2]);
    camberVertices [3] = camberPlaneMeshFilter.transform.TransformPoint (camberPlaneMeshFilter.mesh.vertices [3]);

    camberVertices [4] = planeMeshFilter.transform.TransformPoint (planeMeshFilter.mesh.vertices [0]);
    camberVertices [5] = planeMeshFilter.transform.TransformPoint (planeMeshFilter.mesh.vertices [1]);
    camberVertices [6] = planeMeshFilter.transform.TransformPoint (planeMeshFilter.mesh.vertices [2]);
    camberVertices [7] = planeMeshFilter.transform.TransformPoint (planeMeshFilter.mesh.vertices [3]);

    camberVolFilter.mesh.vertices = camberVertices;
  }

  public void Reset()
  {
    DestroyReferencePlane ();
    levelingData = null;
    ShowReferencePlane ();

    ResetCamberOnly ();
  }

	public void ResetCamberOnly () {
   
    	if (mode == LeveledScanMode.Camber) {
      		
			DestroyCamberPlane ();

      		levelingData.camberPlanePos = currentCombinedBounds.center - sourceObject.transform.position;
			levelingData.camberPlaneRot = Vector3.zero;
      		levelingData.camberPlaneScale = new Vector3 (4.0f, 1.0f, 4.0f);

      		ShowCamberPlane ();
    	}
  	}

  public void SaveLevelingData()
  {
		if (sourceObject == null)
			return;

    levelingData.camberMode = mode == LeveledScanMode.Camber;

    levelingData.refPlanePosition = planeTransform.position - sourceObject.transform.position;
    levelingData.refPlaneRot = planeTransform.eulerAngles;
    levelingData.refPlaneScale = planeTransform.localScale;

    if (levelingData.camberMode)
    {
      levelingData.camberPlanePos = camberPlaneTransform.position - sourceObject.transform.position;
      levelingData.camberPlaneRot = camberPlaneTransform.eulerAngles;
      levelingData.camberPlaneScale = camberPlaneTransform.localScale;
    }
  }
		
	public void RefreshPosition () {
		
		if (planeTransform != null) {
			planeTransform.position = sourceObject.transform.position + levelingData.refPlanePosition;
		}
	    
		if (camberPlaneTransform != null) {
	      camberPlaneTransform.position = sourceObject.transform.position + levelingData.camberPlanePos;
	    }
  	}

  /// <summary>
  /// Use it along with destroying the source object
  /// </summary>
  public void Destroy()
  {
    DestroyReferencePlane ();
    DestroyCamberPlane ();
  }

  void CreateCamberVolume()
  {
    Vector3[] vertices = new Vector3[8];

    // 1-----2
    // | \   |
    // |   \ |
    // 0-----3
    // this be the order

    // inner edges
    vertices [0] = new Vector3 (-0.5f, 0, -0.5f);
    vertices [1] = new Vector3 (-0.5f, 0, 0.5f);
    vertices [2] = new Vector3 (0.5f, 0, 0.5f);
    vertices [3] = new Vector3 (0.5f, 0, -0.5f);

    // outter edges
    vertices [4] = new Vector3 (-0.5f, 0, -0.5f);
    vertices [5] = new Vector3 (-0.5f, 0, 0.5f);
    vertices [6] = new Vector3 (0.5f, 0, 0.5f);
    vertices [7] = new Vector3 (0.5f, 0, -0.5f);

    int[] triangles = new int[]
    {
      // inner
      0, 1, 3,
      3, 1, 2,
      0, 3, 1, // both faces of the inner one
      3, 2, 1,

      // left side
      4, 1, 0,
      4, 5, 1,
      1, 5, 6,
      1, 6, 2,
      2, 6, 7,
      2, 7, 3,
      3, 7, 0,
      4, 0, 7
    };

    GameObject camberVolumeGo = new GameObject ("Camber Volume " + sourceObject.name);
    camberVolumeGo.layer = LevelingTool.HEIGHTMAP_LAYER_ID;
    camberVolumeTransform = camberVolumeGo.transform;

    camberVolFilter = camberVolumeGo.AddComponent<MeshFilter> ();
    camberVolFilter.mesh = new Mesh ();
    camberVolFilter.mesh.vertices = vertices;
    camberVolFilter.mesh.triangles = triangles;

    MeshRenderer meshRenderer = camberVolumeGo.AddComponent<MeshRenderer> ();
    currentCamberVolMaterial = new Material(camberVolumeMaterial);
    meshRenderer.material = currentCamberVolMaterial;

    camberVertices = vertices;
  }

  void DestroyReferencePlane () {

    if (planeMeshFilter != null) {
      GameObject.Destroy (planeMeshFilter.gameObject);
    }

    planeMeshFilter = null;
    planeTransform = null;
  }

  void DestroyCamberPlane () {

    if (camberPlaneMeshFilter != null) {
      GameObject.Destroy (camberPlaneMeshFilter.gameObject);
    }

    if (camberVolFilter != null) {
      GameObject.Destroy (camberVolFilter.gameObject);
    }

    camberPlaneMeshFilter = null;
    camberPlaneTransform = null;
    camberVolFilter = null;
  }

  public static void CreateEdgeLineBorder (MeshFilter meshFilter, Transform parent, GameObject edgePrefab) {

    if (edgePrefab != null) // Assumes use of 1x1x1 cube.
    {
      Vector3[] vertices = meshFilter.mesh.vertices;
      Vector3 prevVertex = vertices [vertices.Length - 1];

      for (int i = 0; i < vertices.Length; i++) {

        Vector3 vertex = vertices [i];

        GameObject edge = GameObject.Instantiate (edgePrefab);
        edge.transform.SetParent (parent, true);
        edge.transform.localPosition = Vector3.Lerp(vertex, prevVertex, 0.5f);
        edge.transform.rotation = Quaternion.LookRotation((prevVertex - vertex).normalized, Vector3.up);

        Vector3 scale = edge.transform.localScale;
        scale.z = Vector3.Distance (vertex, prevVertex) + scale.x;
        edge.transform.localScale = scale;

        prevVertex = vertex;
      }
    }
  }
}
