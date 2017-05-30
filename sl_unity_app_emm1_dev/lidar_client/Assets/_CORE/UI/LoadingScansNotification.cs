using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class LoadingScansNotification : MonoBehaviour {

	[SerializeField] private GameObject displayRoot;
	[SerializeField] private Image loadingBarFill;

	void Awake () {

		displayRoot.SetActive (false);
	}

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.load_scans, ScanLoadingStarted);				// Load start.
		MessageDispatcher.AddListener (MessageDatabase.scans_loading, ScanLoadInProgress);			// Load in progress.
		MessageDispatcher.AddListener (MessageDatabase.scans_loaded, ScanLoadingFinished);			// Load complete.
		MessageDispatcher.AddListener (MessageDatabase.scan_load_cancelled, ScanLoadingCancelled);	// Load cancelled.
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.load_scans, ScanLoadingStarted);
		MessageDispatcher.RemoveListener (MessageDatabase.scans_loading, ScanLoadInProgress);
		MessageDispatcher.RemoveListener (MessageDatabase.scans_loaded, ScanLoadingFinished);
		MessageDispatcher.RemoveListener (MessageDatabase.scan_load_cancelled, ScanLoadingCancelled);
	}

	public void Cancel () {

		MessageDispatcher.SendMessage (MessageDatabase.scan_load_cancelled);
	}

	private void ScanLoadingStarted (IMessage message) {
		displayRoot.SetActive (true);
	}

	private void ScanLoadInProgress (IMessage message) {
		loadingBarFill.fillAmount = (float)(message.Data);
	}

	private void ScanLoadingFinished (IMessage message) {
		displayRoot.SetActive (false);
	}

	// Listen for cancel message in case some external script wants to cancel it as well, instead of just 
	// our cancel button.
	private void ScanLoadingCancelled (IMessage message) {
		displayRoot.SetActive (false);
	}
}
