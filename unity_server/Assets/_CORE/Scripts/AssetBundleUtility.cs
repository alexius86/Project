using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using BestHTTP;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AssetBundleUtility : MonoBehaviour {
    public static string serverUri = "http://10.32.16.183:5001/v1/";

    public static int pingTimeoutSeconds = 5;
    public static bool enableDebugLogs = true;
    private static string username = "yang";
    private static string password = "su";

    public void BuildAllAssetBundles (string destinationPath) {

		#if UNITY_EDITOR
		Print("Building all AssetBundles");
		BuildPipeline.BuildAssetBundles(destinationPath, BuildAssetBundleOptions.None, BuildTarget.iOS);
		RefreshEditor();
		Print ("Build All AssetBundles Complete");
		#endif
	}

	public static void BuildAssetBundle (string bundleOutputDirectory, string bundleName, string[] assetPaths) {

    bundleOutputDirectory = bundleOutputDirectory.Trim (new char[] { '/' });

		#if UNITY_EDITOR
		Print ("Building AssetBundle " + bundleName);

    var stopwatch = new System.Diagnostics.Stopwatch();
    stopwatch.Start();
        RequestAuthentication(username, password);
        // Set asset bundle name for all assets in this bundle.
        for (int i = 0; i < assetPaths.Length; i++) {

			// Get the asset importer for file.
			Debug.Log("Checking file path: " + assetPaths[i]);
			AssetImporter assetImporter = AssetImporter.GetAtPath(assetPaths[i]);

			// Set AssetBundle name.
			assetImporter.assetBundleName = bundleName;

			// Update importer.
			assetImporter.SaveAndReimport();
		}

		// Create new AssetBundle build map for bundle.
		AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
		buildMap[0].assetBundleName = bundleName;
		buildMap[0].assetNames = assetPaths;

		// Make sure root folder for asset bundles exists.
    string bundlePath = Application.dataPath + "/" + bundleOutputDirectory;
		if (!Directory.Exists (bundlePath)) {
			
      Directory.CreateDirectory (bundlePath);
			RefreshEditor();
		}

		// Build the asset bundle.
		BuildPipeline.BuildAssetBundles(
			"Assets/" + bundleOutputDirectory, 
			buildMap, 
			BuildAssetBundleOptions.None, 
			BuildTarget.iOS
		);
        
        

        //open the file and send it through http request. 
        //parse the file name and if it's required to create a new site, slab, scan
        FileStream fs = new FileStream("Assets/" + bundleOutputDirectory + "/" + bundleName.ToLower(), FileMode.Open, FileAccess.Read);
        // parse filename.. into site, and slabs. and if sites don't exist, create sites, if slabs don't exist, create slabs. 

        HTTPRequest upload_scan_request = new HTTPRequest(new System.Uri(serverUri + "scan_upload/" + bundleName.ToLower()+ "/"), HTTPMethods.Post);

        // location, datetime, tolerance, grid spacing not avilable. other notes. 
        //postScreenshot.AddBinaryData ("screenshot", currentScreen.GetRawTextureData (), nameField.text + fileFormat);
        //upload_scan_request.AddField("file_name", bundleOutputDirectory);
        upload_scan_request.UploadStream = fs;
        upload_scan_request.Send();
        RefreshEditor();

    stopwatch.Stop();
    Debug.Log("Finished building asset bundle:  " + bundleName + "\nBuild time: " + stopwatch.Elapsed);
		#endif
	}


    #region User Login Authentication
    static void RequestAuthentication(string userName, string password)
    {   // xxx NOTE: Apiary doesn't support this yet 

        // Prepare request.
        HTTPRequest request = new HTTPRequest(new System.Uri(serverUri + "auth/"), HTTPMethods.Post, OnAuthenticationCompleted);
        request.ConnectTimeout = new System.TimeSpan(0, 0, pingTimeoutSeconds);
        request.AddField("username", userName);
        request.AddField("password", password);
        //		request.Credentials = new Credentials (userName, password);
        
        request.Send();
    }

    static void OnAuthenticationCompleted(HTTPRequest request, HTTPResponse response)
    {

        string message = "";
        if (response == null)
        {

            message = "Authentication for login request failed.\n" + "Unable to reach server. Please check your internet connection.";
            Debug.Log(message);
        }
        else if (response.IsSuccess)
        {

            Debug.Log(response.DataAsText);


        }
    }
    #endregion


    private static void RefreshEditor () {

		#if UNITY_EDITOR
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh ();
		#endif
	}

	private static void Print (string message) {

		if (enableDebugLogs) {
			Debug.Log (message);
		}
	}
}
