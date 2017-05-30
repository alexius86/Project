using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class GeneralSettingsDisplay : DetailsPanelToggleItem {

  [SerializeField] private GameObject root;
  [SerializeField] private LevelingTool levelingTool;

  void Awake () {
    Hide ();
  }

  public override void Show () {
    root.SetActive (true);
  }

  public override void Hide () {
    root.SetActive (false);
  }
}






