using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class ScanSelector : MonoBehaviour {

	[SerializeField] private Transform root;
	[SerializeField] private GameObject scanItemPrefab;

	private int site_id;
	private int slab_id;

	private List<ScanSelectorItem> scanItems;

	private bool scanLoadInProgress = false;
	private ScanData[] scanDataBeingLoaded;	// Save scans being loaded in case we need to cancel them.

	void OnEnable () {

		// Get site ID and slab ID.
		MessageDispatcher.AddListener(MessageDatabase.menu_site_selected, SiteSelected);
		MessageDispatcher.AddListener (MessageDatabase.menu_slab_selected, SlabSelected);

		// Get list of scans for current site and slab.
		MessageDispatcher.AddListener (MessageDatabase.scan_list_received, ScanListLoaded);

		// Scan selection (Menu vs Viewer).
		MessageDispatcher.AddListener (MessageDatabase.menu_scan_selected, MenuScanSelected);
		MessageDispatcher.AddListener (MessageDatabase.viewer_scan_selected, ViewerScanSelected);

		// Scan bundle loading.
		MessageDispatcher.AddListener (MessageDatabase.load_scans, LoadingStarted);
		MessageDispatcher.AddListener (MessageDatabase.scans_loaded, LoadingComplete);
		MessageDispatcher.AddListener (MessageDatabase.scan_load_cancelled, LoadingCancelled);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener(MessageDatabase.menu_site_selected, SiteSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.menu_slab_selected, SlabSelected);

		MessageDispatcher.RemoveListener (MessageDatabase.scan_list_received, ScanListLoaded);
		MessageDispatcher.RemoveListener (MessageDatabase.menu_scan_selected, MenuScanSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.viewer_scan_selected, ViewerScanSelected);

		MessageDispatcher.RemoveListener (MessageDatabase.load_scans, LoadingStarted);
		MessageDispatcher.RemoveListener (MessageDatabase.scans_loaded, LoadingComplete);
		MessageDispatcher.RemoveListener (MessageDatabase.scan_load_cancelled, LoadingCancelled);
	}

	public void Initialize (ScanData[] scans) {

		// Clear out any old scan items. TODO: Pooling.
		foreach (Transform t in root) {
			Destroy (t.gameObject);
		}

		scanItems = new List<ScanSelectorItem> ();

		for (int i = 0; i < scans.Length; i++) {

			GameObject newItem = GameObject.Instantiate (scanItemPrefab) as GameObject;
			newItem.transform.SetParent (root, false);

			ScanData scanData = scans [i];
			ScanSelectorItem scanItem = newItem.GetComponent<ScanSelectorItem> ();
			scanItem.Initialize (site_id, slab_id, scanData);

			scanItems.Add (scanItem);
		}
	}

	public void Show () {
		root.gameObject.SetActive (true);
	}

	public void Hide () {
		root.gameObject.SetActive (false);
	}

	private void SiteSelected (IMessage message) {

		SiteData site = (SiteData)(message.Data);
		site_id = site.site_id;
	}

	private void SlabSelected (IMessage message) {

		SlabData slab = (SlabData)(message.Data);
		slab_id = slab.slab_id;
	}

	private void LoadingStarted (IMessage message) {

		scanDataBeingLoaded = (ScanData[])(message.Data);

		Debug.Log ("Scan load start. Scan selector disabled.");
		scanLoadInProgress = true;
	}

	private void LoadingComplete (IMessage message) {
		Debug.Log ("Scan load complete. Scan selector enabled.");
		scanLoadInProgress = false;
	}

	private void LoadingCancelled (IMessage message) {

		for (int i = 0; i < scanDataBeingLoaded.Length; i++) {
			for (int j = 0; j < scanItems.Count; j++) {

				if (scanDataBeingLoaded [i].scan_id == scanItems [j].ScanID) {
					scanItems [j].ToggleSelectionMarkerOnly ();
				}
			}
		}

		Debug.Log ("Scan load cancelled. Scan selector enabled.");
		scanLoadInProgress = false;
	}

	private void ScanListLoaded (IMessage message) {

		ScanData[] scans = (ScanData[])(message.Data);
		Initialize (scans);
	}

	// Scan was selected from list in main menu UI (site selection).
	private void MenuScanSelected (IMessage message) {

		ScanData scan = (ScanData)(message.Data);

		// Find index of selected scan and toggle it ON.
		for (int i = 0; i < scanItems.Count; i++) {

			// Triggers AssetBundle load (if not cached on disk, will download from server).
			if (scanItems [i].ScanID == scan.scan_id) {
				scanItems [i].Toggle ();
			}
		}
	}

	// Scan was selected from list in viewer scene.
	private void ViewerScanSelected (IMessage message) {

		if (scanLoadInProgress) {
			return;
		}

		ScanData scan = (ScanData)(message.Data);

		// Find index of selected scan and toggle it ON.
		for (int i = 0; i < scanItems.Count; i++) {

			// Triggers AssetBundle load (if not cached on disk, will download from server).
			if (scanItems [i].ScanID == scan.scan_id) {
				scanItems [i].Toggle ();
			}
		}
	}
}






