using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public enum LevelingToolEditMode {
	Disabled,
	ReferencePlane,
	CamberPlane,
}

public class LevelingTool : MonoBehaviour {

  public const int HEIGHTMAP_LAYER_ID = 8;

  public Transform ReferencePlane {
  	get { return planeTransform; }
  }

  public Transform CamberPlane {
    get { return camberPlaneTransform; }
  }

  public Transform CamberVolume {
    get { return camberVolume; }
  }

	// Gets the current plane transform that user is editing based on the current edit mode. Returns null if
	// edit mode is disabled or if the wanted transform in uninitialized.
	public Transform CurrentTransform {
		get {
			if (mode == LevelingToolEditMode.Disabled)
				return null;
			else if (mode == LevelingToolEditMode.ReferencePlane) {
				return planeTransform;
			} else {
				return camberPlaneTransform;
			}
		}
	}

	// Min and max for clamping height offset within bounding box of scan mesh.
	public float MinHeight { get; private set; }
	public float MaxHeight { get; private set; }

	public System.Action<Transform> ReferencePlaneCreated;
	public System.Action<float> CamberPlaneCreated;
	public System.Action LevelingToolExit;

	[SerializeField] private bool keepPlanesParallel; // this will keep camber plane and reference plane parallel to eachother 
	[SerializeField] private Material levelingPlaneMaterial; // material for the bottom plane
	[SerializeField] private Material camberPlaneMaterial; // material for the top plane
	[SerializeField] private Material camberVolumeMaterial; // material that will render the camber volume to the height map buffer
	[SerializeField] private Material heightMapMaterial; // regular material for the reference plane method
	[SerializeField] private Material heightMapMaterialVolumeBased; // material with a shader that reads from the height map buffer render texture
	[SerializeField] private GameObject edgeLinePrefab; // prefab for the plane edges make sure Z dimension of this prefab is 1 unit long both in number and unit for it to stretch properly

	public LevelingToolEditMode mode = LevelingToolEditMode.Disabled;

	[Header("Shared Controls")]
	[SerializeField] private float rotationRate = 7;
	[SerializeField] private float moveRate = 1;
	[SerializeField] private float horizontalMoveRate = 5;
	[SerializeField] private float scaleRate = 2;

	public bool IsEditingCamber { get { return mode == LevelingToolEditMode.CamberPlane; } }
	public bool IsEditingReferencePlane { get { return mode == LevelingToolEditMode.ReferencePlane; } }

	private GameObject currentSourceObject { get { return leveledScan != null ? leveledScan.sourceObject : null; } }
	private Transform planeTransform { get { return leveledScan != null ? leveledScan.planeTransform : null; } }
	private Transform camberPlaneTransform { get { return leveledScan != null ? leveledScan.camberPlaneTransform : null; } }
	private Transform camberVolume { get { return leveledScan != null ? leveledScan.camberVolumeTransform : null; } }

	private MeshFilter planeMeshFilter { get { return leveledScan != null ? leveledScan.planeMeshFilter : null; } }
	private MeshFilter camberPlaneMeshFilter { get { return leveledScan != null ? leveledScan.camberPlaneMeshFilter : null; } }
  	private MeshFilter camberVolFilter { get { return camberVolFilter; } }

	public Material currentMaterial { get { return leveledScan != null ? leveledScan.currentMaterial : null; } }
	private Material currentCamberVolMaterial { get { return leveledScan != null ? leveledScan.currentCamberVolMaterial : null; } }
	private Matrix4x4 matrix = new Matrix4x4();
  	private CanvasGroup canvasGroup;

	private Vector3[] camberVertices = new Vector3[8];

	private LeveledScan leveledScan = new LeveledScan(null);
	private Dictionary<int, LeveledScan> leveledScanCache = new Dictionary<int, LeveledScan>(); // int is scanID
	private Dictionary<int, LevelingData> levelingDatas = new Dictionary<int, LevelingData>();


	void Awake () {
		
		canvasGroup = GetComponentInChildren<CanvasGroup> ();

		EnableUI (false);
		mode = LevelingToolEditMode.Disabled;
	}

