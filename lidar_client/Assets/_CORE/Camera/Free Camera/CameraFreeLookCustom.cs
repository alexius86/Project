using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Cinemachine;	// Camera Tool.
using Lean.Touch;	// Lean Touch (Gestures).
using DentedPixel;	// Lean Tween (Splines)
using com.ootii.Messages;	// Message Dispatcher.
using MoreMountains.Tools;	// Nice Touch (UI).

public class CameraFreeLookCustom : MonoBehaviour {

	#region Serialized Fields
	[Space(10.0f)]
	[SerializeField] private GameObject root;
	[SerializeField] private Slider rotationSlider;
	[SerializeField] private Slider heightSlider; 
	[SerializeField] private Slider zoomSlider;

	[Header("Limits")]
	[SerializeField] private float maxRadius = 50.0f;	// Radius value is used for zoom.
	[SerializeField] private float maxHeight = 25.0f;	// Y offset from current camera target position.
	[Range(0, 10)]
	[SerializeField] private int heightCurveResolution = 6;	// Height curve will have 3 control points (top, middle, bottom), but these are the stepping points within.

	[Header("Values")]
	[Range(0.0f, 1.0f)]
	[SerializeField] private float heightFactor;	// Current pan height as a factor of max height.
	[Range(0.0f, 1.0f)]
	[SerializeField] private float zoomFactor;		// Current orbit radius and zoom amount as a factor of max radius.

	[Header("Cinemachine")]
	[SerializeField] private CinemachineVirtualCamera virtualCamera;	// Use to get/set composer and transposer targets, as well as set any offset values.
	[SerializeField] private Transform composerTarget;		// Only moves along X/Z plane.
	[SerializeField] private Transform transposerTarget;	// Only moves along Y axis. Also orbits around composer target via Y axis.

	[Header("Orbit Rings")]
	[Range(0.0f, 1.0f)]

	[SerializeField] private float topOrbitRadiusFactor = 0.25f;	// How wide is top orbit ring as a factor of max radius?
	[Range(0.0f, 1.0f)]
	[SerializeField] private float middleOrbitRadiusFactor = 0.5f;	// How wide is middle orbit ring as a factor of max radius?
	[Range(0.0f, 1.0f)]
	[SerializeField] private float middleHeightFactor = 0.35f;		// At what height, as a factor of max height, is middle orbit ring located?
	[Range(0.0f, 1.0f)]
	[SerializeField] private float bottomOrbitRadiusFactor = 0.25f;	// How wide is bottom orbit ring as a factor of max radius?
	[Range(0.0f, 1.0f)]
	[SerializeField] private float bottomHeightFactor = 0.1f;	// The offset of bottom orbit ring from the camera target's Y position. Can use this to set a minimum Y value.
	[Space(10.0f)]
	[SerializeField] private Color ringColor = Color.white;

	[Header("UI")]
	[Tooltip("Ignore fingers that are over UI elements?")]
	[SerializeField] private bool ignoreFingersOverUI = true;

	[Header("Pan")]
	[SerializeField] private MMTouchDynamicJoystick panJoystick;
	[SerializeField] private Image panJoystickBacker;
	[SerializeField] private Image panJoystickKnob;
	[SerializeField] private float joystickPanSpeed = 2.0f;

	[Header("Height Gesture")]	
	[SerializeField] private bool heightGestureEnabled = false;
	[SerializeField] private float heightGestureSpeed = 1.0f;

	[Header("Rotation Gesture")]
	[SerializeField] private bool rotationGestureEnabled = true;
	[SerializeField] private float rotationSpeed = 5.0f;
	#endregion

	#region Private Fields
	// State.
	private Bounds currentContentBounds;	// Bounds for all loaded scans. If size is zero, no scans loaded and we shouldn't worry about doing certain processing.

	// Orbit rings.
	private CameraOrbitRing topOrbitRing;
	private CameraOrbitRing middleOrbitRing;
	private CameraOrbitRing bottomOrbitRing;

	// Spline.
	private LTSpline heightSpline;	// Spline that moves through top, middle, and bottom orbit rings at camera direction.
	#endregion

