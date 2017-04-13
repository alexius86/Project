using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class SiteListItem : SiteSelectionListItem {

  [SerializeField] protected GameObject itemDescriptionContainer;
  [SerializeField] protected Text itemDescription;
  [SerializeField] protected Button backButton;

  public string ItemDescription {
    get { return itemDescription.text; }
    set { itemDescription.text = value; }
  }

  public Button BackButton {
    get { return backButton; }
  }

  public void ShowDetails()
  {
    itemDescriptionContainer.SetActive (true);
  }

  public void HideDetails()
  {
    itemDescriptionContainer.SetActive (false);
  }

	public override void Select () {

		MessageDispatcher.SendMessage (this, MessageDatabase.menu_site_selected, Data, 0.0f);

	}
}




