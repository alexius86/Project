using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class ScanListItem : SiteSelectionListItem {

	public override void Select () {

		MessageDispatcher.SendMessage (this, MessageDatabase.menu_scan_selected, Data, 0.0f);
	}
}