  	public void ShowLevelingTool (int scanId, GameObject scanObject) {

		if (scanObject == null) return;

	    leveledScan = null;
	    leveledScanCache.TryGetValue (scanId, out leveledScan);

	    // Get min and max for clamping height offset within bounding box of scan mesh.
	    Renderer[] renderers = scanObject.GetComponentsInChildren<Renderer> ();
	    Bounds b = new Bounds();
	    for (int i = 0; i < renderers.Length; i++) {

	    	if (i == 0) {
	        	b = new Bounds (renderers [i].bounds.center, renderers [i].bounds.size);
	      	} 
	      	else {
	        	b.Encapsulate (renderers [i].bounds);
	      	}
	    }

	    if (renderers.Length > 0) {

	      MinHeight = b.min.y - 0.5f;
	      MaxHeight = b.max.y + 0.5f;

	      Debug.Log ("Setting min height: " + MinHeight + "\nSetting max height: " + MaxHeight);
	    } 
	    else {
	      Debug.LogWarning ("Renderer component not found on scan mesh that leveling tool is working with.");
	    }

	    // look for previous leveling configurations for this scan
	    LevelingData levelingData = null;
	    if (!levelingDatas.TryGetValue (scanId, out levelingData)) {
	    	levelingDatas.Add (scanId, levelingData);
	    }

		// Hide all planes before showing current.
		foreach (KeyValuePair<int, LeveledScan> kvp in leveledScanCache) {
			kvp.Value.HideCamberPlane ();
			kvp.Value.HideReferencePlane ();
		}

	    if (leveledScan != null) {

			// load up the data if any
			print(levelingData);
			leveledScan.levelingData = levelingData;

			if (leveledScan.mode == LeveledScanMode.Camber) {
				SetMode (LevelingToolEditMode.CamberPlane);
			} else if (leveledScan.mode == LeveledScanMode.ReferencePlane) {
				SetMode (LevelingToolEditMode.ReferencePlane);
			}
	    } 
		else {
	     
			leveledScan = new LeveledScan (scanObject);
	      	leveledScan.scanId = scanId;
	      	leveledScan.levelingData = levelingData;

	      	// set required materials
	      	leveledScan.levelingPlaneMaterial = levelingPlaneMaterial;
	      	leveledScan.camberPlaneMaterial = camberPlaneMaterial;
	      	leveledScan.camberVolumeMaterial = camberVolumeMaterial;
	      	leveledScan.edgeLinePrefab = edgeLinePrefab;

	      	leveledScanCache.Add (scanId, leveledScan);

	      	if (levelingData != null && levelingData.camberMode) {
	        	SetMode (LevelingToolEditMode.CamberPlane);
	      	} else {
	        	SetMode (LevelingToolEditMode.ReferencePlane);
	      	}
	    }
	}


  public void SetMode (LevelingToolEditMode _mode) {
    
		mode = _mode;

    	if (_mode == LevelingToolEditMode.ReferencePlane) {
      
			leveledScan.SetMode (LeveledScanMode.ReferencePlane);
      		leveledScan.SetMaterial (heightMapMaterial);

			EnableUI (true);

			if (ReferencePlaneCreated != null) {
				ReferencePlaneCreated (ReferencePlane);
			}
    	}
	    else if (mode == LevelingToolEditMode.CamberPlane) {
			
			leveledScan.SetMode (LeveledScanMode.Camber);
			leveledScan.SetMaterial(heightMapMaterialVolumeBased);

			EnableUI (true);

			if (CamberPlaneCreated != null) {
				CamberPlaneCreated (ReferencePlane.position.y);
			}
	    }
    	else {
			
			EnableUI (false);
			if (LevelingToolExit != null) {
				LevelingToolExit ();
			}
    	}  

		MessageDispatcher.SendMessage(this, MessageDatabase.levelingToolEditModeChanged, mode, 0.0f);
  }

  public void ResetCamberPlane () {
    leveledScan.ResetCamberOnly ();
  }

  public void ResetReferencePlane ()
  {
    leveledScan.Reset ();
  }

  public void RefreshPositions()
  {
    foreach (var kv in leveledScanCache)
    {
      kv.Value.RefreshPosition ();
    }
  }

  public void DestroyLeveledScan(int scanId)
  {
    LeveledScan leveledScan = null;
    if (leveledScanCache.TryGetValue (scanId, out leveledScan))
    {
      leveledScan.Destroy ();
      leveledScanCache.Remove (scanId);
    }
  }

  void EnableUI(bool enable)
  {
    canvasGroup.alpha = enable ? 1 : 0;
    canvasGroup.interactable = enable;
    canvasGroup.blocksRaycasts = enable;
  }

	#region Rotate
	public void SetPlaneRotationX (float angle) {

		if (mode == LevelingToolEditMode.Disabled)
			return;

		Transform t = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		if (t != null) {
			t.rotation = Quaternion.Euler (new Vector3 (angle, t.eulerAngles.y, t.eulerAngles.z));
		} else {
			Debug.LogWarning ("Unable to set plane rotation X: Plane transform is null.");
		}
	}

