using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public abstract class DetailsPanelToggleItem : MonoBehaviour {

	public delegate void ToggleHandler (DetailsPanelToggleItem toggledItem);
	public event ToggleHandler OnToggle;

	public abstract void Show ();
	public abstract void Hide ();

	public virtual void Toggle () {

		if (OnToggle != null) {
			OnToggle (this);
		}
	}
}

/// <summary>
/// Details panel UI is a menu toolbar with buttons and useful information (like a compass).
/// Each button opens up a small window with data or extra controls. Only one button's window
/// can be open at a time.
/// </summary>
public class DetailsPanelUI : MonoBehaviour {

	[Space(5.0f)]
	[SerializeField] private DetailsPanelToggleItem scanDetailsItem;
	[SerializeField] private DetailsPanelToggleItem cachedScansItem;
	[SerializeField] private DetailsPanelToggleItem levelingToolItem;
	[SerializeField] private DetailsPanelToggleItem planeControlsItem;
	[SerializeField] private DetailsPanelToggleItem camberControlItem;
 	[SerializeField] private DetailsPanelToggleItem generalSettingsItem;
	[Space(10.0f)]
	[SerializeField] private GameObject planeControlButton;
	[SerializeField] private GameObject camberControlButton;

	// Current panel button that is toggled ON. NULL if all toggle items are in OFF state.
	private DetailsPanelToggleItem currentItem = null;

	// The scans that are currently loaded into the scene.
	private List<ScanData> workingScans = new List<ScanData> ();

	void OnEnable () {

		scanDetailsItem.OnToggle += ItemToggled;
		cachedScansItem.OnToggle += ItemToggled;
		levelingToolItem.OnToggle += ItemToggled;
		planeControlsItem.OnToggle += ItemToggled;
		camberControlItem.OnToggle += ItemToggled;
    	generalSettingsItem.OnToggle += ItemToggled;

		MessageDispatcher.AddListener (MessageDatabase.camera_selected, OnCameraSelected);
		MessageDispatcher.AddListener (MessageDatabase.selection_menu_loaded, OnBackToMenu);

		MessageDispatcher.AddListener (MessageDatabase.scans_loaded, OnScanLoaded);
		MessageDispatcher.AddListener (MessageDatabase.scan_unloaded, OnScanUnloaded);
	}

	void OnDisable () {

		scanDetailsItem.OnToggle -= ItemToggled;
		cachedScansItem.OnToggle -= ItemToggled;
		levelingToolItem.OnToggle -= ItemToggled;
		planeControlsItem.OnToggle -= ItemToggled;
		camberControlItem.OnToggle -= ItemToggled;
   		generalSettingsItem.OnToggle -= ItemToggled;

		MessageDispatcher.RemoveListener (MessageDatabase.camera_selected, OnCameraSelected);
		MessageDispatcher.RemoveListener (MessageDatabase.selection_menu_loaded, OnBackToMenu);

		MessageDispatcher.RemoveListener (MessageDatabase.scans_loaded, OnScanLoaded);
		MessageDispatcher.RemoveListener (MessageDatabase.scan_unloaded, OnScanUnloaded);
	}

	private void OnScanLoaded (IMessage message) {

		workingScans.Add (((Scan)message.Data).scanData);
	}

	private void OnScanUnloaded (IMessage message) {

		workingScans.Remove ((ScanData)message.Data);

		// Make sure plane and camber edit buttons and displays are hidden if there are no more
		// scan objects to work with.
		if (workingScans.Count == 0) {

			// Turn off camber edit mode so that it isn't already on for next scan that is loaded.
			//(camberControlItem as CamberControlDisplay).CamberEditToggle(false);

			// Hide display windows.
			planeControlsItem.Hide ();
			camberControlItem.Hide ();

			// Hide buttons.
			planeControlButton.SetActive(false);
			camberControlButton.SetActive (false);
		}
	}

	private void OnBackToMenu (IMessage message) {

		// Hide all display windows.
		scanDetailsItem.Hide ();
		cachedScansItem.Hide ();
		levelingToolItem.Hide ();
		planeControlsItem.Hide ();
		camberControlItem.Hide ();

		currentItem = null;
	}

	private void OnCameraSelected (IMessage message) {

		CameraSelectItem item = (CameraSelectItem)message.Data;
		if (item.Type == CameraSelectType.FIRST_PERSON) {

			scanDetailsItem.Hide ();
			cachedScansItem.Hide ();
			levelingToolItem.Hide ();
			planeControlsItem.Hide ();
			camberControlItem.Hide ();

			currentItem = null;
		}
	}

	private void ItemToggled (DetailsPanelToggleItem toggledItem) {

		// If active window's item was toggled, hide the active window and clear current item pointer.
		if (currentItem != null && toggledItem == currentItem) {
			
			currentItem.Hide ();
			currentItem = null;
		} 
		else {	// Out with the old and in with the new..

			if (currentItem != null) {
				currentItem.Hide ();
			}
			currentItem = toggledItem;
			currentItem.Show ();
		}
	}
}







