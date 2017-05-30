using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour {

	public Transform needleTransform;
	public Vector3 northDirection = Vector3.forward;
	public Slider rotationSlider;

	private Transform cameraTransform;
	private Transform temp;

	void Awake () {

		cameraTransform = Camera.main.transform;
		temp = new GameObject ().transform;
		temp.name = "Compass - Working Transform";
	}

	void Update () {

		Vector3 needleEuler = needleTransform.eulerAngles;
		needleEuler.z = (rotationSlider.value * 360.0f) - 180.0f;
		needleTransform.eulerAngles = needleEuler;
	}
}











