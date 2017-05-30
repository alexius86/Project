using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class EditModeDisplay : MonoBehaviour {

	public LevelingTool levelingTool;

	public GameObject displayRoot;
	public TextMeshProUGUI planeLabel;

	void OnEnable () {

		levelingTool.CamberPlaneCreated += CamberModeEnter;
		levelingTool.ReferencePlaneCreated += ReferenceModeEnter;
		levelingTool.LevelingToolExit += OnLevelingToolExit;
	}

	void OnDisable () {

		levelingTool.CamberPlaneCreated -= CamberModeEnter;
		levelingTool.ReferencePlaneCreated -= ReferenceModeEnter;
		levelingTool.LevelingToolExit -= OnLevelingToolExit;
	}

	private void CamberModeEnter (float y) {

		displayRoot.SetActive (true);
		planeLabel.text = "Camber Plane";
	}

	private void ReferenceModeEnter (Transform t) {

		displayRoot.SetActive (true);
		planeLabel.text = "Reference Plane";
	}

	private void OnLevelingToolExit () {

		displayRoot.SetActive (false);
	}
}
