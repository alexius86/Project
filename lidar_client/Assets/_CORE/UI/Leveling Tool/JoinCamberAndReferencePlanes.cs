using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinCamberAndReferencePlanes : MonoBehaviour {

	[Space(10.0f)]
	public LevelingTool levelingTool;
	[Space(5.0f)]
	public LineRenderer topLeftLine;
	public LineRenderer topRightLine;
	public LineRenderer bottomLeftLine;
	public LineRenderer bottomRightLine;

	void Update () {

		if (levelingTool != null && levelingTool.IsEditingCamber) {

			MeshRenderer referencePlaneRenderer = null;
			MeshRenderer camberPlaneRenderer = null;

			Transform referencePlane = levelingTool.ReferencePlane;
			if (referencePlane != null) {
				referencePlaneRenderer = referencePlane.GetComponent<MeshRenderer> ();
			}

			Transform camberPlane = levelingTool.CamberPlane;
			if (camberPlane != null) {
				camberPlaneRenderer = camberPlane.GetComponent<MeshRenderer> ();
			}

			if (referencePlaneRenderer != null && camberPlaneRenderer != null) {

				ShowLines ();

				Bounds referenceBounds = referencePlaneRenderer.bounds;
				Bounds camberBounds = camberPlaneRenderer.bounds;

				Vector3 camberCenter = camberBounds.center;
				Vector3 camberTopLeft = new Vector3 (camberCenter.x - camberBounds.extents.x, camberCenter.y, camberCenter.z + camberBounds.extents.z);
				Vector3 camberTopRight = new Vector3 (camberCenter.x + camberBounds.extents.x, camberCenter.y, camberCenter.z + camberBounds.extents.z);
				Vector3 camberBottomLeft = new Vector3 (camberCenter.x - camberBounds.extents.x, camberCenter.y, camberCenter.z - camberBounds.extents.z);
				Vector3 camberBottomRight = new Vector3 (camberCenter.x + camberBounds.extents.x, camberCenter.y, camberCenter.z - camberBounds.extents.z);

				Vector3 referenceCenter = referenceBounds.center;
				Vector3 referenceTopLeft = new Vector3 (referenceCenter.x - referenceBounds.extents.x, referenceCenter.y, referenceCenter.z + referenceBounds.extents.z);
				Vector3 referenceTopRight = new Vector3 (referenceCenter.x + referenceBounds.extents.x, referenceCenter.y, referenceCenter.z + referenceBounds.extents.z);
				Vector3 referenceBottomLeft = new Vector3 (referenceCenter.x - referenceBounds.extents.x, referenceCenter.y, referenceCenter.z - referenceBounds.extents.z);
				Vector3 referenceBottomRight = new Vector3 (referenceCenter.x + referenceBounds.extents.x, referenceCenter.y, referenceCenter.z - referenceBounds.extents.z);

				topLeftLine.SetPosition (0, camberTopLeft);
				topLeftLine.SetPosition (1, referenceTopLeft);

				topRightLine.SetPosition (0, camberTopRight);
				topRightLine.SetPosition (1, referenceTopRight);

				bottomLeftLine.SetPosition (0, camberBottomLeft);
				bottomLeftLine.SetPosition (1, referenceBottomLeft);

				bottomRightLine.SetPosition (0, camberBottomRight);
				bottomRightLine.SetPosition (1, referenceBottomRight);
			} 
			else {
				HideLines ();
			}
		}
		else {
			HideLines ();
		}
	}

	private void HideLines () {

		topLeftLine.enabled = false;
		topRightLine.enabled = false;
		bottomLeftLine.enabled = false;
		bottomRightLine.enabled = false;
	}

	private void ShowLines () {

		topLeftLine.enabled = true;
		topRightLine.enabled = true;
		bottomLeftLine.enabled = true;
		bottomRightLine.enabled = true;
	}
}
