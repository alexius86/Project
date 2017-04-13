using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using BestHTTP;
using EasyEditor;

public class FbxUtility : MonoBehaviour {

	// How much data (Megabytes) is streamed in before we process that chunk?
	public static int fragmentSizeMB = 1;
	public static bool enableDebugLogs = true;

	public static int MegaByte { get { return 1024 * 1024; } }

	public static void CopyFromUrl (string url, string destinationPath, string fileName) {

		#if UNITY_EDITOR
		Print ("Requesting fbx model from url:\n" + url);

		// Make sure root folder for asset bundles exists.
		string path = Application.dataPath + destinationPath + fileName;
		if (!Directory.Exists (path)) {

			Print("Folder \"" + (Application.dataPath + destinationPath) + "\" doesn't exist. Creating new directory.");
			Directory.CreateDirectory (Application.dataPath + destinationPath);
			RefreshEditor();
		}

		var fbxRequest = new HTTPRequest(new Uri(url), (req, resp) =>
		{
			//TODO: Check HTTP status codes. Only run copy logic if we get 200:OK.

			List<byte[]> fragments = resp.GetStreamedFragments();

			if (fragments != null) {

				// Write out the downloaded data to a file:
				using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(path))) {
					foreach (byte[] data in fragments)
						bw.Write(data);
				}
			}
			else {

				Debug.LogWarning (
					"HTTPRequest streamed fragment data is null.\n" + 
					"This can happen at the end of the stream and shouldn't affect anything."
				);
			}

			if (resp.IsStreamingFinished) {

				Print("FBX file " + fileName + " successfully downloaded to:\n" + path);
				RefreshEditor();
			}
		});

		fbxRequest.OnProgress += (req, down, length) => Debug.Log(string.Format("Progress: {0:P2}", down / (float)length));
		fbxRequest.UseStreaming = true;
		fbxRequest.StreamFragmentSize = fragmentSizeMB * MegaByte;
		fbxRequest.DisableCache = true;	// Saving the file to disk, so we have no use for cache.
		fbxRequest.Send();
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
