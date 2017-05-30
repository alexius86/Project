using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using com.ootii.Messages;

public class LevelingToolUI : DetailsPanelToggleItem
{
  [SerializeField] private GameObject root;
	[Space(5.0f)]
	[SerializeField] private GameObject levelingControlsButton;
	[SerializeField] private GameObject camberControlButton;
	[Space(5.0f)]
  [SerializeField] private RectTransform itemsContainer;
  [SerializeField] private GameObject itemPrefab;
  [SerializeField] private ToggleGroup toggleGroup;
  [SerializeField] private LevelingTool levelingTool;

  private List<LevelingToolItem> items = new List<LevelingToolItem>();
  private ScanData currentScanData;

  void Awake()
  {
    MessageDispatcher.AddListener (MessageDatabase.scans_loaded, OnScanLoaded);
    MessageDispatcher.AddListener (MessageDatabase.scan_unloaded, OnScanUnloaded);
    MessageDispatcher.AddListener (MessageDatabase.selection_menu_loaded, OnSelectionMenuLoaded);

    root.SetActive (false);
  }

  void OnDestroy()
  {
    MessageDispatcher.RemoveListener (MessageDatabase.scans_loaded, OnScanLoaded);
    MessageDispatcher.RemoveListener (MessageDatabase.scan_unloaded, OnScanUnloaded);
    MessageDispatcher.RemoveListener (MessageDatabase.selection_menu_loaded, OnSelectionMenuLoaded);
  }

	void OnScanLoaded (IMessage message) {

		Scan scan = (Scan)message.Data;
		AddScanItem (scan);

    levelingTool.RefreshPositions();
	}

	void OnScanUnloaded (IMessage message) {
		
		ScanData scanData = (ScanData)message.Data;

		for (int i = 0; i < items.Count; i++) {
			
			LevelingToolItem item = items [i];
			if (item.ScanData == scanData) {
				
				Destroy (item.gameObject);
				items.Remove (item);

		        if (currentScanData == scanData) {
		          levelingTool.SetMode (LevelingToolEditMode.Disabled);
		        }

				break;
			}
		}

    if (items.Count <= 0)
    {
      this.Toggle ();
    }

    levelingTool.DestroyLeveledScan (scanData.scan_id);

    // do this for when the scan changes position
    levelingTool.RefreshPositions();
	}

	void OnSelectionMenuLoaded (IMessage message) {
		for (int i = 0; i < items.Count; i++) {
			
			LevelingToolItem item = items [i];
			items.Remove (item);
			Destroy (item.gameObject);
			i--;
		}

		levelingControlsButton.SetActive (false);
		camberControlButton.SetActive(false);
		levelingTool.SetMode (LevelingToolEditMode.Disabled);
	}

	public override void Show () {
		root.SetActive (true);
	}

	public override void Hide () {

		//levelingTool.ExitLevelingTool ();
		root.SetActive (false);
	}

  public void ToggleRoot()
  {
    root.SetActive (!root.gameObject.activeSelf);
  }

  public void AddScanItem(Scan scan)
  {
    LevelingToolItem item = Instantiate (itemPrefab).GetComponent<LevelingToolItem>();
    item.transform.SetParent (itemsContainer, false);

    item.ScanData = scan.scanData;
    item.ItemName = scan.scanData.scan_id.ToString();
    item.ItemToggle.group = toggleGroup;
    item.ItemToggle.isOn = false;
    item.ItemToggle.onValueChanged.AddListener ((value) =>
    {
				if (value) {	// Scan level item toggled ON.
          			ShowScan(scan);
				}
		        else {	// Scan level item was toggled OFF.
					
					bool allDisabled = true;
					foreach (LevelingToolItem i in items) {

						if (i.ItemToggle.isOn) {
							allDisabled = false;
							break;
						}
					}

					if (allDisabled) {
						levelingControlsButton.SetActive (false);
						camberControlButton.SetActive(false);
						levelingTool.SetMode(LevelingToolEditMode.Disabled);
					}
				}
    });

    items.Add (item);
  }

  public void ShowScan(Scan scan)
  {
		levelingControlsButton.SetActive (true);
		camberControlButton.SetActive(true);

    currentScanData = scan.scanData;
    levelingTool.ShowLevelingTool (scan.scanData.scan_id, scan.loadedObjectTransform.gameObject);
  }
}
