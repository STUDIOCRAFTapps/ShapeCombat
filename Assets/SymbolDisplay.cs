using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class SymbolDisplay : MonoBehaviour {

    public SymbolUnit symbolUnitPrefab;
    private Dictionary<ulong, SymbolUnitGroup> symbolUnitGroups;
    private Queue<SymbolUnitGroup> symbolUnitPool;
    private List<Enemies> screenSpaceEnemies;

    private void Awake () {
        symbolUnitGroups = new Dictionary<ulong, SymbolUnitGroup>();
        symbolUnitPool = new Queue<SymbolUnitGroup>();
        screenSpaceEnemies = new List<Enemies>();
    }

    // Only display symbol for enemies on screen
    // Only set dirty when enemies leave/join screen or summoned/destroyed

    void Update () {
        UpdateUnits();

        foreach(KeyValuePair<ulong, SymbolUnitGroup> kvp in symbolUnitGroups) {
            kvp.Value.UpdateDisplayPosition();
        }
    }

    private void UpdateUnits () {
        foreach(KeyValuePair<ulong, SymbolUnitGroup> kvp in symbolUnitGroups) {
            ReturnSymbolUnitGroup(kvp.Key);
        }
        symbolUnitGroups.Clear();
        EnemyManager.ClearUnitGroups();
        screenSpaceEnemies.Clear();
        EnemyManager.ForEachOnScreenEnemy((e) => {
            e.CalculateSymbolHash();
            screenSpaceEnemies.Add(e);
        });

        for(int i = 0;  i < screenSpaceEnemies.Count; i++) {
            if(symbolUnitGroups.TryGetValue(screenSpaceEnemies[i].symbolHash, out SymbolUnitGroup value)) {
                value.enemies.Add(screenSpaceEnemies[i]);
            } else {
                SymbolUnitGroup sug = GetSymbolUnitGroup(screenSpaceEnemies[i].symbolHash);
                sug.enemies.Add(screenSpaceEnemies[i]);
                sug.UpdateUnitSymbol();
            }
        }
    }

    private SymbolUnitGroup GetSymbolUnitGroup (ulong hash) {
        if(symbolUnitPool.Count > 0) {
            SymbolUnitGroup sug = symbolUnitPool.Dequeue();
            sug.index = symbolUnitGroups.Count;
            symbolUnitGroups.Add(hash, sug);
            return sug;
        } else {
            SymbolUnitGroup sug = new SymbolUnitGroup(symbolUnitPrefab, transform);
            sug.index = symbolUnitGroups.Count;
            symbolUnitGroups.Add(hash, sug);
            return sug;
        }
    }

    private void ReturnSymbolUnitGroup (ulong hash) {
        SymbolUnitGroup sug = symbolUnitGroups[hash];
        sug.Clear();
        symbolUnitPool.Enqueue(sug);
    }
}

public class SymbolUnitGroup {
    public int index;
    public List<Enemies> enemies;
    public SymbolUnit symbolUnit;

    public SymbolUnitGroup (SymbolUnit symbolUnitPrefab, Transform parent) {
        symbolUnit = GameObject.Instantiate(symbolUnitPrefab, parent);
        enemies = new List<Enemies>();
    }

    public void Clear () {
        enemies.Clear();
    }

    public void UpdateUnitSymbol () {
        symbolUnit.GenerateSymbols(enemies[0].keySymbols);
    }

    public void UpdateDisplayPosition () {
        //Averages XZ position, highest Y value
        float3 avg = float3.zero;
        for(int i = 0; i < enemies.Count; i++) {
            avg += new float3(enemies[i].transform.position.x, 0f, enemies[i].transform.position.z);
            avg.y = math.max(avg.y, enemies[i].transform.position.y);
        }
        avg = new float3(avg.x / enemies.Count, avg.y, avg.z / enemies.Count);

        symbolUnit.transform.position = avg + math.up() * 1.5f;
    }
}