	#region Initialize
	void Awake () {

		// Initialize Cinemachine targets.
		composerTarget.position = Vector3.zero;
		virtualCamera.CameraComposerTarget = composerTarget;
		virtualCamera.CameraTransposerTarget = transposerTarget;

		// Add orbit rings.
		topOrbitRing = gameObject.AddComponent<CameraOrbitRing> ();
		middleOrbitRing = gameObject.AddComponent<CameraOrbitRing> ();
		bottomOrbitRing = gameObject.AddComponent<CameraOrbitRing> ();

		// Default camera distance to orbit radius of middle orbit ring.
		zoomFactor = middleOrbitRadiusFactor;

		// Default camera height to middle orbit ring height.
		heightFactor = middleHeightFactor;

		currentContentBounds = new Bounds (Vector3.zero, Vector3.zero);

		RefreshAll ();
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.loaded_scans_refreshed, ScanLayoutChanged);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.loaded_scans_refreshed, ScanLayoutChanged);
	}

	public void Toggle (bool value) {
		root.SetActive (value);
	}
	#endregion

	#region Update
	void Update () {

		if (currentContentBounds.size == Vector3.zero) {
			return;
		}

		RefreshAll ();
	}
		
	void LateUpdate () {

		if (currentContentBounds.size == Vector3.zero) {
			return;
		}

		// Handle single finger active.
		List<LeanFinger> singleFingers = LeanTouch.GetFingers (true, 1);
		if (singleFingers != null) {
			CheckPan (singleFingers);
		}

		// Handle two fingers active.
		List<LeanFinger> twoFingers = LeanTouch.GetFingers (ignoreFingersOverUI, 2);
		if (twoFingers != null) {

			Vector2 firstFingerDirection = twoFingers [0].ScreenDelta.normalized;
			Vector2 secondFingerDirection = twoFingers [1].ScreenDelta.normalized;

			// Dot product of both finger directions will be 1.0f if they are moving in exactly the same direction.
			// We should have some tolerance threshold for this that is close to 1.0f
			float sameDirectionThreshold = 0.9f;

			// If fingers are moving in same direction, do rotate.
			if (Vector2.Dot (firstFingerDirection, secondFingerDirection) > sameDirectionThreshold) {

				// Rotate.
				CheckRotationGesture (twoFingers);
			} 
			else {	// Otherwise fingers are treated as pinch, so do height zoom.
				CheckHeightGesture(twoFingers);
			}	
		}

		// Create the spline to follow for height adjustment.
		GenerateHeightCurve();

		// Place transposer target at correct point on spline for current height factor.
		heightSpline.place(transposerTarget, heightFactor);
	}
	#endregion

	#region Zoom/Rotate/Pan
	/// <summary>
	/// Rotates camera around the composer target's current position.
	/// </summary>
	private void CheckRotationGesture (List<LeanFinger> fingers) {

		if (rotationGestureEnabled) {

			// Finger will be null if we don't have exactly one touch.
			if (fingers != null) {

				Vector2 fingerDelta = LeanGesture.GetScaledDelta (fingers);	// Scaled delta is screen size independent.
				rotationSlider.value += (fingerDelta.x * rotationSpeed * Time.deltaTime);
			}
		}
	}

	/// <summary>
	/// Checks for pinch zoom and adjusts camera height accordingly.
	/// </summary>
	private void CheckHeightGesture (List<LeanFinger> fingers) {

		if (heightGestureEnabled) {

			// Fingers will be null if we don't have the correct number of fingers.
			if (fingers != null) {

				// If actually pinching with two fingers, ratio will be above or below 1.0 depending on pinch "in" vs "out".
				float pinchRatio = LeanGesture.GetPinchRatio (fingers);

				// Since another camera control (like camera pan) could be using the same number of fingers, we should ignore
				// current fingers if they are not actually pinching very much.
				if (pinchRatio != 1.0f) {

					//Debug.Log ("Height pinch ratio: " + pinchRatio);

					// Pinch ratio is < 1.0 for pinch out and > 1.0 for pinch in.
					float pinchFactor = 1.0f - pinchRatio;

					// Scale the current value based on the pinch ratio.
					heightFactor += pinchFactor * heightGestureSpeed * Time.deltaTime;

					// Clamp the zoom factor to keep in normalized range.
					heightFactor = Mathf.Clamp01 (heightFactor); 

					SetNormalizedHeight(heightFactor);	// Updates slider and whatever else as well.
				}
			}
		}
	}

	public void JoystickPan (Vector2 movement) {

		if (!panJoystickKnob.gameObject.activeSelf)
			return;

		Transform cameraTransform = Camera.main.transform;
		Vector3 right = cameraTransform.right;
		Vector3 forward = cameraTransform.forward;

		// Project forward and right vectors on horizontal plane (Y = 0).
		right.y = 0.0f;
		right.Normalize ();
		forward.y = 0.0f;
		forward.Normalize();

		Vector3 delta = (right * movement.x * joystickPanSpeed) + (forward * movement.y * joystickPanSpeed);

		// Apply movement to composer target.
		composerTarget.position += (delta * Time.deltaTime);

		// Apply movement to transposer target.
		transposerTarget.position += (delta * Time.deltaTime);
	}

	public void HidePanJoystick () {

		panJoystickBacker.gameObject.SetActive (false);
		panJoystickKnob.gameObject.SetActive(false);
	}
		
	private bool isPanning = false;
	private void CheckPan (List<LeanFinger> fingers) {

		// Pan fingers will be null if we don't have the correct number of fingers.
		if (fingers != null) {

			// Get finger position on screen (center of all fingers).
			Vector2 pixelPos = LeanGesture.GetScreenCenter (fingers);
			//Debug.Log ("Finger down position (pixels): " + pixelPos.ToString ());

			if (fingers [0].Down) {

				panJoystickBacker.gameObject.SetActive (true);
				panJoystickKnob.gameObject.SetActive (true);

				// Move the dynamic joystick's detection area.
				panJoystick.transform.position = pixelPos;
				panJoystickBacker.transform.position = pixelPos;

				panJoystick.MoveDynamicJoystickTo (pixelPos);	// Move the entire stick (background and all).

				isPanning = true;
			} 
			else if (isPanning) {
				
				panJoystick.MoveStickTowards (pixelPos);		// Move just the knob within the newly repositioned stick.

				if (fingers [0].Up || fingers[0].HeldUp) {
					
					HidePanJoystick ();
					panJoystick.OnEndDrag (null);
					isPanning = false;
				}
			}

			//NOTE: Finger held logic for joystick is handled by JoystickPan function.
		}
	}
	#endregion

	/// <summary>
	/// Grow or shrink the radius of the curved cylinder that camera moves along the outside of.
	/// </summary>
	public void SetNormalizedZoom (float normalizedValue) {

		zoomFactor = normalizedValue;

		if (zoomSlider != null) {
			zoomSlider.value = normalizedValue;
		}
	}

	public void SetNormalizedHeight (float normalizedValue) {
		
		heightFactor = normalizedValue;

		if (heightSlider != null) {
			heightSlider.value = normalizedValue;
		}
	}

	public void SetNormalizedRotation (float normalizedValue) {

		Vector3 composerPos = composerTarget.position;
		Vector3 transposerPos = transposerTarget.position;
		composerPos.y = 0.0f;
		transposerPos.y = 0.0f;

		Vector3 direction = composerPos - transposerPos;
		float distance = direction.magnitude;

		// Move tranposer target to default orbit rotation starting point.
		transposerTarget.position = composerTarget.position + (Vector3.right * distance);

		// Now do the orbit around composer target by normalized value.
		transposerTarget.RotateAround(composerTarget.position, Vector3.up, (normalizedValue * 360.0f) - 90.0f);

		if (rotationSlider != null) {
			rotationSlider.value = normalizedValue;
		}

		if (AssetBundleLoader.Instance.LoadedScanCount > 0) {
			RefreshAll ();
		}
	}

	/// <summary>
	/// Reset camera to default settings, but keep current target.
	/// </summary>
	public void Reset (IMessage message = null) {
		EncapsulateContent ();
	}

	private void ScanLayoutChanged (IMessage message) {

		currentContentBounds = (Bounds)(message.Data);
		if (currentContentBounds.size != Vector3.zero) {
			
			GenerateHeightCurve ();
			EncapsulateContent ();
		}
	}

	/// <summary>
	/// Increases the max radius and max height to fit all currently loaded scans.
	/// </summary>
	private void EncapsulateContent () {

		// Increase or decrease max radius and max height based size of combined bounds.
		maxRadius = currentContentBounds.extents.magnitude * 2.5f;
		maxHeight = maxRadius * 1.5f;

		// Move camera. If camera is already in the middle of a move, stop it before running this move.
		//SetFreePosition (new Vector3(currentContentBounds.center.x, transposerTarget.position.y, currentContentBounds.center.z));

		// Place composer (look at) target at middle point of combined bounds.
		composerTarget.position = new Vector3 (currentContentBounds.center.x, composerTarget.position.y, currentContentBounds.center.z);

		zoomFactor = 0.65f;	// Mostly zoomed out.	
		SetNormalizedHeight (0.25f);	// Higher up. Also updates slider.

		if (AssetBundleLoader.Instance.LoadedScanCount > 0) {
			SetNormalizedRotation (0.5f); // Facing +Z
		}
	}

	/// <summary>
	/// Just pans the free look composer and transposer targets to the desired point.
	/// </summary>
	/// <param name="position">Position.</param>
	private void SetFreePosition (Vector3 position) {

		// Get direction from composer target to transposer target.
		Vector3 composerPos = composerTarget.position;
		composerPos.y += 0.0f;
		Vector3 transposerPos = transposerTarget.position;
		transposerPos.y = 0.0f;

		// Get direction from composer target to transposer target (which now ignores Y position).
		Vector3 cameraDirection = composerPos - transposerPos;

		// Transposer target goes straight to desired position.
		transposerTarget.position = position;

		// Set composer to desired position, but keep Y. Also offset by previous direction vector so angle stays the same.
		composerTarget.position = new Vector3 (position.x, composerTarget.position.y, position.z) - cameraDirection;
	}

	void OnDrawGizmos () {

		// Draw spline.
		Gizmos.color = ringColor;
		if (heightSpline != null) {
			heightSpline.gizmoDraw (); // To Visualize the path, use this method.
		}
	}

	/// <summary>
	/// Create a spline curve through all three orbit rings at current camera direction + distance from composer target.
	/// </summary>
	private void GenerateHeightCurve () {

		// Get the points on each ring in the direction of camera at the current zoom level.
		List<Vector3> ringPoints = new List<Vector3> ();

		// Get direction from composer target to transposer target (ignore Y).
		Vector3 cameraDirection = GetCameraDirection(true);

		ringPoints.Add (composerTarget.position - (cameraDirection * topOrbitRing.radius) + (Vector3.up * maxHeight));
		ringPoints.Add (composerTarget.position - (cameraDirection * middleOrbitRing.radius) + (Vector3.up * middleHeightFactor * maxHeight));
		ringPoints.Add (composerTarget.position - (cameraDirection * bottomOrbitRing.radius) + (Vector3.up * bottomHeightFactor * maxHeight));

		// Create a new spline using control points.
		Vector3[] splinePoints = new Vector3[5];
		splinePoints [0] = ringPoints [0];	// First point is control point.
		splinePoints [1] = ringPoints [0];
		splinePoints [2] = ringPoints [1];
		splinePoints [3] = ringPoints [2];
		splinePoints [4] = ringPoints [2];	// Last point is control point.

		// Set global resolution of spline.
		LTSpline.SUBLINE_COUNT = heightCurveResolution;

		// Create the new spline using points and control points.
		heightSpline = new LTSpline (splinePoints, true);
	}

	/// <summary>
	/// Update system values in case inspector values changed.
	/// </summary>
	private void RefreshAll () {

		RefreshOrbitRings ();
		RefreshTransposerTarget ();
	}

	/// <summary>
	/// Make sure orbit rings are up to date. 
	/// </summary>
	private void RefreshOrbitRings () {

		topOrbitRing.ringColor = ringColor;
		topOrbitRing.origin = composerTarget.position;
		topOrbitRing.height = maxHeight;
		topOrbitRing.radius = maxRadius * zoomFactor * topOrbitRadiusFactor;

		middleOrbitRing.ringColor = ringColor;
		middleOrbitRing.origin = composerTarget.position;
		middleOrbitRing.height = maxHeight * middleHeightFactor;
		middleOrbitRing.radius = maxRadius * zoomFactor * middleOrbitRadiusFactor;

		bottomOrbitRing.ringColor = ringColor;
		bottomOrbitRing.origin = composerTarget.position;
		bottomOrbitRing.height = maxHeight * bottomHeightFactor;
		bottomOrbitRing.radius = maxRadius * zoomFactor * bottomOrbitRadiusFactor;
	}

	/// <summary>
	/// Take inspector settings and update transposer target transform such that
	/// zoom/orbit distance and pan height are reflected.
	/// </summary>
	private void RefreshTransposerTarget () {

		// Get direction from composer target to transposer target (ignore Y).
		Vector3 cameraDirection = GetCameraDirection(true);

		// Move transposer target away from composer target first, then set the correct height.
		Vector3 transposerTargetPos = composerTarget.position - (cameraDirection * zoomFactor * maxRadius);

		// Update current height.
		transposerTargetPos.y = composerTarget.position.y + (heightFactor * maxHeight);
		transposerTarget.position = transposerTargetPos;
	}

	/// <summary>
	/// Get direction from composer target (what we're looking at) to transposer target (where
	/// the camera is). If ignoreY = true, composer and transposer positions are set to same Y 
	/// value before direction check is made. This is useful if you want a direction away from a
	/// point at a specific height.
	/// </summary>
	private Vector3 GetCameraDirection (bool ignoreY = false) {

		if (!ignoreY) {
			return (composerTarget.position - transposerTarget.position).normalized;
		} 
		else {

			float actualCameraHeight = heightFactor * maxHeight;

			// Update current zoom (orbit distance).	
			// Get position of composer and transposer targets and level them out at the desired height.
			Vector3 composerPos = composerTarget.position;
			composerPos.y += 0.0f;
			Vector3 transposerPos = transposerTarget.position;
			transposerPos.y = 0.0f;

			// Get direction from composer target to transposer target (which now ignores Y position).
			return (composerPos - transposerPos).normalized;
		}
	}
}



















