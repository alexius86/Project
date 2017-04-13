using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using EasyEditor;

public class Test : MonoBehaviour {

	[Header("FBX")]
	public string fbxModelUrl = "https://github.com/keijiro/NeoLowMan/raw/master/Assets/NeoLowMan/Neo.fbx";
	public string fbxDestination = "/FBX Files/";
	public string fbxSubFolder = "Site_A/Slab_01/";
	public string fbxFileName = "copy.fbx";

	[Header("AssetBundle")]
	public string assetBundleName = "test_bundle";
	public string assetBundleSubFolder = "Site_A/Slab_01/";

	[Inspector]
	public void CopyFBXFromURL () {

		FbxUtility.CopyFromUrl (fbxModelUrl, fbxDestination + fbxSubFolder, fbxFileName);
	}

	[Inspector]
	public void BuildAssetBundleContainingFBX () {

		string[] assetNames = new string[1];
		assetNames [0] = "Assets" + fbxDestination + fbxSubFolder + fbxFileName;
		AssetBundleUtility.BuildAssetBundle(assetBundleSubFolder, assetBundleName, assetNames);
	}
}
