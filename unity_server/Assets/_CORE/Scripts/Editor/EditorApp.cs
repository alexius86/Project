using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Loads up and updates PCD Mesh Generators
/// </summary>
[InitializeOnLoad]
public class EditorApp
{
  // using static constructor for this to be initialized in unity editor
  static EditorApp()
  {
    // We want to trigger an asset database refresh in the background that asset imports happen regardless of editor focus
    EditorApplication.update += OnUpdate;

    // load all the mesh generator scriptable objects
    string[] assetPaths = AssetDatabase.FindAssets ("t:PCDMeshGenerator");
    for (int i = 0; i < assetPaths.Length; i++)
      AssetDatabase.LoadAssetAtPath<Object> (AssetDatabase.GUIDToAssetPath(assetPaths [i]));
  }

  static float refreshFrequency = 5; // refresh database every x seconds
  static double lastRefresh;

  static void OnUpdate()
  {
    // refresh database
    if (EditorApplication.timeSinceStartup - lastRefresh > refreshFrequency)
    {
      if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
      {
        AssetDatabase.Refresh ();
      }

      lastRefresh = EditorApplication.timeSinceStartup;
    }

    // update generators
    for (int i = 0; i < PCDMeshGenerator.Generators.Count; i++)
    {
      PCDMeshGenerator.Generators [i].Update ();
    }
  }
}