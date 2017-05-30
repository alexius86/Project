using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using com.ootii.Messages;

public class CamberControlDisplay : DetailsPanelToggleItem {

	[Space(5.0f)]
	[SerializeField] private GameObject root;
	[SerializeField] private LevelingTool levelingTool;
	[SerializeField] private Toggle camberToggle;

	[Header("Control Items")]
	[SerializeField] private GameObject xPositionItem;
	[SerializeField] private GameObject zPositionItem;
	[SerializeField] private GameObject xScaleItem;
	[SerializeField] private GameObject zScaleItem;
	[SerializeField] private GameObject yOffsetItem;
	[SerializeField] private float yOffsetSpeed = 1.0f;

	[Header("Input Fields")]
	[SerializeField] private TMP_InputField posX;
	[SerializeField] private TMP_InputField posZ;
	[Space(5.0f)]
	[SerializeField] private TMP_InputField scaleX;
	[SerializeField] private TMP_InputField scaleZ;
	[Space(5.0f)]
	[SerializeField] private TMP_InputField offsetY;

	private float camberPlaneStartY;	// Y position of camber plane at time of creation. Y offset is applied to this value.

	void Awake () {

		levelingTool.CamberPlaneCreated += OnCamberPlaneCreated;

		#region Position
		posX.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.CamberPlane != null) {
				Vector3 pos = levelingTool.CamberPlane.position;
				pos.x = Parse (s);
				levelingTool.CamberPlane.position = pos;
			}
		});

		offsetY.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.CamberPlane != null) {
        Vector3 pos = levelingTool.CamberPlane.position;
        pos.y = Parse(s);
        levelingTool.CamberPlane.position = pos;
			}
		});

		posZ.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.CamberPlane != null) {
				Vector3 pos = levelingTool.CamberPlane.position;
				pos.z = Parse (s);
				levelingTool.CamberPlane.position = pos;
			}
		});
		#endregion

		#region Scale
		scaleX.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.CamberPlane != null) {
				Vector3 scale = levelingTool.CamberPlane.localScale;
				scale.x = Parse (s);
				levelingTool.CamberPlane.localScale = scale;
			}
		});

		scaleZ.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.CamberPlane != null) {
				Vector3 scale = levelingTool.CamberPlane.localScale;
				scale.z = Parse (s);
				levelingTool.CamberPlane.localScale = scale;
			}
		});
		#endregion

		Hide ();
	}

	void OnEnable () {
		MessageDispatcher.AddListener (MessageDatabase.camera_selected, CameraModeChanged);
	}

	void OnDisable () {
		MessageDispatcher.RemoveListener (MessageDatabase.camera_selected, CameraModeChanged);
	}

	private void CameraModeChanged (IMessage message) {

		// If first person camera was selected, exit camber edit mode and disable the toggle button.
		CameraSelectItem item = (CameraSelectItem)(message.Data);
		if (item.Type == CameraSelectType.FIRST_PERSON && levelingTool.mode == LevelingToolEditMode.CamberPlane) {

			CamberEditToggle (false);
			camberToggle.interactable = false;
		} 
		else {
			camberToggle.interactable = true;	// Allow camber mode toggle for all other camera modes.
		}
	}

	public void ResetCamberPlane () {

		levelingTool.ResetCamberPlane ();
	}

	private float Parse (string s) {

		float val = 0;
		float.TryParse (s, out val);
		return val;
	}

	private void OnCamberPlaneCreated (float originY) {

		camberPlaneStartY = originY;
		LateUpdate ();
	}

	void LateUpdate () {

		Transform camberPlane = levelingTool.CamberPlane;
		if (camberPlane != null) {

			UpdateField (posX, camberPlane.position.x);
			UpdateField (posZ, camberPlane.position.z);
      		UpdateField (offsetY, camberPlane.position.y);

			UpdateField (scaleX, camberPlane.localScale.x);
			UpdateField (scaleZ, camberPlane.localScale.z);
		}

    	UpdateCamberToggle ();
	}

	void UpdateField (TMP_InputField field, float value) {

		if (!field.isFocused) {
			field.text = ((int)(value * 1000) / 1000f).ToString();
		}
	}

	void UpdateCamberToggle () {
		
		if (levelingTool.mode != LevelingToolEditMode.Disabled) {
			camberToggle.isOn = levelingTool.mode == LevelingToolEditMode.CamberPlane;
		}
	}

	public override void Show () {
		
		root.SetActive (true);

		xPositionItem.SetActive (camberToggle.isOn);
		zPositionItem.SetActive (camberToggle.isOn);
		xScaleItem.SetActive (camberToggle.isOn);
		zScaleItem.SetActive (camberToggle.isOn);
		yOffsetItem.SetActive (camberToggle.isOn);
	}

	public override void Hide () {

		xPositionItem.SetActive (false);
		zPositionItem.SetActive (false);
		xScaleItem.SetActive (false);
		zScaleItem.SetActive (false);
		yOffsetItem.SetActive (false);

		root.SetActive (false);
	}

	public void CamberEditToggle (bool value) {

		// Let LevelingTool know that camber edit mode was enabled/disabled.
		if (value) {
      		levelingTool.SetMode (LevelingToolEditMode.CamberPlane);
		} else {
      		levelingTool.SetMode (LevelingToolEditMode.ReferencePlane);
		}

		Debug.Log ("Setting camber mode: " + value);

		// Also set toggle value in case this function is called from a script instead
		// of the toggle itself.
		camberToggle.isOn = value;

		xPositionItem.SetActive (value);
		zPositionItem.SetActive (value);
		xScaleItem.SetActive (value);
		zScaleItem.SetActive (value);
		yOffsetItem.SetActive (value);
	}
}









