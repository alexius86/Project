using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;
using UnityEngine.Networking;

using com.ootii.Messages;
using BestHTTP;

public class AssetBundleLoader : MonoBehaviour {

	[SerializeField] private List<string> assetBundleUrls;
	[Space(10.0f)]
	[SerializeField] private Transform contentOrigin;	// Position is where first asset in sequence will go. Rotation is applied to all assets in sequence.
	[SerializeField] private Vector3 contentEulerAngles = new Vector3(-90.0f, 0.0f, 0.0f);
	[Space(10.0f)]
	[SerializeField] private float spacing = 1.0f; 

	// Keep track of bundles that are loaded. If scan is toggled off we should unload the bundle it was in.
	private Dictionary<int, AssetBundle> loadedBundles = new Dictionary<int, AssetBundle> ();

	// The instantated instances of the data in asset bundles (the data that is shown to user). 
	private Dictionary<int, Transform> loadedContent = new Dictionary<int, Transform> ();

	// The combined bounds (all sub-mesh renderers) for each scan object.
	private List<Bounds> scanBoundsList = new List<Bounds>();

	// Keep track of request, data, IDs for an in-progress download so we can delete the incomplete cache file from disk.
	private HTTPRequest currentDownloadRequest = null;
	private int downloadingSiteID = -1;
	private int downloadingSlabID = -1;
	private ScanData downloadingScanData = null;

	private static AssetBundleLoader _instance = null;
	public static AssetBundleLoader Instance { get { return _instance; } }

	public int LoadedScanCount { get { return loadedContent.Count; } }

