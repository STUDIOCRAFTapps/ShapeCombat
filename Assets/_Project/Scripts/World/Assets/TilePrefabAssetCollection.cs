using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TilePrefabAssetCollection", menuName = "Custom/TilePrefabAssetCollection", order = -1)]
public class TilePrefabAssetCollection : ScriptableObject {
    public TilePrefab[] tilePrefabs;
}
