
public class MessageDatabase {

	// Camera control type was changed.
	// Message Data: CameraSelectItem (check 'Type' property).
	// FREE: Pan, Rotate, Zoom around a target point.
	// FIRST_PERSON: Walk around plane in first person view.
	// SCAN: Camera is set to a specific point with rotation and cannot be moved.
	public static string camera_selected = "camera_selected";

	// Site entry was selected from site list in menu.
	// Message Data: SiteData
	public static string menu_site_selected = "menu_site_selected";

	// Slab entry was selected from slab list in menu.
	// Message Data: SlabData
	public static string menu_slab_selected = "menu_slab_selected";

	// Leveling tool is now in one of three modes: Reference Plane, Camber Plane, Disabled.
	public static string levelingToolEditModeChanged = "leveling_tool_edit_mode_changed";

	#region Scan Cache
	// Message Data: ScanData
	public static string scan_bundle_cached = "scan_bundle_cached";

	// Message Data: ScanData
	public static string cached_scan_deleted = "cached_scan_deleted";
	#endregion

	#region UI messages
	public static string selection_menu_loaded = "selection_menu_loaded";
	#endregion

	#region Scans
	// Scan entry was selected from scan list in menu.
	// Message Data: ScanData
	public static string menu_scan_selected = "menu_scan_selected";	

	// Scan was selected from list in main scene's viewer UI. Scan gets toggled (load/unload).
	public static string viewer_scan_selected = "scan_selected";

	// Began loading AssetBundle for some scan.
	// Message Data: ScanData[]
	public static string load_scans = "load_scans";

	// AssetBundle load in progress.
	// Message Data: float (normalized progress value).
	public static string scans_loading = "scans_loading";

	// AssetBundle finished loading. Copy of bundle data instantiated in scene.
	// Message Data: Scan object
	public static string scans_loaded = "scans_loaded";

  	// Asset bundle unloads scan
	// Message Data: ScanData
  	public static string scan_unloaded = "scan_unloaded";

	// Something went wrong with AssetBundle load (no internet access, etc).
	// Message Data: None
	public static string scan_load_failed = "scan_load_failed";

	public static string scan_load_cancelled = "scan_load_cancelled";

	// Number of scanned objects in scene was just updated and the layout of these objects was changed.
	// Message Data: Bounds (bounding box containing ALL scan objects loaded and organized in scene).
	public static string loaded_scans_refreshed = "loaded_scans_refreshed";
	#endregion

	#region Network Messages
	// Login success.
	// Message Data: None?
	public static string user_auth_success = "user_auth_success";

	// Login failed.
	// Message Data: string response message.
	public static string user_auth_failure = "user_auth_failure";

	// Got user data for currently logged in user.
	// Message Data: UserData
	public static string user_info_received = "got_user_info";

	// Received requested list of all available sites.
	// Message Data: SiteData[]
	public static string site_list_received = "site_list_received";

	// Received requested list of all available slabs for currently selected site.
	// Message Data: SlabData[]
	public static string slab_list_received = "slab_list_received";

	// Received requested list of all available scans for currently selected slab from currently selected site.
	// Message Data: ScanData[]
	public static string scan_list_received = "scan_list_received";
	#endregion
}
