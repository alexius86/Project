using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class PCDPostprocessor : AssetPostprocessor 
{
  static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
  {
    // iterate through all the imported assets and see if any generators match the paths and extensions
    for (int i = 0; i < importedAssets.Length; i++)
    {
      TryImport (importedAssets [i]);
    }

    for (int i = 0; i < movedAssets.Length; i++)
    {
      TryImport (movedAssets [i]);
    }
  }

  static void TryImport(string assetPath)
  {
    string extension = Path.GetExtension (assetPath).ToLower();

    if (PCDMeshGenerator.SupportedExtensions.Contains (extension))
    {
      assetPath = assetPath.Replace ("Assets/", "");

      foreach (PCDMeshGenerator generator in PCDMeshGenerator.Generators)
      {
        if (generator.EnqueuePCDToAsset(assetPath))
        {
          break;
        }
      }
    }
  }
}