using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class DataDisplay : DetailsPanelToggleItem {

	[Space(10.0f)]
	[SerializeField] private Image backerImage;
	[SerializeField] private GameObject contentRoot;
	[Space(10.0f)]
	[SerializeField] private Text siteName;
	[SerializeField] private Text siteDescription;
	[Space(10.0f)]
	[SerializeField] private Text slabName;
	[SerializeField] private Text slabDescription;
	[Space(10.0f)]
	[SerializeField] private Text scanName;
	[SerializeField] private Text scanDescription;

	void Awake () {

		Hide ();
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.menu_site_selected, SiteDataSelected);
		MessageDispatcher.AddListener (MessageDatabase.menu_slab_selected, SlabDataSelected);
		MessageDispatcher.AddListener (MessageDatabase.menu_scan_selected, ScanDataSelected);
	}

	void OnDisable () {
		
	    MessageDispatcher.RemoveListener (MessageDatabase.menu_site_selected, SiteDataSelected);
	    MessageDispatcher.RemoveListener (MessageDatabase.menu_slab_selected, SlabDataSelected);
	    MessageDispatcher.RemoveListener (MessageDatabase.menu_scan_selected, ScanDataSelected);
	}

	#region Site
	public string SiteName { 
		get { return siteName.text; }
		set {
			siteName.text = value;
		}
	}

	public string SiteDescription { 
		get { return siteDescription.text; }
		set {
			siteDescription.text = value;
		}
	}

	private void SiteDataSelected (IMessage message) {

		//Debug.Log ("Got site data in details display.");

		SiteData data = (SiteData)(message.Data);
		siteName.text = "Site: " + data.site_name;
		siteDescription.text = "Description: " + data.site_description;
	}
	#endregion

	#region Slab
	public string SlabName { 
		get { return slabName.text; }
		set {
			slabName.text = value;
		}
	}

	public string SlabDescription { 
		get { return slabDescription.text; }
		set {
			slabDescription.text = value;
		}
	}

	private void SlabDataSelected (IMessage message) {

		SlabData data = (SlabData)(message.Data);
		slabName.text = "Slab: " + data.slab_name;
		slabDescription.text = "Description: " + data.description;
	}
	#endregion

	#region Scan
	public string ScanName { 
		get { return scanName.text; }
		set {
			scanName.text = value;
		}
	}

	public string ScanDescription { 
		get { return scanDescription.text; }
		set {
			scanDescription.text = value;
		}
	}

	private void ScanDataSelected (IMessage message) {

		ScanData data = (ScanData)(message.Data);
		scanName.text = "Scan ID: " + data.scan_id;
		scanDescription.text = "Timestamp: " + data.timestamp + "\nType: " + data.type + "\nLatitude: " + data.latitude + "\nLongitude: " + data.longitude;
	}
	#endregion

	public override void Show () {

		backerImage.enabled = true;
		contentRoot.SetActive (true);
	}

	public override void Hide () {

		backerImage.enabled = false;
		contentRoot.SetActive (false);
	}
}
