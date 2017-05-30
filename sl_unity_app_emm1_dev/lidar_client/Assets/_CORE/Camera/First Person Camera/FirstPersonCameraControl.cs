using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Cinemachine;

public class FirstPersonCameraControl : MonoBehaviour {

	[Header("Cinemachine")]
	[SerializeField] private CinemachineVirtualCamera virtualCamera;
	[SerializeField] private Transform composerTarget;
	[SerializeField] private Transform transposerTarget;

	[Header("View")]
	[SerializeField] private float cameraHeight = 1.5f;
	[SerializeField] private float viewDistance = 1.0f;	// Distance from camera to look at target (Transposer to Composer).

	[Header("Speed")]
	[SerializeField] private float moveSpeed = 1.0f;
	[SerializeField] private float strafeSpeed = 1.0f;
	[SerializeField] private float turnSpeed = 1.0f;
	[SerializeField] private float lookSpeed = 1.0f;

	[Header("Debug")]
	[SerializeField] private bool activateOnStart = false;
	[SerializeField] private Vector3 debugStartPosition;

	// True if user is in first person mode.
	private bool isActivated = false;

	// Min and max heights for composer look at target. Used limiting look up and down.
	private float minComposerY;
	private float maxComposerY;

	void Start () {

		if (activateOnStart) {
			Activate (debugStartPosition);
		}
	}

	void Update () {

		#region Keyboard Controls
		if (Input.GetKey(KeyCode.W)) {
			MoveForward();
		}
		if (Input.GetKey(KeyCode.S)) {
			MoveBackward();
		}
		if (Input.GetKey(KeyCode.A)) {
			StrafeLeft();
		}
		if (Input.GetKey(KeyCode.D)) {
			StrafeRight();
		}
		if (Input.GetKey(KeyCode.LeftArrow)) {
			TurnLeft();
		}
		if (Input.GetKey(KeyCode.RightArrow)) {
			TurnRight();
		}
		#endregion
	}

	public void Activate (Vector3 startPosition) {

		// Place camera at start position with height offset.
		transposerTarget.position = startPosition + Vector3.up * cameraHeight;

		// Place look at target in front of current camera position by view distance.
		composerTarget.position = transposerTarget.position + (transposerTarget.forward * viewDistance);

		// Set limits for looking up/down.
		minComposerY = startPosition.y;
		maxComposerY = transposerTarget.position.y + (2.0f * viewDistance);

		// First person camera is in use.
		virtualCamera.enabled = true;
		isActivated = true;
	}

	public void Deactivate () {
		
		// First person camera no longer in use.
		virtualCamera.enabled = false;
		isActivated = false;
	}

	#region Virtual Joystick Controls
	public void LeftStickMove (Vector2 move) {

		Strafe (move.x);
		Move (move.y);
	}

	public void RightStickMove (Vector2 move) {

		Turn (move.x);
		Look (move.y);
	}

	private void Move (float vertical) {

		Vector3 forward = transposerTarget.forward;
		float distance = moveSpeed * vertical * Time.deltaTime;

		composerTarget.position += forward * distance;
		transposerTarget.position += forward * distance;
	}

	private void Strafe (float horizontal) {

		Vector3 right = transposerTarget.right;
		float distance = strafeSpeed * horizontal * Time.deltaTime;

		composerTarget.position += right * distance;
		transposerTarget.position += right * distance;
	}

	private void Turn (float horizontal) {

		transposerTarget.Rotate (Vector3.up, turnSpeed * horizontal * Time.deltaTime);

		Vector3 composerTargetPos = composerTarget.position;
		float composerY = composerTargetPos.y;
		composerTargetPos = transposerTarget.position + (transposerTarget.forward * viewDistance);
		composerTargetPos.y = composerY;	// Keep original Y (look up/down).
		composerTarget.position = composerTargetPos;
	}

	private void Look (float vertical) {

		// Get composer target and apply virtual joystick input to move target up or down.
		Vector3 composerTargetPos = composerTarget.position;
		composerTargetPos.y += (lookSpeed * vertical * Time.deltaTime);

		// Keep composer target within our min/max bounds.
		composerTargetPos.y = Mathf.Clamp (composerTargetPos.y, minComposerY, maxComposerY);

		// Apply new composer target position.
		composerTarget.position = composerTargetPos;
	}
	#endregion

	#region Keyboard Controls
	private void MoveForward () {

		Vector3 forward = transposerTarget.forward;
		float distance = moveSpeed * Time.deltaTime;

		composerTarget.position += forward * distance;
		transposerTarget.position += forward * distance;
	}

	private void MoveBackward () {

		Vector3 forward = transposerTarget.forward;
		float distance = moveSpeed * Time.deltaTime;

		composerTarget.position -= forward * distance;
		transposerTarget.position -= forward * distance;
	}

	private void StrafeLeft () {

		Vector3 left = -transposerTarget.right;
		float distance = strafeSpeed * Time.deltaTime;

		composerTarget.position += left * distance;
		transposerTarget.position += left * distance;
	}

	private void StrafeRight () {

		Vector3 right = transposerTarget.right;
		float distance = strafeSpeed * Time.deltaTime;

		composerTarget.position += right * distance;
		transposerTarget.position += right * distance;
	}
		
	private void TurnLeft () {

		transposerTarget.Rotate (Vector3.up, -turnSpeed * Time.deltaTime);
		composerTarget.position = transposerTarget.position + (transposerTarget.forward * viewDistance);
	}

	private void TurnRight () {

		transposerTarget.Rotate (Vector3.up, turnSpeed * Time.deltaTime);
		composerTarget.position = transposerTarget.position + (transposerTarget.forward * viewDistance);
	}

	private void LookUp () {


	}

	private void LookDown () {


	}
	#endregion
}











