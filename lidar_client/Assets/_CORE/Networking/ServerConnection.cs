using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BestHTTP;
using BestHTTP.Authentication;

using com.ootii.Messages;

/// <summary>
/// Access construction data from remote server. 
/// Site: the construction site. 
/// Slab: The specific concrete pour at a given site.
/// Scan: A specific scan of slab within site (time slice).
/// </summary>
public class ServerConnection : MonoBehaviour {

	#region Old - To Be Removed
	[SerializeField] private string url = "http://private-8bed3-slclient.apiary-mock.com";
	[SerializeField] private string versionPrefix = "/v1";
	[Space(10.0f)]
	[SerializeField] private string pingUrl = "/ping"; // initial server ping
	[SerializeField] private string loginUrl = "/login"; // login 
	[SerializeField] private string listSitesUrl = "/sites"; // list of all available sites
	[SerializeField] private string siteDetailUrl = "/site/"; // details of selected site & list of slabs (/v1/site/{site_id}
	#endregion

	[Space(10.0f)]
	[SerializeField] private ServerMessagePopup messagePopup;
	[Range(1, 60)]
	[SerializeField] private int pingTimeoutSeconds = 5;

	[Header("Production Server")]	//NOTE: These should eventually replace url, versionPrefix, <whatever>Url variables above (when server is up).
	#region Notes
	// Eg.) GET uri + sites + <site_name> + "/" + <slab_name> + "/" + <scan_name> + "/"
	//			- Returns json of scan info, not actual binary.

	// Eg.) GET uri + sites + <site_name> + slabs + "/"
	//			- Returns json for all slabs associated with the site.
	#endregion
	[SerializeField] private string uri = "http://deployed_instance_address.com/v1/";
	[SerializeField] private string auth = "auth/";
	[SerializeField] private string ping = "ping/";
	[SerializeField] private string user = "user/";
	[SerializeField] private string sites = "sites/";
	[SerializeField] private string slabs = "slabs/";
	[SerializeField] private string scans = "scans/";
	[SerializeField] private string download = "download";

	[Header("Debug")]
	[SerializeField] private bool debug_logs_enabled = false;

	// Debug events.
	public delegate void NetworkActivityHandler (string message);
	public static event NetworkActivityHandler OnNetworkActivity;

	// Singleton instance.
	private static ServerConnection _instance = null;
	public static ServerConnection Instance { get { return _instance; } }

	#region Init
	void Awake () {

		// Singleton.
	    if (_instance == null) {
			_instance = this;
			DontDestroyOnLoad (gameObject);
	    } 
		else {
	      Destroy (gameObject);
	    }
	}
	#endregion

	#region Ping
	void OnPingRequestCompletedDelegate (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Ping failed. \nUnable to reach server.");
			return;
		}

