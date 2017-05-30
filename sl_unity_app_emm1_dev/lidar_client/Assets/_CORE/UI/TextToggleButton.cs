using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

// Set text to ON or OFF instead of showing or hiding a graphic.
public class TextToggleButton : MonoBehaviour {

  public string onText = "ON";
  public string offText = "OFF";

  public TextMeshProUGUI text;

  void Awake () {

    text.text = offText;
  }

  public void Toggle (bool value) {

    text.text = value ? onText : offText;
  }
}















