using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.UI;

public class ServerMessagePopup : MonoBehaviour {

	[SerializeField] private GameObject root;
	[SerializeField] private Text messageLabel;

	public void ShowMessage (string message) {

		messageLabel.text = message;
		root.SetActive (true);
	}

	public void Close () {

		root.SetActive (false);
	}
}
