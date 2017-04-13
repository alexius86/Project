using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AssetBundleUtility : MonoBehaviour {

	public static bool enableDebugLogs = true;

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

		RefreshEditor();

    stopwatch.Stop();
    Debug.Log("Finished building asset bundle:  " + bundleName + "\nBuild time: " + stopwatch.Elapsed);
		#endif
	}

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
