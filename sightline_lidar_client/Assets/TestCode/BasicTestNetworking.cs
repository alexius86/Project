using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BestHTTP;

public class BasicTestNetworking : MonoBehaviour {

	public string url = "https://private-8bed3-topsecretsl.apiary-mock.com";
	public string pingLocation = "/ping";


	HTTPRequest request;

	// Use this for initialization
	void Start () {
		request	= new HTTPRequest (new System.Uri (url + pingLocation), OnRequestFinishedDelegate);
		request.Send ();
	}

	void OnRequestFinishedDelegate(HTTPRequest request, HTTPResponse response){
		Debug.Log ("Request Finished: " + response.DataAsText);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