	void Awake () {

		if (_instance == null) {
			_instance = this;
		}
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.scan_load_cancelled, ScanDownloadCancelled);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.scan_load_cancelled, ScanDownloadCancelled);
	}

	void OnDrawGizmos () {

		if (scanBoundsList.Count == 0)
			return;

		Bounds combinedBounds = new Bounds ();
		int index = 0;
		foreach (KeyValuePair<int, Transform> kvp in loadedContent) {

			Renderer[] renderers = kvp.Value.GetComponentsInChildren<Renderer> ();
			Bounds b = new Bounds();
			for (int i = 0; i < renderers.Length; i++) {

				if (i == 0) {
					b = new Bounds (renderers [i].bounds.center, renderers [i].bounds.size);
				} 
				else {
					b.Encapsulate (renderers [i].bounds);
				}
			}

			Gizmos.color = Color.red;
			Gizmos.DrawWireCube (b.center, b.size);
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere (b.center, 0.25f);

			if (index == 0) {
				combinedBounds = new Bounds (b.center, b.size);
			} else {
				combinedBounds.Encapsulate (b);
			}

			index++;
		}

		Gizmos.color = Color.black;
		Gizmos.DrawWireSphere (combinedBounds.center, 0.25f);
		Gizmos.DrawWireCube (combinedBounds.center, combinedBounds.size);
	}

	#region Scan Load/Unload (Scene)
	/// <summary>
	/// Load a single scan's AssetBundle into memory and instantiate bundle contents in scene.
	/// </summary>
	public void LoadScan (int site_id, int slab_id, ScanData scan) {

		MessageDispatcher.SendMessage (this, MessageDatabase.load_scans, new ScanData[] { scan }, 0.0f);
		LoadScanBundle (site_id, slab_id, scan);
	}

	/// <summary>
	/// Unloads all scans. Useful for cleanup on scene change, etc.
	/// </summary>
	public void UnloadAllScans () {

		foreach (KeyValuePair<int, AssetBundle> kvp in loadedBundles) {

			//TODO: Message for each scan unload? Do other scripts care when ALL are unloaded at once?

			kvp.Value.Unload (true);
		}

		loadedBundles.Clear ();
		loadedContent.Clear ();
	}

	/// <summary>
	/// Unloads scan assets from the app and removes content in scene that came from AssetBundle.
	/// </summary>
	public void UnloadScan (int site_id, int slab_id, ScanData scanData) {

		int scan_id = scanData.scan_id;
		Debug.Log ("Scan unload: " + scan_id);

		// Unload the scan if it is actually loaded.
		if (loadedBundles.ContainsKey (scan_id)) {

			loadedBundles [scan_id].Unload (true);
			loadedBundles [scan_id] = null;

			loadedBundles.Remove (scan_id);

			Destroy(loadedContent[scan_id].gameObject);
			loadedContent [scan_id] = null;
			loadedContent.Remove (scan_id);	// Remove reference to transform from dictionary.

			// Refresh layout of objects in scene.
			OrganizeBundledDataInScene ();

			MessageDispatcher.SendMessage (this, MessageDatabase.scan_unloaded, scanData, 0.0f);
		}
	}
	#endregion

	/// <summary>
	/// Given a scan ID, attempt to find the loaded content with matching ID. If a match is found,
	/// return the position of the matched transform.
	/// </summary>
	public Vector3 WorldPositionForLoadedScan (int scanID) {

		foreach (KeyValuePair<int, Transform> kvp in loadedContent) {

			if (kvp.Key == scanID) {
				return kvp.Value.position;
			}
		}
		return Vector3.zero;
	}

	/// <summary>
	/// Scan download was cancelled via cancel button, returning to main menu, etc.
	/// </summary>
	private void ScanDownloadCancelled (IMessage message) {

		// Stop any http "threads".
		StopAllCoroutines ();

		// If an http request is in progress.
		if (currentDownloadRequest != null) {

			// Abort current download.
			currentDownloadRequest.Abort ();

			// Delete unfinished download from cache on disk.
			DeleteScanBundleFromDisk (downloadingSiteID, downloadingSlabID, downloadingScanData.scan_id);
		}

		// Reset download IDs.
		downloadingSiteID = -1;
		downloadingSlabID = -1;
		downloadingScanData = null;
		currentDownloadRequest = null;
	}

	/// <summary>
	/// Loads scan AssetBundle into scene. Gets AssetBundle from local disk if cached, 
	/// downloads from server otherwise.
	/// </summary>
	private void LoadScanBundle (int site_id, int slab_id, ScanData scan) {

		if (IsScanCached (site_id, slab_id, scan.scan_id)) {
			LoadScanBundleFromDisk(site_id, slab_id, scan);
		} 
		else {
			DownloadScanBundleBestHTTP (site_id, slab_id, scan);
		}
	}

	#region File I/O
	public bool IsScanCached (int site_id, int slab_id, int scan_id) {

		bool cached = false;

		// Get folder file is in.
		string path = Application.persistentDataPath + "/Cached Data/" + site_id + "/" + slab_id + "/" + scan_id + "/";

		if (Directory.Exists (path)) {

			string[] files = Directory.GetFiles (path);
			if (files.Length > 0) {
				cached = true;
			}
		}
		return cached;
	}

	// AssetBundle cache read.
	public void LoadScanBundleFromDisk (int site_id, int slab_id, ScanData scan) {

		Debug.Log ("Loading asset bundle from disk.");

		// Get folder file is in.
		string path = Application.persistentDataPath + "/Cached Data/" + site_id + "/" + slab_id + "/" + scan.scan_id + "/";

		if (Directory.Exists (path)) {

			string[] files = Directory.GetFiles (path);
			Debug.Log ("Files found at " + path + ": " + files.Length);

			//Note: There should only ever be one file (the asset bundle) at each site/slab/scan/ directory.
			for (int i = 0; i < files.Length; i++) {

				Debug.Log ("Loading local asset bundle at: " + files [i]);

				// Load asset bundle.
				AssetBundle bundle = AssetBundle.LoadFromFile (files [i]);
				loadedBundles.Add (scan.scan_id, bundle);

				// Load contents.
				LoadScanBundleContents (bundle, scan);
			}
		} 
		else {
			Debug.Log ("Directory not found: " + path);
		}
	}

	public void DeleteScanBundleFromDisk (int site_id, int slab_id, int scan_id) {

		// Get folder file is in.
		string path = Application.persistentDataPath + "/Cached Data/" + site_id + "/" + slab_id + "/" + scan_id + "/";

		if (Directory.Exists (path)) {

			string[] files = Directory.GetFiles (path);

			if (files.Length == 0) {
				Debug.LogWarning ("Couldn't delete cached scan at path \"" + path + "\". Reason: No files found - empty folder.");
			} 
			else {	//Note: There should only ever be one file (the asset bundle) at each site/slab/scan/ directory.

				for (int i = 0; i < files.Length; i++) {
					File.Delete (files [i]);
				}
			}
		} 
		else {
			Debug.LogWarning ("Couldn't delete cached scan at path \"" + path + "\". Reason: Directory not found.");
		}
	}

	private byte[] ObjectToByteArray (object obj) {

		if (obj == null) return null;

		BinaryFormatter bf = new BinaryFormatter();
		using (MemoryStream ms = new MemoryStream()) {

			bf.Serialize(ms, obj);
			return ms.ToArray();
		}
	}
	#endregion

	private void LoadScanBundleContents (AssetBundle bundle, ScanData scan) {

		Debug.Log ("Loading asset bundle contents for scan " + scan.scan_id);

		// Get all objects in the bundle into memory (not instantiated in scene yet).
		Object[] bundleObjects = bundle.LoadAllAssets ();	// 0:GameObject, 1:Mesh, 2:Avatar

		// Instantiate new GameObject using only the first object from bundle object array (the Object that references main GameObject of asset).
		GameObject bundleObject = GameObject.Instantiate (bundleObjects [0], transform) as GameObject;

		// Get Transform so we can position and orient it.
		Transform bundleObjectTransform = bundleObject.transform;
		bundleObjectTransform.position = contentOrigin.position;
		bundleObjectTransform.eulerAngles = contentEulerAngles;

		// Keep track of instantiated bundle content and layout content in scene.
		loadedContent.Add (scan.scan_id, bundleObjectTransform);
		OrganizeBundledDataInScene ();

		Scan levelingScan = new Scan ();
		levelingScan.scanData = scan;
		levelingScan.loadedObjectTransform = bundleObjectTransform;

		MessageDispatcher.SendMessage (this, MessageDatabase.scans_loaded, levelingScan, 0.0f);
	}
		
	private void DownloadScanBundleBestHTTP (int site_id, int slab_id, ScanData scan) {

		downloadingSiteID = site_id;
		downloadingSlabID = slab_id;
		downloadingScanData = scan;

		// Get location on disk.
		string directory = Application.persistentDataPath + "/Cached Data/" + site_id + "/" + slab_id + "/" + scan.scan_id + "/";
		string fullPath = directory + "scan.assetbundle";

		// Make sure scan data written isn't backed up to iCloud.
		#if UNITY_IOS
		UnityEngine.iOS.Device.SetNoBackupFlag(fullPath);
		#endif

		// We only want to append to assetbundle file while downloading streamed file fragments.
		// Reset file on redownload.
		if (File.Exists (fullPath)) {	
			File.Delete (fullPath);
		}
		else if (!Directory.Exists (directory)) {	// Make sure the path to file exists.
			Directory.CreateDirectory (directory);
		}

		Debug.Log ("Downloading asset bundle from: " + scan.url + "\nSaving local copy to: " + fullPath);

		currentDownloadRequest = new HTTPRequest(new System.Uri(scan.url), (req, resp) =>
			{
				if (resp == null) {
					Debug.LogWarning("Current download request's response is null. This is OK if download was just cancelled.");
					return;
				}

				List<byte[]> fragments = resp.GetStreamedFragments();

				if (fragments == null) {
					Debug.LogWarning("Streamed data from download is null. This is OK if user left app.");
					return;
				}

				// Write out the downloaded data to a file (cache).
				using (FileStream fs = new FileStream(fullPath, FileMode.Append)) {

					foreach(byte[] data in fragments) {
						fs.Write(data, 0, data.Length);
					}
				}

				if (req.State == HTTPRequestStates.Finished) {

					if (resp.IsStreamingFinished && req.Response.IsSuccess) {

						Debug.Log("Download completed successfully!");

						//NOTE: If handler.assetBundle is an AssetBundle that is already loaded, a null reference exception will occur.
						//		This shouldn't ever happen in production, however, since AssetBundle content for each scan will always be unique.

						// Let scan cache UI know there's a new cached item to put in its list.
						MessageDispatcher.SendMessage(this, MessageDatabase.scan_bundle_cached, scan, 0.0f);

						// Read from newly cached file and load the AssetBundle content.
						LoadScanBundleFromDisk(site_id, slab_id, scan);

						// Reset download IDs.
						downloadingSiteID = -1;
						downloadingSlabID = -1;
						downloadingScanData = null;
						currentDownloadRequest = null;
					}
				}
				else if (req.State == HTTPRequestStates.Error) {

					Debug.LogWarning("Request Finished with Error! " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));

					// Reset download IDs.
					downloadingSiteID = -1;
					downloadingSlabID = -1;
					downloadingScanData = null;
					currentDownloadRequest = null;
				}
			});
		currentDownloadRequest.OnProgress = OnScanDownloadProgress;
		currentDownloadRequest.UseStreaming = true;
		currentDownloadRequest.StreamFragmentSize = 1 * 1024 * 1024; // 1 megabyte
		currentDownloadRequest.DisableCache = true; // already saving to a file, so turn off caching
		currentDownloadRequest.Send();
	}

	private void OnScanDownloadProgress (HTTPRequest request, int downloaded, int length) {

		float progressPercent = (downloaded / (float)length) * 100.0f;
		MessageDispatcher.SendMessage (this, MessageDatabase.scans_loading, (float)downloaded / (float)length, 0.0f);
	}

	// Take the main transform from each loaded AssetBundle and display according to how many bundles are loaded, up to maximum of 4.
	// Placing them all in a row for now.
	//TODO: Bunch objects intelligently? (box of 4, pyramid of 3, etc).
	private void OrganizeBundledDataInScene () {

		int placedCount = 0;

		Vector3 prevPos = contentOrigin.position;	// Center point of the previously placed object.
		Vector3 prevExtents = Vector3.one;			// Bounds extents (half size) of the previously placed object.

		scanBoundsList = new List<Bounds> ();	

		foreach (KeyValuePair<int, Transform> kvp in loadedContent) {

			Transform t = kvp.Value;
			Bounds bounds = new Bounds(t.position, Vector3.one);

			// Calculate bounding box for current scan object.
			Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++) {
				bounds.Encapsulate (renderers[i].bounds);
			}
			scanBoundsList.Add (bounds);

			// Position current scan object by taking into account number of scan objects being loaded and
			// the width of the current scan object.
			t.position = prevPos + (Vector3.forward * spacing);

			// Place right at origin if first object we're placing.
			// Otherwise, move current object away from previous position based on width of previously placed object.
			if (placedCount > 0) {

				t.position += (Vector3.right * prevExtents.x);		// Move over by half the width of previously placed object.
				t.position += (Vector3.right * bounds.extents.x);	// Move over by half the height of current object.
			}

			prevPos = t.position;
			prevExtents = bounds.extents;

			placedCount++;
		} 

		Bounds combinedBounds;

		// Bounds is not a nullable type. If there are no more scan objects left in scene, we will pass a Bounds struct with size zero.
		if (loadedContent.Count == 0) {
			combinedBounds = new Bounds (Vector3.zero, Vector3.zero);
		} else {
			combinedBounds = CalculateCombinedBounds (loadedContent.Values.ToList ());
		}

		// Perhaps the camera would like to know when layout changes so it can change its view properties?
		MessageDispatcher.SendMessage (this, MessageDatabase.loaded_scans_refreshed, combinedBounds, 0.0f);
	}

	private Bounds CalculateCombinedBounds (List<Transform> contentList) {

		Bounds combinedBounds = new Bounds ();
		int index = 0;
		foreach (Transform t in contentList) {

			Renderer[] renderers = t.GetComponentsInChildren<Renderer> ();
			Bounds b = new Bounds();
			for (int i = 0; i < renderers.Length; i++) {

				if (i == 0) {
					b = new Bounds (renderers [i].bounds.center, renderers [i].bounds.size);
				} 
				else {
					b.Encapsulate (renderers [i].bounds);
				}
			}

			if (index == 0) {
				combinedBounds = new Bounds (b.center, b.size);
			} else {
				combinedBounds.Encapsulate (b);
			}
			index++;
		}
		return combinedBounds;
	}
}














