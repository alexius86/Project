using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

using MoreMountains.NiceTouch;
using MoreMountains.Tools;

using TMPro;

public class ContextSensitivePlaneEditing : MonoBehaviour {

	[Header("Leveling Tool")]
	[SerializeField] private LevelingTool levelingTool;
	[SerializeField] private Color referenceControlColor = Color.green;
	[SerializeField] private Color camberControlColor = Color.red;

	[Header("Transform Buttons")]
	[SerializeField] private Button translateButton;
	[SerializeField] private Button rotateButton;
	[SerializeField] private Button scaleButton;
	[Space(5.0f)]
	[SerializeField] private Image translateButtonImage;
	[SerializeField] private Image scaleButtonImage;
	[SerializeField] private Image rotateButtonImage;

	[Header("Control Areas")]
	[SerializeField] private GameObject leftControlsContainer;	// Root control object.
	[SerializeField] private GameObject rightControlsContainer;
	[SerializeField] private GameObject transformControls;	// Translate, rotate, scale.
	[Space(5.0f)]
	[SerializeField] private Image leftControlsBacker;	// Background inlay of control area to tint color of.
	[SerializeField] private Image rightControlsBacker;

	[Header("Control Mounts")]
	[SerializeField] private GameObject leftJoystickMount;
	[SerializeField] private GameObject leftSliderMount;
	[SerializeField] private GameObject rightSliderMount;

	[Header("Controls")]
	[SerializeField] private MMTouchJoystick leftJoystick;
	[SerializeField] private MMTouchJoystick leftSlider;
	[SerializeField] private MMTouchJoystick rightSlider;
	[Space(5.0f)]
	[SerializeField] private Image leftSliderBacker;
	[SerializeField] private Image leftSliderHandle;
	[SerializeField] private Image rightSliderBacker;
	[SerializeField] private Image rightSliderHandle;

	[Header("Labels")]
	[SerializeField] private TextMeshProUGUI mainLabelLeft;		// Eg.) "Scale Plane X"
	[SerializeField] private TextMeshProUGUI mainLabelRight;	// Eg.) "Translate Plane XZ"
	[Space(5.0f)]
	[SerializeField] private TextMeshProUGUI infoLabelLeft;		// The readout for fields that are being manipulated by the control.
	[SerializeField] private TextMeshProUGUI infoLabelRight;	// Eg.) "X: 12.0 Z: 4.5"

	[Header("Icons")]
	[SerializeField] private Image leftJoystickBacker;
	[SerializeField] private Sprite referenceJoystickBackerSprite;
	[SerializeField] private Sprite camberJoystickBackerSprite;
	[Space(5.0f)]
	[SerializeField] private Image leftControlIcon;
	[SerializeField] private Image rightControlIcon;
	[Space(5.0f)]
	[SerializeField] private Sprite xRotateSprite;
	[SerializeField] private Sprite zRotateSprite;
	[SerializeField] private Sprite xScaleSprite;
	[SerializeField] private Sprite zScaleSprite;
	[SerializeField] private Sprite translateSprite;
	[SerializeField] private Sprite translateYSprite;
	[Space(5.0f)]
	[SerializeField] private Sprite rotateButtonSprite;
	[SerializeField] private Sprite translateButtonSprite;
	[SerializeField] private Sprite scaleButtonSprite;
	[SerializeField] private Sprite rotateButtonSpriteSelected;
	[SerializeField] private Sprite translateButtonSpriteSelected;
	[SerializeField] private Sprite scaleButtonSpriteSelected;
	[SerializeField] private Sprite rotateButtonSpriteDisabled;

	public enum Context {
		None, Translate, Rotate, Scale,
	}

	private Context currentContext;
	private LevelingToolEditMode editMode;

	#region Initialize
	void Awake () {

		currentContext = Context.None;
		editMode = LevelingToolEditMode.Disabled;
	}

	void OnEnable () {

		translateButton.onClick.AddListener(TranslateSelected);
		rotateButton.onClick.AddListener(RotateSelected);
		scaleButton.onClick.AddListener(ScaleSelected);

		MessageDispatcher.AddListener (MessageDatabase.levelingToolEditModeChanged, OnLevelingToolModeChange);
	}

	void OnDisable () {

		translateButton.onClick.RemoveListener(TranslateSelected);
		rotateButton.onClick.RemoveListener(RotateSelected);
		scaleButton.onClick.RemoveListener(ScaleSelected);
	
		MessageDispatcher.RemoveListener (MessageDatabase.levelingToolEditModeChanged, OnLevelingToolModeChange);
	}
	#endregion

	#region Readout UI Update
	// Refresh On Input.
	private void UpdateTranslationReadout (Vector2 input) {

		// Ignore callbacks when there is no input.
		if (input == Vector2.zero)
			return;

		UpdateTranslationReadout ();
	}

	// Manual Refresh.
	private void UpdateTranslationReadout () {

		Transform plane = levelingTool.CurrentTransform;
		infoLabelLeft.text = "X:\n" + plane.position.x.ToString("0.00") + "<size=12>m</size>" + "\nZ:\n" + plane.position.z.ToString("0.00") + "<size=12>m</size>";
		infoLabelRight.text = "Y:\n" + plane.position.y.ToString("0.00") + "<size=12>m</size>";
	}

