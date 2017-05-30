using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Cinemachine;

public class StaticCamera : MonoBehaviour {

	[Header("Cinemachine")]
	[SerializeField] private CinemachineVirtualCamera virtualCamera;
	[SerializeField] private Transform composerTarget;
	[SerializeField] private Transform transposerTarget;

	[Header("Other")]
	[SerializeField] private string collisionPlaneLayerName = "CollisionPlane";

	public void Set (Vector3 position, Vector3 eulerAngles) {

		SetPosition (position);
		SetRotation (eulerAngles);
	}

	private void SetPosition (Vector3 position) {

		transposerTarget.position = position;
	}

	private void SetRotation (Vector3 eulerAngles) {

		GameObject angleTest = new GameObject ();
		angleTest.name = "Camera Angle";
		angleTest.SetActive (false);
		Destroy (angleTest, 2.0f);

		Transform angleTestTransform = angleTest.transform;
		angleTestTransform.position = Camera.main.transform.position;	//TODO: Transposer target position is offset along Y for some reason..
		angleTestTransform.eulerAngles = eulerAngles;
		Debug.DrawRay (angleTestTransform.position, angleTestTransform.forward, Color.blue, 15.0f);

		// Raycast from angle test transform (tranposer target position, with desired rotation) towards scene's collision plane.
		RaycastHit hitInfo = new RaycastHit();
		if (Physics.Raycast (angleTestTransform.position,angleTestTransform.forward, out hitInfo, Mathf.Infinity, LayerMask.NameToLayer (collisionPlaneLayerName))) {

			// Get collision point. Since collision plane is below where our camera targets will be placed, move the collision point Y up to composer target Y.
			Vector3 hitPos = hitInfo.point;
			hitPos.y = composerTarget.position.y;

			Debug.DrawRay (angleTestTransform.position, hitPos - angleTestTransform.position, Color.green, 10.0f);

			// Update composer target. Camera will automatically rotate to face new target point. 
			composerTarget.position = hitPos;
		}
	}
}














