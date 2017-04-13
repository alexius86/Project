using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class LevelingToolItem : MonoBehaviour
{
  [SerializeField] private Toggle itemToggle;
  [SerializeField] private TextMeshProUGUI itemName;

  public Toggle ItemToggle
  {
    get { return itemToggle; }
  }

  public string ItemName
  {
    get { return itemName.text; }
    set { itemName.text = value; }
  }

  public ScanData ScanData { get; set; }
}
