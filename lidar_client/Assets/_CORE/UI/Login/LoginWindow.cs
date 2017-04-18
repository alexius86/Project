using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


using com.ootii.Messages;

/// <summary>
/// Handles login logic
/// </summary>
public class LoginWindow : MonoBehaviour {
  
	[Space(5.0f)]
	[SerializeField] private bool skipAuthentication = false;

	[Space(5.0f)]
	[SerializeField] private InputField userName;
	[SerializeField] private InputField password;

	[Space(5.0f)]
	[SerializeField] private string mainSceneName = "Main";
	[SerializeField] private string forgotPasswordLink = "www.forgotpasswordlocation.com";

	private bool acceptingInput = true;

	void OnEnable () {

		MessageDispatcher.AddListener (MessageDatabase.user_auth_success, OnAuthSuccess);
		MessageDispatcher.AddListener (MessageDatabase.user_auth_failure, OnAuthFailure);
	}

	void OnDisable () {

		MessageDispatcher.RemoveListener (MessageDatabase.user_auth_success, OnAuthSuccess);
		MessageDispatcher.RemoveListener (MessageDatabase.user_auth_failure, OnAuthFailure);
	}

	public void TryLogin () {

		if (!acceptingInput) return;

//		if (skipAuthentication) {
//			Login ();
//		}
		else {

			ServerConnection.Instance.RequestAuthentication (userName.text, password.text);

			// Block input while request is processing.
			Toggle(false);

			//TODO: Give visual feedback that login progress is working (in case connection is poor).
		}
	}

	public void ForgotPassword () {

		if (acceptingInput) {
			Application.OpenURL (forgotPasswordLink);
		}
	}

	private void OnAuthSuccess (IMessage message) {

        // Session cookie should be set for subsequent requests to use.
        print(message.Data);
		Login ();
	}

	private void Login () {

		SceneManager.LoadScene (mainSceneName);
	}

	private void Toggle (bool acceptingInput) {

		this.acceptingInput = acceptingInput;

		userName.interactable = acceptingInput;
		password.interactable = acceptingInput;
	}

	private void OnAuthFailure (IMessage message) {

		// Unblock input.
		Toggle(true);
		
	}
}
