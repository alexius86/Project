using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CameraOrbitRing))]
public class CameraOrbitRingEditor : Editor {

	void OnSceneGUI () {
	
		CameraOrbitRing orbitRing = target as CameraOrbitRing;
	
		Handles.color = orbitRing.ringColor;
		Handles.DrawWireArc(orbitRing.origin + (Vector3.up * orbitRing.height), Vector3.up, -orbitRing.transform.right,
			360, orbitRing.radius );
	}
}
#endif