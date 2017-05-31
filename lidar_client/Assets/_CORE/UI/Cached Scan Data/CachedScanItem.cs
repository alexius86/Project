using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;
using TMPro;

public class CachedScanItem : MonoBehaviour {

	[SerializeField] private TextMeshProUGUI scanLabel;

	private int site_id;
	private int slab_id;
	private ScanData scanData;

	public int ScanID { get { return scanData != null ? scanData.scan_id : -1; } }

	public void Initialize (int site_id, int slab_id, ScanData scanData) {

		this.site_id = site_id;
		this.slab_id = slab_id;
		this.scanData = scanData;

		scanLabel.text = scanData.scan_id.ToString ();
	}

	public void Delete () {

		AssetBundleLoader.Instance.DeleteScanBundleFromDisk (site_id, slab_id, scanData.scan_id);
		MessageDispatcher.SendMessage (this, MessageDatabase.cached_scan_deleted, scanData, 0.0f);
	}
}















