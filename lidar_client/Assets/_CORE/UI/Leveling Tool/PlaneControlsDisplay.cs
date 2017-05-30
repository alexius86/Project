using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class PlaneControlsDisplay : DetailsPanelToggleItem {

	[Space(5.0f)]
	[SerializeField] private GameObject root;
	[Space(5.0f)]
	[SerializeField] private LevelingTool levelingTool;
	[Space(5.0f)]
	[SerializeField] private TMP_InputField posX;
	[SerializeField] private TMP_InputField posY;
	[SerializeField] private TMP_InputField posZ;
	[Space(5.0f)]
	[SerializeField] private TMP_InputField rotX;
	[SerializeField] private TMP_InputField rotY;
	[SerializeField] private TMP_InputField rotZ;
	[Space(5.0f)]
	[SerializeField] private TMP_InputField scaleX;
	[SerializeField] private TMP_InputField scaleZ;

	void Awake () {

		levelingTool.ReferencePlaneCreated += OnReferencePlaneCreated;

		#region Position
		posX.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 pos = levelingTool.ReferencePlane.position;
				pos.x = Parse (s);
				levelingTool.ReferencePlane.position = pos;
			}
		});

		posY.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 pos = levelingTool.ReferencePlane.position;
				pos.y = Parse (s);
				levelingTool.ReferencePlane.position = pos;
			}
		});

		posZ.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 pos = levelingTool.ReferencePlane.position;
				pos.z = Parse (s);
				levelingTool.ReferencePlane.position = pos;
			}
		});
		#endregion

		#region Rotation
		rotX.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 rot = levelingTool.ReferencePlane.eulerAngles;
				rot.x = Parse (s);
				levelingTool.ReferencePlane.rotation = Quaternion.Euler(rot);
			}
		});

		rotY.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 rot = levelingTool.ReferencePlane.eulerAngles;
				rot.y = Parse (s);
				levelingTool.ReferencePlane.rotation = Quaternion.Euler(rot);
			}
		});

		rotZ.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 rot = levelingTool.ReferencePlane.eulerAngles;
				rot.z = Parse (s);
				levelingTool.ReferencePlane.rotation = Quaternion.Euler(rot);
			}
		});
		#endregion

		#region Scale
		scaleX.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 scale = levelingTool.ReferencePlane.localScale;
				scale.x = Parse (s);
				levelingTool.ReferencePlane.localScale = scale;
			}
		});

		scaleZ.onValueChanged.AddListener ((s) => {
			if (levelingTool != null && levelingTool.ReferencePlane != null) {
				Vector3 scale = levelingTool.ReferencePlane.localScale;
				scale.z = Parse (s);
				levelingTool.ReferencePlane.localScale = scale;
			}
		});
		#endregion

		Hide ();
	}

	void OnReferencePlaneCreated (Transform referencePlane) {
		LateUpdate ();
	}

	void LateUpdate () {
		
		if (levelingTool.ReferencePlane != null) {
			
			UpdateField (posX, levelingTool.ReferencePlane.position.x);
			UpdateField (posY, levelingTool.ReferencePlane.position.y);
			UpdateField (posZ, levelingTool.ReferencePlane.position.z);

			UpdateField (rotX, levelingTool.ReferencePlane.eulerAngles.x);
			UpdateField (rotY, levelingTool.ReferencePlane.eulerAngles.y);
			UpdateField (rotZ, levelingTool.ReferencePlane.eulerAngles.z);

      		UpdateField (scaleX, levelingTool.ReferencePlane.localScale.x);
      		UpdateField (scaleZ, levelingTool.ReferencePlane.localScale.z);
		}
	}

	void UpdateField (TMP_InputField field, float value) {
		
		if (!field.isFocused) {
			field.text = ((int)(value * 1000) / 1000f).ToString();
		}
	}

	public void ResetReferencePlane () {

		levelingTool.ResetReferencePlane ();
	}

	public override void Show () {
		root.SetActive (true);
	}

	public override void Hide () {
		root.SetActive (false);
	}

	float Parse (string s) {
		
		float val = 0;
		float.TryParse (s, out val);
		return val;
	}
}