	// Refresh On Input.
	private void UpdateRotationReadout (Vector2 input) {

		// Ignore callbacks when there is no input.
		if (input == Vector2.zero)
			return;

		UpdateRotationReadout ();
	}

	// Manual Refresh.
	private void UpdateRotationReadout () {

		Transform plane = levelingTool.CurrentTransform;
		infoLabelRight.text = "X:\n" + plane.eulerAngles.x.ToString("0.00") + "<size=12>°</size>";
		infoLabelLeft.text = "Z:\n" + plane.eulerAngles.z.ToString("0.00") + "<size=12>°</size>";
	}

	// Refresh On Input.
	private void UpdateScaleReadout (Vector2 input) {

		// Ignore callbacks when there is no input.
		if (input == Vector2.zero)
			return;

		UpdateScaleReadout ();
	}

	// Manual Refresh.
	private void UpdateScaleReadout () {

		Transform plane = levelingTool.CurrentTransform;
		infoLabelLeft.text = "X:\n" + plane.localScale.x.ToString("0.00") + "<size=12>m</size>";
		infoLabelRight.text = "Z:\n" + plane.localScale.z.ToString("0.00") + "<size=12>m</size>";
	}
	#endregion

	//TODO: This function could use a refactor. Lots of duplicated code.
	private void UpdateContext (LevelingToolEditMode mode, Context context) {

		// If first time edit mode is entered, default to translate controls.
		if (editMode == LevelingToolEditMode.Disabled && currentContext == Context.None) {
			currentContext = Context.Translate;
			editMode = mode;
		} 
		else {
			editMode = mode;
			currentContext = context;
		}

		// Reset UI before enabling only the ones we need.
		ClearAll ();

		// Camber plane rotate is not supported. Disable rotate button.
		rotateButton.interactable = mode != LevelingToolEditMode.CamberPlane;

		// If context was rotate in reference plane edit mode, set it to translate when camber mode is entered.
		// Rotation of camber plane is not supported.
		if (editMode == LevelingToolEditMode.CamberPlane && currentContext == Context.Rotate) {
			currentContext = Context.Translate;
		}

		// Toggle transform edit icons to match new context.
		translateButtonImage.sprite = currentContext == Context.Translate ? translateButtonSpriteSelected : translateButtonSprite;
		rotateButtonImage.sprite = currentContext == Context.Rotate ? rotateButtonSpriteSelected : rotateButtonSprite;
		scaleButtonImage.sprite = currentContext == Context.Scale ? scaleButtonSpriteSelected : scaleButtonSprite;

		leftControlsBacker.gameObject.SetActive (editMode != LevelingToolEditMode.Disabled);
		rightControlsBacker.gameObject.SetActive (editMode != LevelingToolEditMode.Disabled);

		#region Reference Plane Edit
		if (editMode == LevelingToolEditMode.ReferencePlane) {

			// Show main control backer windows.
			leftControlsContainer.SetActive (true);
			rightControlsContainer.SetActive (true);
			transformControls.SetActive (true);

			SetColor(referenceControlColor);
			leftJoystickBacker.sprite = referenceJoystickBackerSprite;	// Color is still baked into the joystick sprites. 

			//TODO: Make UI sprites the reference color.

			if (currentContext == Context.Translate) {

				// Set header label.
				mainLabelLeft.text = "Translate XZ";
				mainLabelRight.text = "Translate Y";

				// Set icons.
				leftControlIcon.sprite = translateSprite;
				rightControlIcon.sprite = translateYSprite;

				// Enable left joystick and right slider.
				leftJoystickMount.SetActive (true);
				rightSliderMount.SetActive (true);

				// Set functions to handle input from joystick and slider.
				leftJoystick.JoystickValue.AddListener (levelingTool.MoveHorizontally);
				rightSlider.JoystickValue.AddListener (levelingTool.MoveVertically);

				// Set functions to update readout display UI for changes.
				leftJoystick.JoystickValue.AddListener (UpdateTranslationReadout);
				rightSlider.JoystickValue.AddListener (UpdateTranslationReadout);

				UpdateTranslationReadout();
			} 
			else if (currentContext == Context.Rotate) {

				// Set header label.
				mainLabelLeft.text = "Rotate Z";
				mainLabelRight.text = "Rotate X";

				// Set icons.
				leftControlIcon.sprite = xRotateSprite;
				rightControlIcon.sprite = zRotateSprite;

				// Enable left joystick and right slider.
				leftSliderMount.SetActive (true);
				rightSliderMount.SetActive (true);

				// Set functions to handle input from joystick and slider.
				leftSlider.JoystickValue.AddListener (levelingTool.RotatePlaneX);
				rightSlider.JoystickValue.AddListener (levelingTool.RotatePlaneZ);

				// Set functions to update readout display UI for changes.
				leftSlider.JoystickValue.AddListener (UpdateRotationReadout);
				rightSlider.JoystickValue.AddListener (UpdateRotationReadout);

				UpdateRotationReadout();
			}
			else if (currentContext == Context.Scale) {

				// Set header label.
				mainLabelLeft.text = "Scale X";
				mainLabelRight.text = "Scale Z";

				// Set icons.
				leftControlIcon.sprite = xScaleSprite;
				rightControlIcon.sprite = zScaleSprite;

				// Enable left joystick and right slider.
				leftSliderMount.SetActive (true);
				rightSliderMount.SetActive (true);

				// Set functions to handle input from joystick and slider.
				leftSlider.JoystickValue.AddListener (levelingTool.ScaleX);	// Horizontal
				rightSlider.JoystickValue.AddListener (levelingTool.ScaleZ);	// Vertical

				// Set functions to update readout display UI for changes.
				leftSlider.JoystickValue.AddListener (UpdateScaleReadout);
				rightSlider.JoystickValue.AddListener (UpdateScaleReadout);

				UpdateScaleReadout();
			}
		}
		#endregion

		#region Camber Plane Edit
		else if (editMode == LevelingToolEditMode.CamberPlane) {

			// Show main control backer windows.
			leftControlsContainer.SetActive (true);
			rightControlsContainer.SetActive (true);
			transformControls.SetActive (true);

			SetColor(camberControlColor);
			leftJoystickBacker.sprite = camberJoystickBackerSprite;	// Color is still baked into the joystick sprites. 

			// Rotation control disabled in camber edit mode.
			rotateButtonImage.sprite = rotateButtonSpriteDisabled;

			if (currentContext == Context.Translate) {

				// Set header label.
				mainLabelLeft.text = "Translate XZ";
				mainLabelRight.text = "Translate Y";

				// Set icons.
				leftControlIcon.sprite = translateSprite;
				rightControlIcon.sprite = translateYSprite;

				// Enable left joystick and right slider.
				leftJoystickMount.SetActive (true);
				rightSliderMount.SetActive (true);

				// Set functions to handle input from joystick and slider.
				leftJoystick.JoystickValue.AddListener (levelingTool.MoveHorizontally);
				rightSlider.JoystickValue.AddListener (levelingTool.MoveVertically);

				// Set functions to update readout display UI for changes.
				leftJoystick.JoystickValue.AddListener (UpdateTranslationReadout);
				rightSlider.JoystickValue.AddListener (UpdateTranslationReadout);

				UpdateTranslationReadout();
			}
			else if (currentContext == Context.Scale) {

				// Set header label.
				mainLabelLeft.text = "Scale X";
				mainLabelRight.text = "Scale Z";

				// Set icons.
				leftControlIcon.sprite = xScaleSprite;
				rightControlIcon.sprite = zScaleSprite;

				// Enable left joystick and right slider.
				leftSliderMount.SetActive (true);
				rightSliderMount.SetActive (true);

				// Set functions to handle input from joystick and slider.
				leftSlider.JoystickValue.AddListener (levelingTool.ScaleX);
				rightSlider.JoystickValue.AddListener (levelingTool.ScaleZ);

				// Set functions to update readout display UI for changes.
				leftSlider.JoystickValue.AddListener (UpdateScaleReadout);
				rightSlider.JoystickValue.AddListener (UpdateScaleReadout);

				UpdateScaleReadout();
			}
		}
		#endregion

		#region Exit Edit Mode
		else if (editMode == LevelingToolEditMode.Disabled) {
			ClearAll ();
		}
		#endregion
	}

