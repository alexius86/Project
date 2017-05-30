using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TestSite {

	public string name;
	public string description;

	// Use an alternate name for slabs list so JSON utility doesn't try to deserialize it. 
	// We will be assigning these manually.
	public List<TestSlab> _slabs;

	public static string default_name = "site_name";
	public static string default_description = "default_description";

	public TestSite () {

		_slabs = new List<TestSlab> ();
		name = default_name;
		description = default_description;
	}
}

[System.Serializable]
public class TestSlab {

	public string name;
	public string timestamp;
	public string description;

	// Use an alternate name for scans list so JSON utility doesn't try to deserialize it. 
	// We will be assigning these manually.
	public List<TestScan> _scans;

	public static string default_name = "site_name";
	public static string default_description = "default_description";

	public TestSlab () {

		_scans = new List<TestScan> ();
		name = default_name;
		description = default_description;
	}
}

[System.Serializable]
public class TestScan {

	public string file_id;
	public string scan_type;
	public float longitude;
	public float latitude;

	public static string default_file_id = "default_file_id";

	public TestScan () {

		file_id = default_file_id;

		//TODO: Remove.
		latitude = -1.0f;
		longitude = -1.0f;
	}
}

public class JsonExtensionsTest : MonoBehaviour {

	[Multiline] public string jsonString;

	private List<TestSite> testSites = new List<TestSite>();

	void Update () {

		if (Input.GetKeyDown (KeyCode.Space)) {

			Debug.Log ("Testing JSON: \n" + jsonString);

			testSites.Clear ();
			testSites = new List<TestSite> ();

			// Get all site JSON data.
			string[] jsonSites = JsonHelper.GetJsonObjectArray (jsonString, "sites");
			if (jsonSites != null) {

				// For each site..
				for (int i = 0; i < jsonSites.Length; i++) {

					// Parse site data from JSON into data class.
					TestSite site = JsonUtility.FromJson<TestSite> (jsonSites [i]);

					// Get all slab JSON items for current site.
					string[] jsonSlabs = JsonHelper.GetJsonObjectArray (jsonSites [i], "slabs");
					if (jsonSlabs != null) {

						// For each slab..
						for (int j = 0; j < jsonSlabs.Length; j++) {

							// Parse slab data from JSON into data class.
							TestSlab slab = JsonUtility.FromJson<TestSlab> (jsonSlabs [j]);

							if (slab.name != TestSlab.default_name) {	// If still default, then it means we had an empty array.
								site._slabs.Add (slab);					// Add slab to current site's slab list.
							}

							// Get all slab JSON data.
							string[] jsonScans = JsonHelper.GetJsonObjectArray (jsonSlabs [j], "scans");
							if (jsonScans != null) {

								// For each scan..
								for (int k = 0; k < jsonScans.Length; k++) {

									// Parse scan data from JSON into data class.
									TestScan scan = JsonUtility.FromJson<TestScan> (jsonScans [k]);

									if (scan.file_id != TestScan.default_file_id) {	// If still default, then it means we had an empty array.
										slab._scans.Add (scan);						// Add scan to current slab's scan list.
									}
								}
							}
						}
					}

					// Add final site (with slab and scan info) to site list.
					testSites.Add (site);
				}
			}
		}
	}
}


/*

TEST
{
	"sites": [
	{
		"name": "site a",
		"description": "site a description",
		"slabs": [{
			"name": "slab 01",
			"timestamp": "some time",
			"description": "slab 01 description",
			"scans": [{
				"file_id": "scan 01",
				"scan_type": "type",
				"latitude": "50.0",
				"longitude": "-50.0"
			}]
		}]
	}, 
	{
		"name": "site b",
		"description": "site b description",
		"slabs": [{}]
	}, 
	{
		"name": "site c",
		"description": "site c description",
		"slabs": [{
			"name": "slab 01",
			"timestamp": "some time",
			"description": "slab 01 description",
			"scans": [{}]
		}]
	}]
}

PROPOSED BY SIGHTLINE
{
	"site_c": {
		"name": "site c",
		"description": "this is site c",
		"slabs": {
			"slab99_Wet": {
				"name": "slab99 Wet",
				"timestamp": "2017-03-28T14:04:45.402194",
				"description": "Some other slab",
				"scans": {}
			}
		}
	}
}
 */ 