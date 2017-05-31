using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationWidget : MonoBehaviour {

	private Transform cachedTransform;

	void Awake () {

		cachedTransform = transform;
	}

	void LateUpdate () {

		cachedTransform.rotation = Quaternion.identity;	// Keep global rotation locked.
	}
}
