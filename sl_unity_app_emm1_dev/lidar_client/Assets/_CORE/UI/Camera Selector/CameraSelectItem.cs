using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using com.ootii.Messages;

public enum CameraSelectType {
	NONE, FREE, FIRST_PERSON, STATIC,
}

public class CameraSelectItem : MonoBehaviour {

	[SerializeField] private TextMeshProUGUI label;

	private CameraSelectType cameraType;
	private bool selected = false;

	// Static scan camera only.
	private ScanData scanData;
	private Vector3 _scanPosition;
	private Vector3 _scanEulerAngles;

	public CameraSelectType Type { get { return cameraType; } }

	// Static scan camera only.
	public int ScanID { get { return scanData.scan_id; } } 
	public Vector3 Position { get { return _scanPosition; } }
	public Vector3 Rotation { get { return _scanEulerAngles; } }

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.camera_selected, CameraSelected);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.camera_selected, CameraSelected);
	}

	public void Load (CameraSelectType type, ScanData scanData = null) {

		if (type == CameraSelectType.FREE) {
			LoadFreeLookCamera ();
		} 
		else if (type == CameraSelectType.FIRST_PERSON) {
			LoadFirstPersonCamera ();
		} 
		else if (type == CameraSelectType.STATIC) {
			LoadScanCamera (scanData);
		}

		selected = false;
	}

	private void LoadFreeLookCamera () {

		cameraType = CameraSelectType.FREE;
		label.text = "Free";
	}

	private void LoadFirstPersonCamera () {

		cameraType = CameraSelectType.FIRST_PERSON;
		label.text = "First Person";
	}

	private void LoadScanCamera (ScanData scanData) {

		cameraType = CameraSelectType.STATIC;

		this.scanData = scanData;
		label.text = "Scan " + scanData.scan_id.ToString();

		//TODO: Get position and rotation angle out of scan data.
	}

	public void Select () {

		selected = true;
		MessageDispatcher.SendMessage (this, MessageDatabase.camera_selected, this, 0.0f);
	}

	private void Deselect () {

		selected = false;
	}

	private void CameraSelected (IMessage message) {

		// Camera was selected from UI and we are also selected.
		if (selected) {

			// If selected camera item is not the camera item that we reference, deselect our camera item.
			CameraSelectItem item = (CameraSelectItem)(message.Data);
			if (item != this) {
				Deselect ();
			}
		}
	}
}
