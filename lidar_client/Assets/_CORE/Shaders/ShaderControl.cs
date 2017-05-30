using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ShaderControl : MonoBehaviour {

	[Space(5.0f)]
	[SerializeField] private LevelingTool levelingTool;

	[Header("UI References")]
	[SerializeField] private TMP_InputField lowerInputField;
	[SerializeField] private TMP_InputField targetMinInputField;
	[SerializeField] private TMP_InputField targetMaxInputField;
	[SerializeField] private TMP_InputField upperInputField;

	[Header("Material References")]
	// Regular material for the reference plane.
	[SerializeField] private Material heightMapMaterial; 

	// Reads from the height map buffer render texture.
	[SerializeField] private Material heightMapMaterialVolumeBased; 

	[Header("Properties")]
	[SerializeField] private string lowerHeight = "_Bottom_Height";
	[SerializeField] private string targetHeightMin = "_Target_Height_Min";
	[SerializeField] private string targetHeightMax = "_Target_Height_Max";
	[SerializeField] private string upperHeight = "_Top_Height";

    void UpdateCurrentStatus() {


        if (levelingTool.currentMaterial != null)
        {
            CurrentStatus.lowerThreshhold = levelingTool.currentMaterial.GetFloat(lowerHeight);
            CurrentStatus.upperThreshhold = levelingTool.currentMaterial.GetFloat(upperHeight);
            CurrentStatus.TargetMinThreshhold = levelingTool.currentMaterial.GetFloat(targetHeightMin);
            CurrentStatus.TargetMaxThreshhold = levelingTool.currentMaterial.GetFloat(targetHeightMax);
        }
        else {
            CurrentStatus.lowerThreshhold = heightMapMaterial.GetFloat(lowerHeight);
            CurrentStatus.upperThreshhold = heightMapMaterial.GetFloat(upperHeight);
            CurrentStatus.TargetMinThreshhold = heightMapMaterial.GetFloat(targetHeightMin);
            CurrentStatus.TargetMaxThreshhold = heightMapMaterial.GetFloat(targetHeightMax);
        }
    }
	void OnEnable () {

		// Set current values.
		if (levelingTool.currentMaterial != null) {

			lowerInputField.text = levelingTool.currentMaterial.GetFloat (lowerHeight).ToString ();
			targetMinInputField.text = levelingTool.currentMaterial.GetFloat (targetHeightMin).ToString ();
			targetMaxInputField.text = levelingTool.currentMaterial.GetFloat (targetHeightMax).ToString ();
			upperInputField.text = levelingTool.currentMaterial.GetFloat (upperHeight).ToString ();
		} 
		else {
		
			lowerInputField.text = heightMapMaterial.GetFloat (lowerHeight).ToString ();
			targetMinInputField.text = heightMapMaterial.GetFloat (targetHeightMin).ToString ();
			targetMaxInputField.text = heightMapMaterial.GetFloat (targetHeightMax).ToString ();
			upperInputField.text = heightMapMaterial.GetFloat (upperHeight).ToString ();
		}
        UpdateCurrentStatus();

    }

	/// <summary>
	/// Tries to parse a float value from a string. If parse is ok, tries to set value for material property.
	/// If we are able to parse a height value from input field, set values in the actual material assets.
	/// If parse was ok and current leveling tool material is not null, also update the current scan object's instance material.
	/// </summary>
	/// <param name="valueText">Input field text to parse.</param>
	/// <param name="property">The name of the shader property to set.</param>
	private void TryPropertySet (string valueText, string property) {

		float val = 0;
		bool parseOk = false;
		if (float.TryParse (valueText, out val)) {
			parseOk = true;
		}

		if (parseOk) {


            
            heightMapMaterial.SetFloat (property, val);
			heightMapMaterialVolumeBased.SetFloat (property, val);

			if (levelingTool.currentMaterial != null) {
				levelingTool.currentMaterial.SetFloat (property, val);
			} else {
				Debug.LogWarning ("Unable to set " + val + " as value for " + property + " because current material is null.");
			}

            // everytime the parse is set as a proper value, update the currentStatus with the threshhold values.
            UpdateCurrentStatus();
        } 
		else {
			Debug.LogWarning ("Unable to parse " + valueText + " to float value.");
		}

		#if UNITY_EDITOR
		if (Application.isEditor) {
			UnityEditor.EditorUtility.SetDirty(heightMapMaterial);
			UnityEditor.EditorUtility.SetDirty(heightMapMaterialVolumeBased);
			UnityEditor.AssetDatabase.Refresh();
		}
		#endif
	}

	public void LowerValueUpdated (string valueText) {
		TryPropertySet (valueText, lowerHeight);
	}

	public void MinTargetValueUpdated (string valueText) {
		TryPropertySet (valueText, targetHeightMin);
	}

	public void MaxTargetValueUpdated (string valueText) {
		TryPropertySet (valueText, targetHeightMax);
	}

	public void UpperValueUpdated (string valueText) {
		TryPropertySet (valueText, upperHeight);
	}
}








