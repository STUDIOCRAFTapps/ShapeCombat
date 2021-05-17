using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TilePrefabAssetCollection", menuName = "Custom/TilePrefabAssetCollection", order = -1)]
public class TilePrefabAssetCollection : ScriptableObject {
    public TilePrefab[] tilePrefabs;

    public void Init () {
        for(int i = 0; i < tilePrefabs.Length; i++) {
            tilePrefabs[i].prefabId = i;
        }
    }

    public TilePrefab this[int i] {
        get {
            return tilePrefabs[i];
        }
    }
}
