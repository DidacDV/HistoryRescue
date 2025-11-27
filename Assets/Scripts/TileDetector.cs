

using System.Collections.Generic;
using UnityEngine;

public static class TileDetector {
    public static GameObject[] GetTilesByLayer(string NameOfLayer) {
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None); //faster than normal?
        List<GameObject> detectedTiles = new List<GameObject>();

        foreach (var tile in allObjects) {
            if (tile.layer == LayerMask.NameToLayer(NameOfLayer))
                detectedTiles.Add(tile);
        }
        Debug.Log($"detected {detectedTiles.Count} tiles");
        return detectedTiles.ToArray();
    }
}