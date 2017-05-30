using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class CameraSelector : MonoBehaviour {

	[SerializeField] private Transform cameraListRoot;
	[SerializeField] private Transform currentCameraRoot;
	[SerializeField] private GameObject cameraItemPrefab;
	[SerializeField] private Transform dropDownArrow;

	private List<CameraSelectItem> cameraItems;
	private bool cameraListOpen = false;

	private Vector3 dropDownArrowDownScale;
	private Vector3 dropDownArrowUpScale;

	private CameraSelectItem currentCamera = null;

	void Awake () {

		// Hide camera list initially.
		cameraListRoot.gameObject.SetActive (false);

		// Set initial drop down arrow state.
		dropDownArrowDownScale = dropDownArrow.localScale;
		dropDownArrowUpScale = new Vector3 (dropDownArrow.localScale.x, -dropDownArrow.localScale.y, dropDownArrow.localScale.z);
		dropDownArrow.localScale = dropDownArrowDownScale;

		// Delete camera item that might be in current as a mock-up.
		Transform mockCurrent = currentCameraRoot.GetChild(0);
		if (mockCurrent != null && mockCurrent.GetComponent<CameraSelectItem>() != null) {	// Make sure we don't delete drop-down arrow.
			Destroy (mockCurrent.gameObject);
		}
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.scan_list_received, ScanListLoaded);
		MessageDispatcher.AddListener (MessageDatabase.camera_selected, CameraSelected);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.scan_list_received, ScanListLoaded);
		MessageDispatcher.RemoveListener (MessageDatabase.camera_selected, CameraSelected);
	}

	public void Show () {

		cameraListOpen = true;
		cameraListRoot.gameObject.SetActive (true);

		// Flip arrow to point up when camera list is open.
		dropDownArrow.localScale = dropDownArrowUpScale;
	}

	public void Hide () {

		cameraListOpen = false;
		cameraListRoot.gameObject.SetActive (false);

		// Flip arrow to point down when camera list is closed.
		dropDownArrow.localScale = dropDownArrowDownScale;
	}

	public void CameraListToggle () {

		cameraListOpen = !cameraListOpen;
		cameraListRoot.gameObject.SetActive (cameraListOpen);

		// Flip arrow to point up when camera list is open.
		dropDownArrow.localScale = cameraListOpen ? dropDownArrowUpScale : dropDownArrowDownScale;
	}

	private void CameraSelected (IMessage message) {

		CameraSelectItem item = (CameraSelectItem)(message.Data);
		SetCurrentCamera (item);
	}

	private void ScanListLoaded (IMessage message) {

		ScanData[] scans = (ScanData[])(message.Data);
		Initialize (scans);
	}

	private void Initialize (ScanData[] scans) {

		// Clear out any old scan items in drop-down list.
		foreach (Transform t in cameraListRoot) {
			Destroy (t.gameObject);
		}

		// Delete camera item that might be in current as a mock-up.
		Transform mockCurrent = currentCameraRoot.GetChild(0);
		if (mockCurrent != null && mockCurrent.GetComponent<CameraSelectItem>() != null) {	// Make sure we don't delete drop-down arrow.
			Destroy (mockCurrent.gameObject);
		}

		cameraItems = new List<CameraSelectItem> ();

		// Set up free look camera (pan, rotate, zoom).
		CameraSelectItem freeCameraItem = AddItem(CameraSelectType.FREE);
		freeCameraItem.Select ();

		// Set up first-person camera.
		AddItem(CameraSelectType.FIRST_PERSON);

		// Add static scan cameras.
		for (int i = 0; i < scans.Length; i++) {
			AddItem (CameraSelectType.STATIC, scans [i]);
		}

		Hide ();
	}

	private void SetCurrentCamera (CameraSelectItem newCameraItem) {

		// Current camera should only ever be null right before the initial camera is set.
		if (currentCamera != null) {

			// Add current camera back to selection list.
			cameraItems.Add (currentCamera);

			// Move UI item back into drop-down list.
			currentCamera.transform.SetParent (cameraListRoot, false);

			Hide ();
			currentCamera = null;
		}

		// Remove camera item from drop-down list and place in current camera slot.
		if (cameraItems.Contains (newCameraItem)) {

			cameraItems.Remove (newCameraItem);
			newCameraItem.transform.SetParent (currentCameraRoot, false);
			newCameraItem.transform.SetAsFirstSibling ();
			currentCamera = newCameraItem;
		}

		#region Sorting
		// Sort drop-down list of cameras. Priority is Free->First Person->Static
		CameraSelectItem freeItem = null;
		CameraSelectItem firstPersonItem = null;
		List<CameraSelectItem> staticItems = new List<CameraSelectItem> ();

		for (int i = 0; i < cameraItems.Count; i++) {

			CameraSelectType type = cameraItems [i].Type;

			if (type == CameraSelectType.FREE) {
				freeItem = cameraItems [i];
			} else if (type == CameraSelectType.FIRST_PERSON) {
				firstPersonItem = cameraItems [i];
			} else {
				staticItems.Add (cameraItems [i]);
			}
		}

		//TODO: This is a terrible way of keeping this list priority sorted.
		if (freeItem != null) {
			freeItem.transform.SetAsFirstSibling ();
			if (firstPersonItem != null) {
				firstPersonItem.transform.SetSiblingIndex (1);
			}
			int siblingIndex = 2;
			for (int i = 0; i < staticItems.Count; i++) {
				staticItems [i].transform.SetSiblingIndex (siblingIndex);
				siblingIndex++;
			}
		} 
		else if (firstPersonItem != null) {
	
			firstPersonItem.transform.SetAsFirstSibling ();
			int siblingIndex = 1;
			for (int i = 0; i < staticItems.Count; i++) {
				staticItems [i].transform.SetSiblingIndex (siblingIndex);
				siblingIndex++;
			}
		} 
		else {
			int siblingIndex = 0;
			for (int i = 0; i < staticItems.Count; i++) {
				staticItems [i].transform.SetSiblingIndex (siblingIndex);
				siblingIndex++;
			}
		}
		#endregion
	}

	private CameraSelectItem AddItem (CameraSelectType type, ScanData scanData = null) {

		GameObject itemObj = GameObject.Instantiate (cameraItemPrefab) as GameObject;
		itemObj.transform.SetParent (cameraListRoot, false);
		CameraSelectItem item = itemObj.GetComponent<CameraSelectItem> ();
		item.Load (type, scanData);
		cameraItems.Add (item);

		return item;
	}
}
