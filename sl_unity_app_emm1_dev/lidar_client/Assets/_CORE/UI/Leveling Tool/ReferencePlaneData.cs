using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReferencePlaneData : MonoBehaviour
{
  [SerializeField] private LevelingTool levelingTool;

  [SerializeField] private InputField posX;
  [SerializeField] private InputField posY;
  [SerializeField] private InputField posZ;
  [SerializeField] private InputField rotX;
  [SerializeField] private InputField rotY;
  [SerializeField] private InputField rotZ;

  void Awake()
  {
    levelingTool.ReferencePlaneCreated += OnReferencePlaneCreated;

    posX.onValueChanged.AddListener ((s) => {
      Vector3 pos = levelingTool.ReferencePlane.position;
      pos.x = Parse (s);
      levelingTool.ReferencePlane.position = pos;
    });

    posY.onValueChanged.AddListener ((s) => {
      Vector3 pos = levelingTool.ReferencePlane.position;
      pos.y = Parse (s);
      levelingTool.ReferencePlane.position = pos;
    });

    posZ.onValueChanged.AddListener ((s) => {
      Vector3 pos = levelingTool.ReferencePlane.position;
      pos.z = Parse (s);
      levelingTool.ReferencePlane.position = pos;
    });

    rotX.onValueChanged.AddListener ((s) => {
      Vector3 rot = levelingTool.ReferencePlane.eulerAngles;
      rot.x = Parse (s);
      levelingTool.ReferencePlane.rotation = Quaternion.Euler(rot);
    });

    rotY.onValueChanged.AddListener ((s) => {
      Vector3 rot = levelingTool.ReferencePlane.eulerAngles;
      rot.y = Parse (s);
      levelingTool.ReferencePlane.rotation = Quaternion.Euler(rot);
    });

    rotZ.onValueChanged.AddListener ((s) => {
      Vector3 rot = levelingTool.ReferencePlane.eulerAngles;
      rot.z = Parse (s);
      levelingTool.ReferencePlane.rotation = Quaternion.Euler(rot);
    });
  }

  void OnReferencePlaneCreated(Transform referencePlane)
  {
    LateUpdate ();
  }

  void LateUpdate()
  {
    if (levelingTool.ReferencePlane != null)
    {
      UpdateField (posX, levelingTool.ReferencePlane.position.x);
      UpdateField (posY, levelingTool.ReferencePlane.position.y);
      UpdateField (posZ, levelingTool.ReferencePlane.position.z);

      UpdateField (rotX, levelingTool.ReferencePlane.eulerAngles.x);
      UpdateField (rotY, levelingTool.ReferencePlane.eulerAngles.y);
      UpdateField (rotZ, levelingTool.ReferencePlane.eulerAngles.z);
    }
  }

  void UpdateField(InputField field, float value)
  {
    if (!field.isFocused)
    {
      field.text = ((int)(value * 1000) / 1000f).ToString();
    }
  }

  float Parse(string s)
  {
    float val = 0;
    float.TryParse (s, out val);
    return val;
  }
}