	private void SetColor (Color c) {

		leftControlsBacker.color = c;
		rightControlsBacker.color = c;

		leftControlIcon.color = c;
		rightControlIcon.color = c;

		mainLabelLeft.color = c;
		mainLabelRight.color = c;

		infoLabelLeft.color = c;
		infoLabelRight.color = c;

		leftSliderBacker.color = c;
		rightSliderBacker.color = c;
		leftSliderHandle.color = c;
		rightSliderHandle.color = c;

		translateButtonImage.color = c;
		rotateButtonImage.color = c;
		scaleButtonImage.color = c;
	}

	// Removes all listeners from all controls and hides all controls.
	private void ClearAll () {
		
		// Remove any listeners from controls.
		leftJoystick.JoystickValue.RemoveAllListeners();
		leftSlider.JoystickValue.RemoveAllListeners();
		rightSlider.JoystickValue.RemoveAllListeners();
	
		// Hide individual controls.
		leftJoystickMount.SetActive (false);
		leftSliderMount.SetActive (false);
		rightSliderMount.SetActive (false);

		// Hide main control backers.
		leftControlsContainer.SetActive (false);
		rightControlsContainer.SetActive (false);
		transformControls.SetActive (false);
	}

	#region Context Change Triggers
	private void OnLevelingToolModeChange (IMessage message) {

		LevelingToolEditMode mode = (LevelingToolEditMode)(message.Data);
		UpdateContext (mode, currentContext);
	}
		
	private void TranslateSelected () {

		UpdateContext (editMode, Context.Translate);
	}

	private void RotateSelected () {

		UpdateContext (editMode, Context.Rotate);
	}

	private void ScaleSelected () {

		UpdateContext (editMode, Context.Scale);
	}
	#endregion
}




