	public void SetPlaneRotationZ (float angle) {

		if (mode == LevelingToolEditMode.Disabled)
			return;

		Transform t = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		if (t != null) {
			t.rotation = Quaternion.Euler (new Vector3 (t.eulerAngles.x, t.eulerAngles.y, angle));
		} else {
			Debug.LogWarning ("Unable to set plane rotation Z: Plane transform is null.");
		}
	}

	// 1D slider.
	public void RotatePlaneZ (Vector2 direction) {

		if (mode == LevelingToolEditMode.Disabled || direction == Vector2.zero)
			return;

		Transform targetTransform = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		MeshFilter targetMeshFilter = mode == LevelingToolEditMode.ReferencePlane ? planeMeshFilter : camberPlaneMeshFilter;
		float originalRotZ = targetTransform.eulerAngles.z;

		// Rotate either the reference plane or the camber plane, depending on what edit mode we're in.
		PlaneRotation (
			new Vector2(0.0f, direction.y), // Y only.
			targetMeshFilter, 
			targetTransform
		);

		// There is floating point imprecision in PlaneRotation that can't be avoided, so we end up with very slight Z axis rotation as well.
		// Reset it here.
		Vector3 eulerAngles = targetTransform.eulerAngles;
		eulerAngles.z = originalRotZ;
		targetTransform.eulerAngles = eulerAngles;
	}

	// 1D slider.
	public void RotatePlaneX (Vector2 direction) {

		if (mode == LevelingToolEditMode.Disabled || direction == Vector2.zero)
			return;

		Transform targetTransform = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		MeshFilter targetMeshFilter = mode == LevelingToolEditMode.ReferencePlane ? planeMeshFilter : camberPlaneMeshFilter;
		float originalRotX = targetTransform.eulerAngles.x;

		// Rotate either the reference plane or the camber plane, depending on what edit mode we're in.
		PlaneRotation (
			new Vector2(direction.x, 0.0f), // X only. 
			targetMeshFilter, 
			targetTransform
		);

		// There is floating point imprecision in PlaneRotation that can't be avoided, so we end up with very slight X axis rotation as well.
		// Reset it here.
		Vector3 eulerAngles = targetTransform.eulerAngles;
		eulerAngles.x = originalRotX;
		targetTransform.eulerAngles = eulerAngles;
	}

	// 2D joystick.
	public void RotatePlane (Vector2 direction) {

		if (mode == LevelingToolEditMode.Disabled)
			return;

		float absX = Mathf.Abs (direction.x);
		float absY = Mathf.Abs (direction.y);
		float smoothedX = absX * absX;
		float smoothedY = absY * absY;

		// Rotate either the reference plane or the camber plane, depending on what edit mode we're in.
		PlaneRotation (
			new Vector2(direction.x < 0.0f ? -smoothedX : smoothedX, direction.y < 0.0f ? -smoothedY : smoothedY), 
			mode == LevelingToolEditMode.ReferencePlane ? planeMeshFilter : camberPlaneMeshFilter,
			mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform
		);
	}

	/// <summary>
	/// Rotates the plane t.
	/// </summary>
	/// <param name="direction">Stick direction for rotation.</param>
	/// <param name="meshFilter">The plane mesh filter.</param>
	/// <param name="t">The plane transform</param>
	private void PlaneRotation (Vector2 direction, MeshFilter meshFilter, Transform t) {

		if (meshFilter != null) {

			Vector3 rotDelta = new Vector3 (direction.y, 0, -direction.x) * rotationRate * Time.deltaTime;
			rotDelta = Camera.main.transform.TransformDirection (rotDelta);
			rotDelta.y = 0;
			t.rotation = Quaternion.Euler (t.eulerAngles + rotDelta);
		}
	}
	#endregion

	#region Translate
  public void SetPlaneHeight (float height) {

		if (mode == LevelingToolEditMode.Disabled)
			return;

		Transform t = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		if (t != null) {
			t.position = new Vector3 (t.position.x, Mathf.Clamp (height, MinHeight, MaxHeight), t.position.z);
		} else {
			Debug.LogWarning ("Unable to set plane height: Plane transform is null.");
		}
	}

