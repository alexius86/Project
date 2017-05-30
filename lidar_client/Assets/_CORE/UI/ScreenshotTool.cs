using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;
using BestHTTP;
using com.ootii.Messages;
using System;

public class ScreenshotTool : MonoBehaviour {

	[Space(5.0f)]
	// If true, show UI with thumbnail image and input fields for image name and album name.
	// If false, only show "screenshot saved" notification for a few seconds. Image is saved to camera roll with iOS defined name.
	[SerializeField] private bool namingEnabled = true;
	[SerializeField] private GameObject screenshotSavedNotification;
	[SerializeField] private float notificationShownTime = 2.0f;

	[Space(5.0f)]
	[SerializeField] private GameObject previewRoot;
	[SerializeField] private Image previewImage;
	[SerializeField] private TMP_InputField nameField;
  	[SerializeField] private TMP_InputField locationField;
    [SerializeField] private TMP_InputField notesField;

    [SerializeField] private string defaultImageName = "IMG_001";
	[SerializeField] private string fileFormat = "png";

	[Space(5.0f)]
	[SerializeField] private bool sendToServer = true;
	[SerializeField] private string serverUri = "http://10.32.16.183:5001/v1/";
    [SerializeField] private string currentSite = "";
    [SerializeField] private string currentSlab = "";
    [SerializeField] private string currentScan = "";

    [Space(5.0f)]
	[SerializeField] private bool saveToPhotoLibrary = false;

	[Space(10.0f)]
	[SerializeField] private RenderTexture screenshotRenderTexture;

	private Texture2D currentScreen;

	void OnEnable () {
		//ScreenshotManager.OnImageSaved += OnImageSaved;
	}

	void OnDisable () {
	    //ScreenshotManager.OnImageSaved -= OnImageSaved;
	}




    private void SiteDataSelected(IMessage message)
    {
        //Debug.Log ("Got site data in details display.");
        SiteData data = (SiteData)(message.Data);
    }


    public void TakeScreenshot () {

		currentScreen = new Texture2D (Screen.width, Screen.height, TextureFormat.ARGB32, false);

		nameField.text = defaultImageName;

		RenderTexture originalCameraTexture = Camera.main.targetTexture;

		// Tell camera to render into RenderTexture this frame.
		Camera.main.targetTexture = screenshotRenderTexture;
		Camera.main.Render ();

		// Copy screen into texture.
		RenderTexture.active = screenshotRenderTexture;
		currentScreen.ReadPixels (new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		currentScreen.Apply ();

		// Copy pixels from render texture into texture 2D so it can be applied to image sprite.
		previewImage.sprite = Sprite.Create(currentScreen, new Rect(0,0,Screen.width, Screen.height), Vector2.zero);

		Camera.main.targetTexture = originalCameraTexture;
		Camera.main.Render ();
		RenderTexture.active = null;

		// Show the UI.
		if (namingEnabled) {
			previewRoot.SetActive (true);	// Thumbnail and naming fields.
		} 
		else {
			Save ();
		}
	}

	private IEnumerator ShowScreenshotNotification () {

		screenshotSavedNotification.SetActive (true);	
		yield return new WaitForSeconds (notificationShownTime);
		screenshotSavedNotification.SetActive (false);
	}

	public void Save () {

		#if UNITY_IOS && !UNITY_EDITOR
		if (saveToPhotoLibrary) {
      		
			ScreenshotManager.SaveScreenshot (nameField.text, albumField.text, fileFormat, new Rect (0, 0, Screen.width, Screen.height));
			StopAllCoroutines ();	// Stop any existing notification if user is rapidly taking screens.
			StartCoroutine (ShowScreenshotNotification ());	// Simple confirmation message for a few seconds.
		}
		#endif

		if (sendToServer) {
			HTTPRequest postScreenshot = new HTTPRequest (new System.Uri(serverUri + "image_upload/"), HTTPMethods.Post);
            print(CurrentStatus.scanName);
            // location, datetime, tolerance, grid spacing not avilable. other notes. 
            //postScreenshot.AddBinaryData ("screenshot", currentScreen.GetRawTextureData (), nameField.text + fileFormat);
            postScreenshot.AddBinaryData("screenshot", currentScreen.EncodeToPNG(), nameField.text + fileFormat);
            postScreenshot.AddField("name", nameField.text + "");
            postScreenshot.AddField("format", fileFormat + "");
            postScreenshot.AddField("location",locationField.text);
            postScreenshot.AddField("site_name", CurrentStatus.siteName);
            postScreenshot.AddField("slab_name", CurrentStatus.slabName);
            postScreenshot.AddField("scan_id", CurrentStatus.scanID.ToString());
            postScreenshot.AddField("datetime", String.Format("{0:u}", DateTime.Now) + "");
            postScreenshot.AddField("grid_spacing", "not yet implemented");
            postScreenshot.AddField("tolerance",  CurrentStatus.lowerThreshhold.ToString()); // and and whatever threshold value
            postScreenshot.AddField("notes", notesField.text + "");
            postScreenshot.Send ();
		}

		Close ();
	}

	void OnImageSaved (string obj) {
		
		//StopAllCoroutines ();	// Stop any existing notification if user is rapidly taking screens.
		//StartCoroutine (ShowScreenshotNotification ());	// Simple confirmation message for a few seconds.
	}

	public void Close () {

		previewRoot.SetActive (false);
		previewImage.sprite = null;
		nameField.text = "";
		currentScreen = null;
	}
}
