using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reference : 
public static class JSONHelper {
	// parses a json list into a generic array, which is returned
	public static T[] FromJson<T>(string json){

        string newJson = "{ \"Items\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.Items;
    }

	// Iterates a list and returns the json recursively. 
	public static string ToJson<T>(T[] array){
		Wrapper<T> wrapper = new Wrapper<T> ();
		wrapper.Items = array;
		return JsonUtility.ToJson (wrapper);
	}
	// overloaded with pretty print functionality
	public static string ToJson<T>(T[] array, bool prettyPrint){
		Wrapper<T> wrapper = new Wrapper<T> ();
		wrapper.Items = array;
		return JsonUtility.ToJson (wrapper, prettyPrint);
	}
}

[System.Serializable]
class Wrapper<T>{
	public T[] Items;
}
