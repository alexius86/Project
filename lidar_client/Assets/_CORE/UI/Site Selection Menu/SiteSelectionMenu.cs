using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using com.ootii.Messages;

public class SiteSelectionMenu : MonoBehaviour {

	#region Serialized Site Fields
	[Header("Scene Changes")]
	[SerializeField] private string loginSceneName = "Login";

	[Header("Site Select")]
	[Tooltip("The root of site selection menu. Gets resized based on size of scroll rect.")]
	[SerializeField] private RectTransform siteSelectWindow;

	[Tooltip("Scroll view containing site entries. Gets resized based on number of entries.")]
	[SerializeField] private ScrollRect siteSelectScrollRect;

	[Tooltip("Where scroll view content goes. Site list entries should be placed as children of this transform.")]
	[SerializeField] private Transform siteSelectContentRoot;

	[Tooltip("List entry prefab. Instantiated in scroll list for each available site.")]
	[SerializeField] private GameObject siteItemPrefab;
	#endregion

	#region Serialized Slab Fields
	[Header("Slab Select")]
	[Tooltip("The root of slab selection menu. Gets resized based on size of scroll rect.")]
	[SerializeField] private RectTransform slabSelectWindow;

	[Tooltip("Scroll view containing slab entries. Gets resized based on number of entries.")]
	[SerializeField] private ScrollRect slabSelectScrollRect;

	[Tooltip("Where scroll view content goes. Slab list entries should be placed as children of this transform.")]
	[SerializeField] private Transform slabSelectContentRoot;

	[Tooltip("List entry prefab. Instantiated in scroll list for each available slab.")]
	[SerializeField] private GameObject slabItemPrefab;
	#endregion

	#region Serialized Scan Fields
	[Header("Scan Select")]
	[SerializeField] private RectTransform scanSelectWindow;

	[Tooltip("Scroll view containing scan entries. Gets resized based on number of entries.")]
	[SerializeField] private ScrollRect scanSelectScrollRect;

	[Tooltip("Where scroll view content goes. Scan list entries should be placed as children of this transform.")]
	[SerializeField] private Transform scanSelectContentRoot;

	[Tooltip("List entry prefab. Instantiated in scroll list for each available scan.")]
	[SerializeField] private GameObject scanItemPrefab;
	#endregion

  	#region Misc
	  [Header("Misc")]
	  [SerializeField] private GameObject background;
	  [SerializeField] private GameObject loadingIndicator;
	  [SerializeField] private GameObject showSiteSelectionButton;
	[SerializeField] private GameObject logoutButton;
	[SerializeField] private GameObject screenshotButton;
	  #endregion

	#region Private Fields
	private int site_id = -1;
	private int slab_id = -1;
	private Dictionary<int, ScanData> availableScans = new Dictionary<int, ScanData>();
 	private Dictionary<int, SiteListItem> siteListItems = new Dictionary<int, SiteListItem>();
  	private Dictionary<int, SlabListItem> slabListItems = new Dictionary<int, SlabListItem>();
	#endregion

	#region Initialization
	void Start () {
		//DEBUG - REMOVE LATER
		Show();
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.site_list_received, SiteListRefresh);
		MessageDispatcher.AddListener (MessageDatabase.slab_list_received, SlabListRefresh);
		MessageDispatcher.AddListener (MessageDatabase.scan_list_received, ScanListRefresh);

