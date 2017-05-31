using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CollapsibleListItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

	private bool isExpanded;
	private Text contentsText;
	private LayoutElement itemLayout;
	private ContentSizeFitter contentsTextSizeFitter;	// The ContentSizeFitter on the Text component.
	private float initialHeight;	// Height when not expanded.

	void Start () {
		
		contentsText = GetComponentInChildren<Text>();
		itemLayout = GetComponent<LayoutElement>();
		contentsTextSizeFitter = GetComponentInChildren<ContentSizeFitter>();
		initialHeight = contentsText.rectTransform.sizeDelta.y;
		isExpanded = false;
	}

	public void OnPointerUp (PointerEventData eventData) {

		if (isExpanded)
			CollapseText();
		else
			ExpandText();
	}

	public void OnPointerDown(PointerEventData eventData)
	{ }

	private void CollapseText () {

		// Disable ContentSizeFitter and reset height of text to initial height.
		contentsTextSizeFitter.enabled = false;
		contentsText.rectTransform.sizeDelta = new Vector2(contentsText.rectTransform.sizeDelta.x, initialHeight);
		itemLayout.minHeight = initialHeight;
		isExpanded = false;
	}

	private void ExpandText () {
		
		// Turn on the size fitter, and set the panel's min height to the new size fitter.
		contentsTextSizeFitter.enabled = true;

		// ContentSizeFitter is run in a Co-routine.  So we have to wait till its done resizing
		// We can't just wait until the commentText size is > InitialHeight because the comment might not have overflowed
		// So we set the size to 0, then wait for it to be bigger than 0
		contentsText.rectTransform.sizeDelta = new Vector2(contentsText.rectTransform.sizeDelta.x, 0);
		StartCoroutine(WaitForSizeChange());
	}

	private IEnumerator WaitForSizeChange () {

		bool waitFlag = true;
		while (waitFlag) {

			// Continue to wait as long as rect transform's height is still changing.
			if (contentsText.rectTransform.sizeDelta.y > 0)
				waitFlag = false;
			
			yield return null;
		}

		// ContentSizeFitter is done resizing the text, now resize the container.
		itemLayout.minHeight = contentsText.rectTransform.sizeDelta.y;
		isExpanded = true;
	}

}