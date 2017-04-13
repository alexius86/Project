using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public abstract class SiteSelectionListItem : MonoBehaviour {

	[SerializeField] protected Text itemName;

  public object Data { get; set; } // Holds site/slab/scan data for this item
	public int ItemID { get; set; }	// Unique ID for site/slab/scan.

	public string ItemName { 
		get { return itemName.text; }
		set { itemName.text = value; }
	}

	public virtual void Select () {

		Debug.LogWarning ("Subclass of SiteSelectionListItem should handle item selection.");
	}
}










