using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;
using TMPro;

public class ScanSelectorItem : MonoBehaviour {

	[SerializeField] private TextMeshProUGUI scanIDLabel;
	[SerializeField] private Image selectedMarker;

	// Scan is either saved in memory or saved to disk, so it won't need to be downloaded again.
	[SerializeField] private Image cachedMarker;	

	private ScanData scanData;
	private bool selected = false;

	public int SiteID { get; private set; }
	public int SlabID { get; private set; }
	public int ScanID { get { return scanData.scan_id; } }

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.scan_bundle_cached, ScanCached);
		MessageDispatcher.AddListener (MessageDatabase.cached_scan_deleted, ScanCacheDeleted);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.scan_bundle_cached, ScanCached);
		MessageDispatcher.RemoveListener (MessageDatabase.cached_scan_deleted, ScanCacheDeleted);
	}

	private void ScanCached (IMessage message) {

		// If scan cached matches this item's referenced scan, show the cached marker.
		ScanData cachedScan = (ScanData)(message.Data);
		if (cachedScan == scanData) {
			cachedMarker.gameObject.SetActive (true);
		}
	}

	private void ScanCacheDeleted (IMessage message) {


		// If scan cached matches this item's referenced scan, hide the cached marker.
		ScanData cachedScan = (ScanData)(message.Data);
		if (cachedScan == scanData) {
			cachedMarker.gameObject.SetActive (false);
		}
	}

	public void Initialize (int site_id, int slab_id, ScanData scanData) {

		SiteID = site_id;
		SlabID = slab_id;
		this.scanData = scanData;

		selected = false;
		scanIDLabel.text = scanData.scan_id.ToString();
		selectedMarker.gameObject.SetActive (false);

		bool isCached = AssetBundleLoader.Instance.IsScanCached (site_id, slab_id, scanData.scan_id);
		cachedMarker.gameObject.SetActive (isCached);
	}

	public void Select () {

		MessageDispatcher.SendMessage (this, MessageDatabase.viewer_scan_selected, scanData, 0.0f);
	}

	public void Toggle () {

		if (selected) {
			Unload ();
		} else {
			Load ();
		}
	}

	public void ToggleSelectionMarkerOnly () {

		selected = !selected;
		selectedMarker.gameObject.SetActive (!selectedMarker.gameObject.activeSelf);
	}

	public void Load () {

		selected = true;
		selectedMarker.gameObject.SetActive (true);

		AssetBundleLoader.Instance.LoadScan (SiteID, SlabID, scanData);
	}

	public void Unload () {

		selected = false;
		selectedMarker.gameObject.SetActive (false);

		AssetBundleLoader.Instance.UnloadScan (SiteID, SlabID, scanData);
	}
}