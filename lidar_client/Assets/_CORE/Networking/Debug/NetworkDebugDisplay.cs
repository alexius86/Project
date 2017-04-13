using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkDebugDisplay : MonoBehaviour {

	public Text messageField;	// Message text (response or request) for network response at current index.
	public Text locationField;	// Eg.) Message "2/19"

	private List<string> networkMessages = new List<string> ();
	private int messageIndex;

	void OnEnable () {

		ServerConnection.OnNetworkActivity += OnNetworkActivity;
	}

	void OnDisable () {

		ServerConnection.OnNetworkActivity -= OnNetworkActivity;
	}

	public void NextMessage () {

		if (messageIndex < networkMessages.Count - 1) {
			
			messageIndex++;
			messageField.text = networkMessages [messageIndex];

			// Updates RHS of location text.
			locationField.text = (messageIndex+1) + "/" + networkMessages.Count;
		}
	}

	public void PrevMessage () {

		if (messageIndex > 0) {

			messageIndex--;
			messageField.text = networkMessages [messageIndex];

			// Updates RHS of location text.
			locationField.text = (messageIndex+1) + "/" + networkMessages.Count;
		}
	}

	private void OnNetworkActivity (string message) {

		// If this is the initial activity, update message display to show it.
		if (networkMessages.Count == 0) {
			messageField.text = message;
		}

		// Save message.
		networkMessages.Add (message);

		// Updates RHS of location text.
		locationField.text = (messageIndex+1) + "/" + networkMessages.Count;
	}
}