		MessageDispatcher.AddListener (MessageDatabase.menu_site_selected, SiteSelected);
		MessageDispatcher.AddListener (MessageDatabase.menu_slab_selected, SlabSelected);
		MessageDispatcher.AddListener (MessageDatabase.menu_scan_selected, ScanSelected);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.site_list_received, SiteListRefresh);
		MessageDispatcher.RemoveListener (MessageDatabase.slab_list_received, SlabListRefresh);
		MessageDispatcher.RemoveListener (MessageDatabase.scan_list_received, ScanListRefresh);

		MessageDispatcher.RemoveListener (MessageDatabase.menu_site_selected, SiteSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.menu_slab_selected, SlabSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.menu_scan_selected, ScanSelected);
	}
	#endregion

	#region Visibility
	public void Show () {

		// If any scans were in the middle of loading/unloading, make sure they get cancelled.
		MessageDispatcher.SendMessage(MessageDatabase.scan_load_cancelled);

		// Unload the scans that were in the viewer scene.
		AssetBundleLoader.Instance.UnloadAllScans();

	    background.SetActive (true);
	    showSiteSelectionButton.SetActive (false);

		// Only show site list when menu is first opened.
		siteSelectWindow.gameObject.SetActive (true);
		slabSelectWindow.gameObject.SetActive (false);
		scanSelectWindow.gameObject.SetActive (false);

    	ClearSiteList ();
		// Get list of available sites.
		ServerConnection.Instance.RequestSiteList();

		// Reset selection IDs.
		site_id = -1;
		slab_id = -1;
		availableScans = new Dictionary<int, ScanData> ();

    	ToggleLoadingIndicator (true, siteSelectWindow);
		logoutButton.SetActive (true);
		screenshotButton.SetActive (false);

    MessageDispatcher.SendMessage (MessageDatabase.selection_menu_loaded);
	}

	public void Hide () {

	    background.SetActive (false);
	    loadingIndicator.SetActive (false);
		siteSelectWindow.gameObject.SetActive (false);
		slabSelectWindow.gameObject.SetActive (false);
		scanSelectWindow.gameObject.SetActive (false);

   		showSiteSelectionButton.SetActive (true);
		logoutButton.SetActive (false);
		screenshotButton.SetActive (true);
	}

	private void ToggleSiteMenu (bool isVisible) {
		siteSelectWindow.gameObject.SetActive (isVisible);
	}

	private void ToggleSlabMenu (bool isVisible) {
		slabSelectWindow.gameObject.SetActive (isVisible);
	}

	private void ToggleScanMenu (bool isVisible) {
		scanSelectWindow.gameObject.SetActive (isVisible);
	}

	  private void ToggleLoadingIndicator(bool isVisible, RectTransform window = null) {
	    if (window != null)
	      loadingIndicator.transform.position = window.transform.position + (Vector3)window.rect.center * window.lossyScale.x;
	    loadingIndicator.SetActive (isVisible);
	  }
	#endregion

	#region List Population Messages
	private void SiteListRefresh (IMessage message) {

		SiteData[] sites = (SiteData[])(message.Data);

		// Instantiate new list items for each site. TODO: Pooling.
		for (int i = 0; i < sites.Length; i++) {

			SiteListItem newSite = GameObject.Instantiate (siteItemPrefab).GetComponent<SiteListItem>();

      		newSite.Data = sites [i];
			newSite.ItemID = sites [i].site_id;
			newSite.ItemName = sites [i].site_name;
      		newSite.ItemDescription = sites [i].site_description;

      		newSite.BackButton.onClick.AddListener (DeselectSite);

			// Place within list layout.
			newSite.transform.SetParent (siteSelectContentRoot, false);

      		siteListItems.Add (newSite.ItemID, newSite);
		}

    ToggleLoadingIndicator (false);
	}

	private void SlabListRefresh (IMessage message) {

		SlabData[] slabs = (SlabData[])(message.Data);

		// Instantiate new list items for each slab. TODO: Pooling.
		for (int i = 0; i < slabs.Length; i++) {

			SlabListItem newSlab = GameObject.Instantiate (slabItemPrefab).GetComponent<SlabListItem>();

      		newSlab.Data = slabs [i];
			newSlab.ItemID = slabs [i].slab_id;
			newSlab.ItemName = slabs [i].slab_name;
      		newSlab.ItemDescription = slabs [i].description;

      		newSlab.BackButton.onClick.AddListener (DeselectSlab);

			// Place within list layout.
			newSlab.transform.SetParent (slabSelectContentRoot, false);

      		slabListItems.Add (newSlab.ItemID, newSlab);
		}

    	ToggleLoadingIndicator (false);
	}

	private void ScanListRefresh (IMessage message) {

		ScanData[] scans = (ScanData[])(message.Data);

		// Copy all scans into a dictionary so we can access specific data in O(1) time.
		availableScans = new Dictionary<int, ScanData> ();
		for (int i = 0; i < scans.Length; i++) {
			availableScans.Add (scans [i].scan_id, scans [i]);
		}

		// Instantiate new list items for each scan. TODO: Pooling.
		for (int i = 0; i < scans.Length; i++) {

			ScanListItem newScan = GameObject.Instantiate (scanItemPrefab).GetComponent<ScanListItem>();

      		newScan.Data = scans [i];
			newScan.ItemID = scans [i].scan_id;
			newScan.ItemName = newScan.ItemID.ToString ();

			// Place within list layout.
			newScan.transform.SetParent (scanSelectContentRoot, false);
		}

    ToggleLoadingIndicator (false);
	}
	#endregion

	#region Selection Messages
	private void SiteSelected (IMessage message) {

    ToggleSlabMenu (true);
    ClearSlabList ();
    ToggleLoadingIndicator (true, slabSelectWindow);

    site_id = ((SiteData)message.Data).site_id;
		ServerConnection.Instance.RequestSlabList (site_id);

    // Hide all slabs but the selected one and show description
    foreach (var kv in siteListItems)
    {
      if (kv.Value.ItemID != site_id) {
        kv.Value.gameObject.SetActive (false);
      } else {
        kv.Value.ShowDetails ();
        // TODO: possibly set the rect scroll position to top if that's an issue
      }
    }
	}

	private void SlabSelected (IMessage message) {

    ToggleScanMenu (true);
    ClearScanList ();
    ToggleLoadingIndicator (true, scanSelectWindow);

		slab_id = ((SlabData)message.Data).slab_id;
		ServerConnection.Instance.RequestScanList (site_id, slab_id);

    // Hide all slabs but the selected one and show description
    foreach (var kv in slabListItems)
    {
      if (kv.Value.ItemID != slab_id) {
        kv.Value.gameObject.SetActive (false);
      } else {
        kv.Value.ShowDetails ();
        // TODO: possibly set the rect scroll position to top if that's an issue
      }
    }
	}

	private void ScanSelected (IMessage message) {

		// Hide menu.
		Hide ();
	}
	#endregion

	#region UI Messages
	public void Logout () {
		
		// TODO: User logout logic.

		SceneManager.LoadScene (loginSceneName);
	}

	public void ConfirmScanSelection () {

		//TODO: Request scan for each scan ID in list from main networking script.
	}

	public void DeselectSite () {

		// Close any sub-menus that may be open.
		ToggleScanMenu (false);
		ToggleSlabMenu (false);

		slab_id = -1;
		availableScans = new Dictionary<int, ScanData> ();

    siteListItems [site_id].HideDetails();

    foreach (var kv in siteListItems)
    {
      kv.Value.gameObject.SetActive (true);
    }
	}

	public void DeselectSlab () {

		// Close any sub-menus that may be open.
		ToggleScanMenu (false);
		availableScans = new Dictionary<int, ScanData> ();

		//TODO: Close the detailed view of the deselected slab entry.

    slabListItems [slab_id].HideDetails();

    foreach (var kv in slabListItems)
    {
      kv.Value.gameObject.SetActive (true);
    }
	}
	#endregion

  private void ClearSiteList()
  {
    // Clear out site list. TODO: Pooling.
    foreach (Transform t in siteSelectContentRoot) {
      Destroy (t.gameObject);
    }
    siteListItems.Clear ();
  }

  private void ClearSlabList()
  {
    // Clear out slab list. TODO: Pooling.
    foreach (Transform t in slabSelectContentRoot) {
      Destroy (t.gameObject);
    }
    slabListItems.Clear ();
  }

  private void ClearScanList()
  {
    // Clear out scan list. TODO: Pooling.
    foreach (Transform t in scanSelectContentRoot) {
      Destroy (t.gameObject);
    }
  }
}