		DebugPrint ("Hello World Request Finished: " + response.DataAsText + " Message: " + response.Message);
	}
	#endregion

	#region User Login Authentication
	public void RequestAuthentication (string userName, string password) {	// xxx NOTE: Apiary doesn't support this yet 
		
		// Prepare request.
		HTTPRequest request = new HTTPRequest (new System.Uri (uri + auth), HTTPMethods.Post, OnAuthenticationCompleted);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		request.Credentials = new Credentials (userName, password);
		DebugPrint ("Requesting authentication for login.");
		request.Send ();
	}

	void OnAuthenticationCompleted (HTTPRequest request, HTTPResponse response) {

		string message = "";

		if (response == null) {

			message = "Authentication for login request failed.\n" + "Unable to reach server. Please check your internet connection.";
			messagePopup.ShowMessage (message);
			MessageDispatcher.SendMessage (this, MessageDatabase.user_auth_failure, message, 0.0f);
		} 
		else if (response.IsSuccess) {

			MessageDispatcher.SendMessage (MessageDatabase.user_auth_success);
		} 
		else if (!response.IsSuccess) {

			message = "Authentication for login request failed.\n" + response.Message;
			messagePopup.ShowMessage (message);
			MessageDispatcher.SendMessage (this, MessageDatabase.user_auth_failure, message, 0.0f);
		}
	}
	#endregion

	#region User Details
	public void RequestUser () {

		string targetUrl = uri + user;

		HTTPRequest request = new HTTPRequest (new System.Uri (uri + user), HTTPMethods.Get, OnUserRequestCompleted);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		DebugPrint ("Requesting current user info.");
		request.Send ();
	}

	void OnUserRequestCompleted (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("User details request failed.");
			return;
		}

		string message = "User Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		DebugPrint (message);

		//TODO: Message dispatch.
	}
	#endregion

	#region Sites
	public void DeleteAllSites () {

		HTTPRequest request	= new HTTPRequest (new System.Uri (uri + sites), HTTPMethods.Delete, OnSiteDeletionCompleted);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		DebugPrint ("Deleting all sites.");
		request.Send ();
	}

	void OnSiteDeletionCompleted (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Delete sites request failed.");
			return;
		}

		string message = "Delete Sites Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		DebugPrint (message);

		//TODO: Message dispatch.
	}

	public void AddSite (string name, string description) {

		HTTPRequest request	= new HTTPRequest (new System.Uri (uri + sites), HTTPMethods.Post, OnAddSiteCompleted);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		request.AddField ("name", name);
		request.AddField ("description", description);
		DebugPrint ("Adding site named " + name + " with description: " + description);
		request.Send ();
	}

	void OnAddSiteCompleted (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Add site request failed.");
			return;
		}

		string message = "Add Site Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		DebugPrint (message);

		//TODO: Message dispatch.
	}
		
	public void EditSite (string siteName, string siteNewName, string siteNewDescription) {

		HTTPRequest request	= new HTTPRequest (new System.Uri (uri + sites), HTTPMethods.Post, OnAddSiteCompleted);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		request.AddField ("name", siteNewName);
		request.AddField ("description", siteNewDescription);
		DebugPrint ("Updating details for site named: " + siteName + ".\nNew site name: " + siteNewName + ", new site description: " + siteNewDescription);
		request.Send ();
	}

	void OnEditSiteCompleted (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Edit site request failed.");
			return;
		}

		string message = "Edit Site Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		DebugPrint (message);

		//TODO: Message dispatch.
	}

	/// <summary>
	/// Requests list of all available sites.
	/// </summary>
	public void RequestSiteList () {

		string targetUrl = url + versionPrefix + listSitesUrl;	//TODO: "= uri + sites"

		// Prepare request.
		HTTPRequest request	= new HTTPRequest (new System.Uri (targetUrl), OnSiteListDataRequestCompletedDelegate);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		DebugPrint ("Requesting: " + targetUrl);
		request.Send ();
	}

	/// <summary>
	/// Got all sites.
	/// </summary>
	void OnSiteListDataRequestCompletedDelegate (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Loading site list failed. \nUnable to reach server. Will try again.");
			RequestSiteList ();	// Try again.
			return;
		}

		string message = "List All Sites Data Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		//DebugPrint ("[SITE LIST] Trying to parse JSON from: " + response.DataAsText);

		SiteData[] sites = JSONHelper.FromJson<SiteData> (response.DataAsText);
		MessageDispatcher.SendMessage (this, MessageDatabase.site_list_received, sites, 0.0f);

		DebugPrint (message);
	}
	#endregion

	#region Slabs
	/// <summary>
	/// Requests list of all available slabs for some site.
	/// </summary>
	public void RequestSlabList (int site_id) {

		string targetUrl = url + versionPrefix + siteDetailUrl + site_id + "/slabs";

		// Prepare request.
		HTTPRequest request	= new HTTPRequest (new System.Uri (targetUrl), OnSlabListDataRequestCompletedDelegate);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		DebugPrint ("Requesting: " + targetUrl);
		request.Send ();
	}

	/// <summary>
	/// Got all slabs. "/{site_id}/slabs"
	/// </summary>
	void OnSlabListDataRequestCompletedDelegate (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Loading slab list failed. \nUnable to reach server.");
			return;
		}

		string message = "List All Slabs Data Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		//DebugPrint ("[SLAB LIST] Trying to parse JSON from: " + response.DataAsText);

		SlabData[] slabs = JSONHelper.FromJson<SlabData> (response.DataAsText);
		MessageDispatcher.SendMessage (this, MessageDatabase.slab_list_received, slabs, 0.0f);

		DebugPrint (message);
	}
	#endregion

	#region Scans
	/// <summary>
	/// Requests list of all available scans for some slab on some site.
	/// </summary>
	public void RequestScanList (int site_id, int slab_id) {

		string targetUrl = url + versionPrefix + siteDetailUrl + site_id + "/" + slab_id + "/scans";

		// Prepare request.
		HTTPRequest request	= new HTTPRequest (new System.Uri (targetUrl), OnScanListDataRequestCompletedDelegate);
		request.ConnectTimeout = new System.TimeSpan (0, 0, pingTimeoutSeconds);
		DebugPrint ("Requesting: " + targetUrl);
		request.Send ();
	}

	/// <summary>
	/// Got all scans. "/{site_id}/{slab_id}/scans"
	/// </summary>
	void OnScanListDataRequestCompletedDelegate (HTTPRequest request, HTTPResponse response) {

		if (response == null) {
			messagePopup.ShowMessage ("Loading scan list failed. \nUnable to reach server.");
			return;
		}

		string message = "List All Scans Data Request " + (response.IsSuccess ? "Finished: " : "Failed: ") + 
			response.DataAsText + " Status Code: " + response.StatusCode + " Message: " + response.Message;

		//DebugPrint ("[SCAN LIST] Trying to parse JSON from: " + response.DataAsText);

		ScanData[] scans = JSONHelper.FromJson<ScanData> (response.DataAsText);
		MessageDispatcher.SendMessage (this, MessageDatabase.scan_list_received, scans, 0.0f);

		DebugPrint (message);
	}
	#endregion

	#region Debug
	private void DebugPrint (string message) {

		if (debug_logs_enabled) {

			if (OnNetworkActivity != null) {
				OnNetworkActivity (message);
			}
			Debug.Log (message);
		}
	}
	#endregion
}