	public void MoveVertically (Vector2 direction) {

		// Get smoothed (logarithmic) input value. Removes sign (direction) though.
		float absY = Mathf.Abs (direction.y);
		float smoothedY = absY * absY;

		// Calculate the change in movement. Set negative smoothed value if the raw input direction was negative.
		Vector3 adjustedVector = new Vector3 (0, direction.y < 0.0f ? -smoothedY : smoothedY, 0);

		// Do the move.
		PlaneTranslate (adjustedVector * moveRate);
	}

	//TODO: This should be renamed. It is a move along a 2D plane.
	public void MoveHorizontally (Vector2 direction) {

		// Get smoothed (logarithmic) input value. Removes sign (direction) though.
		float absX = Mathf.Abs (direction.x);
		float absY = Mathf.Abs (direction.y);
		float smoothedX = absX * absX;
		float smoothedY = absY * absY;

		// Calculate the change in movement. Set negative smoothed value if the raw input direction was negative.
		Vector3 adjustedDirection = new Vector3 (direction.x < 0.0f ? -smoothedX : smoothedX, 0, direction.y < 0.0f ? -smoothedY : smoothedY);
		adjustedDirection = Camera.main.transform.TransformDirection (adjustedDirection);
		adjustedDirection.y = 0;

		// Do the move.
		PlaneTranslate (adjustedDirection * horizontalMoveRate);
	}

	private void PlaneTranslate (Vector3 moveVector) {
    
		if (mode == LevelingToolEditMode.ReferencePlane && planeMeshFilter != null) {

			if (planeTransform != null) {

				Vector3 target = planeTransform.position + (moveVector * Time.deltaTime);
				target.y = Mathf.Clamp (target.y, MinHeight, MaxHeight);
				planeTransform.position = target;

			} else {
				Debug.LogWarning ("Unable to translate plane: Plane transform is null.");
			}
		} 
		else if (mode == LevelingToolEditMode.CamberPlane && camberPlaneMeshFilter != null) {
      
			if (camberPlaneTransform != null) {
				Vector3 target = camberPlaneTransform.position + moveVector * Time.deltaTime;
				target.y = Mathf.Clamp (target.y, planeTransform.position.y, MaxHeight); // clamp vertical position so it doesn't go below the base
				camberPlaneTransform.position = target;
			} else {
				Debug.LogWarning ("Unable to translate plane: Camber plane transform is null.");
			}
		}
	}
	#endregion

	#region Scale
	public void ScaleZ (Vector2 direction) {

		if (mode == LevelingToolEditMode.Disabled || direction == Vector2.zero)
			return;

		Transform targetTransform = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		MeshFilter targetMeshFilter = mode == LevelingToolEditMode.ReferencePlane ? planeMeshFilter : camberPlaneMeshFilter;
		float originalScaleX = targetTransform.localScale.x;

		// Rotate either the reference plane or the camber plane, depending on what edit mode we're in.
		Scale (
			new Vector2(0.0f, direction.y), // Y input only.
			targetMeshFilter, 
			targetTransform
		);

		// There is floating point imprecision in PlaneRotation that can't be avoided, so we end up with very slight X axis scale as well.
		// Reset it here.
		Vector3 scale = targetTransform.localScale;
		scale.x = originalScaleX;
		targetTransform.localScale = scale;
	}

	public void ScaleX (Vector2 direction) {

		if (mode == LevelingToolEditMode.Disabled || direction == Vector2.zero)
			return;

		Transform targetTransform = mode == LevelingToolEditMode.ReferencePlane ? planeTransform : camberPlaneTransform;
		MeshFilter targetMeshFilter = mode == LevelingToolEditMode.ReferencePlane ? planeMeshFilter : camberPlaneMeshFilter;
		float originalScaleZ = targetTransform.localScale.z;

		// Rotate either the reference plane or the camber plane, depending on what edit mode we're in.
		Scale (
			new Vector2(direction.x, 0.0f), // X input only.
			targetMeshFilter, 
			targetTransform
		);

		// There is floating point imprecision in PlaneRotation that can't be avoided, so we end up with very slight Z axis scale as well.
		// Reset it here.
		Vector3 scale = targetTransform.localScale;
		scale.z = originalScaleZ;
		targetTransform.localScale = scale;
	}

	public void Scale (Vector2 direction, MeshFilter meshFilter, Transform t) {

		if (meshFilter != null && t != null) {

			float absX = Mathf.Abs (direction.x);
			float absY = Mathf.Abs (direction.y);
			float smoothedX = absX * absX;
			float smoothedY = absY * absY;

			Vector3 scale = t.localScale;
			scale.x += ((direction.x < 0.0f) ? -smoothedX : smoothedX) * scaleRate * Time.deltaTime;
			scale.z += ((direction.y < 0.0f) ? -smoothedY : smoothedY) * scaleRate * Time.deltaTime;
			t.localScale = scale;
		}
	}
	#endregion

