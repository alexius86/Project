using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MouseEventHelper : MonoBehaviour
{
  public UnityEvent MouseDown;

  void OnMouseDown()
  {
    MouseDown.Invoke ();
  }
}