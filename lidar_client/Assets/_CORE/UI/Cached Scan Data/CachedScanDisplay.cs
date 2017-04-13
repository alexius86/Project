using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class CachedScanDisplay : DetailsPanelToggleItem {

	[SerializeField] private GameObject displayRoot;
	[SerializeField] private GameObject cachedScanItemPrefab;
	[SerializeField] private Transform itemContainer;

	private List<CachedScanItem> items = new List<CachedScanItem>();

	private SiteData site;
	private SlabData slab;
	private ScanData[] scans;

	void Awake () {

		Hide ();
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.menu_site_selected, SiteSelected);
		MessageDispatcher.AddListener (MessageDatabase.menu_slab_selected, SlabSelected);
		MessageDispatcher.AddListener (MessageDatabase.scan_list_received, GotScanList);
	
		MessageDispatcher.AddListener (MessageDatabase.scan_bundle_cached, ScanCached);
		MessageDispatcher.AddListener (MessageDatabase.cached_scan_deleted, CachedScanDeleted);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.menu_site_selected, SiteSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.menu_slab_selected, SlabSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.scan_list_received, GotScanList);

		MessageDispatcher.RemoveListener (MessageDatabase.scan_bundle_cached, ScanCached);
		MessageDispatcher.RemoveListener (MessageDatabase.cached_scan_deleted, CachedScanDeleted);
	}

	private void SiteSelected (IMessage message) {
		site = (SiteData)(message.Data);
	}

	private void SlabSelected (IMessage message) {
		slab = (SlabData)(message.Data);
	}

	private void GotScanList (IMessage message) {

		scans = (ScanData[])(message.Data);

		// Loop through scan list and check which ones are saved on disk.
		List<ScanData> cachedScans = new List<ScanData>();
		for (int i = 0; i < scans.Length; i++) {

			// Check for cached files.
			if (AssetBundleLoader.Instance.IsScanCached (site.site_id, slab.slab_id, scans [i].scan_id)) {
				cachedScans.Add (scans [i]);
			}
		}

		// Populate list with only those scans that were found on disk.
		Load (cachedScans);
	}

	private void ScanCached (IMessage message) {
		AddScan ((ScanData)(message.Data));
	}

	private void AddScan (ScanData scan) {

		// Create new list entry.
		GameObject newItem = GameObject.Instantiate (cachedScanItemPrefab) as GameObject;
		newItem.transform.SetParent (itemContainer, false);
		newItem.name = "Scan " + scan.scan_id + " [cached]";

		// Initialize entry with scan data.
		CachedScanItem itemScript = newItem.GetComponent<CachedScanItem> ();
		itemScript.Initialize (site.site_id, slab.slab_id, scan);

		// Add entry to internal list.
		items.Add (itemScript);
	}

	private void Load (List<ScanData> cachedScans) {

		// Clear out any existing list contents.
		if (items.Count > 0) {
			for (int i = 0; i < items.Count; i++) {
				Destroy (items [i].gameObject);
			}
		}
		items.Clear ();

		// Load the new data.
		for (int i = 0; i < cachedScans.Count; i++) {
			AddScan (cachedScans [i]);
		}
	}

	private void CachedScanDeleted (IMessage message) {

		ScanData scanData = (ScanData)(message.Data);

		CachedScanItem item = null;
		for (int i = 0; i < items.Count; i++) {

			item = items [i];
			if (item.ScanID == scanData.scan_id) {

				items.Remove (item);
				Destroy (item.gameObject);
				break;
			}
		}
	}

	public override void Show () {
		displayRoot.SetActive (true);
	}

	public override void Hide () {
		displayRoot.SetActive (false);
	}
}







