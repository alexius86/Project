using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONSerialization {

	public ScanData ReadScanData(string json){
		ScanData data = JsonUtility.FromJson<ScanData>(json);
		return data;
	}

	public string WriteScanData( ScanData data){
		string json = "";

		json = JsonUtility.ToJson (data);

		return json;
	}

}
