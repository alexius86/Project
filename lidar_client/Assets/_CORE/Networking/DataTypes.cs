using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
/// <summary>
/// Data related to a specific site.
/// </summary>
public class SiteData {
	
	public int site_id = -1;
	public string site_name = "none";
	public string site_description = "none";
}
	
[System.Serializable]
/// <summary>
/// Data related to a specific slab.
/// </summary>
public class SlabData {

	public int slab_id = -1;
	public string slab_name = "none";
	public string timestamp = "none";
	public string description = "none";
}

[System.Serializable]
/// <summary>
/// Data related to a specific scan.
/// </summary>
public class ScanData {
	
	public int scan_id = -1;
	public string type = "none";
	public string timestamp = "none";
	public double longitude = 0.0;
	public double latitude = 0.0;
	public string url = "www.google.com";
}

[System.Serializable]
/// <summary>
/// Data related to user currently logged in.
/// </summary>
public class UserData {

	public string id = "default_id";
	public string username = "default_user_name";
	public string email = "a@a.com";
	public string first_name = "first";
	public string last_name = "last";
}
