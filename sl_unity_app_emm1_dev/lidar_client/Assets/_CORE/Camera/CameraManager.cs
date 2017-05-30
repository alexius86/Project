using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class CameraManager : MonoBehaviour {

	[Header("Cameras")]
	[SerializeField] private CameraFreeLookCustom freeCamera;
	[SerializeField] private FirstPersonCameraControl firstPersonCamera;
	[SerializeField] private StaticCamera staticCamera;

	private GameObject currentCamera;

	void Awake () {

		currentCamera = freeCamera.gameObject;
		freeCamera.Toggle (true);

		firstPersonCamera.gameObject.SetActive (false);
		staticCamera.gameObject.SetActive (false);
	}

	void OnEnable () {
		MessageDispatcher.AddListener (MessageDatabase.camera_selected, NewCameraSelected);
	}

	void OnDisable () {
		MessageDispatcher.RemoveListener (MessageDatabase.camera_selected, NewCameraSelected);
	}

	private void NewCameraSelected (IMessage message) {

		//TODO: Use an interface or something so all cameras disable self in same way.
		// 		Free camera can't have GameObject disabled because it needs to listen for scan load/unload from
		//		other camera views like first person view.
		if (currentCamera == freeCamera.gameObject) {
			freeCamera.Toggle (false);
		} else {
			currentCamera.SetActive (false);
		}

		CameraSelectItem selectedItem = (CameraSelectItem)(message.Data);
		if (selectedItem.Type == CameraSelectType.FREE) {

			currentCamera = freeCamera.gameObject;
			freeCamera.Toggle (true);
			freeCamera.Reset ();
		}
		else if (selectedItem.Type == CameraSelectType.FIRST_PERSON) {

			currentCamera = firstPersonCamera.gameObject;
			firstPersonCamera.Activate (Vector3.zero);
		}
		else if (selectedItem.Type == CameraSelectType.STATIC) {
			
			currentCamera = staticCamera.gameObject;

			Vector3 staticPos = AssetBundleLoader.Instance.WorldPositionForLoadedScan (selectedItem.ScanID);
			staticCamera.Set (staticPos, selectedItem.Rotation);
		}

		currentCamera.SetActive (true);
	}
}