	public static float ClampAngle (float angle, float min, float max) {
		
		angle = Mathf.Repeat(angle, 360);
		min = Mathf.Repeat(min, 360);
		max = Mathf.Repeat(max, 360);

		bool invert = false;
		float _min = min;
		float _angle = angle;

		if (min > 180) {
			invert = !invert;
			_min -= 180;
		}
		if (angle > 180) {
			invert = !invert;
			_angle -= 180;
		}

		bool result = !invert ? _angle > _min : _angle < _min;
		if (!result) {
			angle = min;
		}
		invert = false;
		_angle = angle;

		float _max = max;
		if (angle > 180) {
			invert = !invert;
			_angle -= 180;
		}
		if (max > 180) {
			invert = !invert;
			_max -= 180;
		}

		result = !invert ? _angle < _max : _angle > _max;
		if (!result) {
			angle = max;
		}

		return angle;
	}

	void LateUpdate () {

		if (mode == LevelingToolEditMode.Disabled)
			return;

		// Clamp edit plane rotations. Do this in Update so values are also automatically clamped if other scripts
		// are trying to edit values (manually entering field values via UI input fields, for example).
		if (CurrentTransform != null) {

			float limit = 20.0f;

			float rotX = ClampAngle (CurrentTransform.eulerAngles.x, -limit, limit);
			float rotZ = ClampAngle (CurrentTransform.eulerAngles.z, -limit, limit);

			CurrentTransform.eulerAngles = new Vector3 (rotX, CurrentTransform.eulerAngles.y, rotZ);
		}

		if (planeMeshFilter != null) {

	      	if (leveledScan.mode == LeveledScanMode.Camber) {
				
			    // this works by having the camberVolume mesh be drawn into a separate camera that renders into a texture
			    // a shader then uses that to sample the height based on the pixel's screen space

			    // for all this tech to work you need to create a second camera that's a child of your regular camera with zero position and rotation (pretty much a clone camera)
			    // set that camera's culling mask to only include the HEIGHTMAP_LAYER_ID layer (const at the top)
			    // set camera's background to black and solid color
			    // other than that all the camera settings have to be the same
			    // now create a render texture, drag it into the camera's Target Texture field
			    // now drag this render texture into the _HeightMapBuffer field of the material using "Height Map Volume Based" shader

			    if (keepPlanesParallel)
			      planeTransform.rotation = camberPlaneTransform.rotation;

			    leveledScan.UpdateCamberVolume ();

			    // We need to know the max height of the volume to pass the range to the shader that creates the render texture
			    Vector3 v = camberPlaneTransform.position - planeTransform.position;
			    Vector3 n = planeTransform.up;
			    float h = Vector3.Dot (v, n);

				if (currentCamberVolMaterial != null) {
					currentCamberVolMaterial.SetFloat ("_StartY", planeTransform.position.y);
					currentCamberVolMaterial.SetFloat ("_EndY", camberPlaneTransform.position.y);
					currentMaterial.SetFloat ("_HeightBufferRange", h);
				} else {
					Debug.LogWarning ("Current camber volume material is null.");
				}

				if (currentMaterial != null) {
					currentMaterial.SetVector ("_ReferenceOrigin", planeTransform.position);
					currentMaterial.SetVector ("_ReferenceNormal", planeTransform.up);
					currentMaterial.SetVector ("_CamberOrigin", camberPlaneTransform.position);
				
					// this is needed by an alternative shader we aren't using anymore but might be worth still exploring that one at some point?
					currentMaterial.SetFloat ("_PlaneSeparationDistance", h);
				} 
				else {
					Debug.LogWarning ("Current material is null.");
				}
			  }
			  else {
			    
				// We send the matrix that we will transform our heightmap by to the material / shader
				matrix.SetTRS (Vector3.zero, Quaternion.Inverse(planeTransform.rotation), Vector3.one);

			    currentMaterial.SetMatrix ("_CustomMatrix", matrix);
			    currentMaterial.SetVector ("_ReferenceOrigin", planeTransform.position);
			  }

		      leveledScan.SaveLevelingData ();
		      levelingDatas [leveledScan.scanId] = leveledScan.levelingData;
		}
	}

  // we update the mesh in world space as that's just easier to keep track of

}