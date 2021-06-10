using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {

    new public Camera camera;
    private Dictionary<ulong, Enemies> enemies = new Dictionary<ulong, Enemies>();
    private static EnemyManager inst;
    private ulong currentEnemyId;

    private void Awake () {
        inst = this;
        enemies = new Dictionary<ulong, Enemies>();
    }

    public static void ClearUnitGroups () {
        foreach(KeyValuePair<ulong, Enemies> kvp in inst.enemies) {
            kvp.Value.symbolUnitGroup = -1;
        }
    }

    public static void ForEachOnScreenEnemy (Action<Enemies> action) {
        foreach(KeyValuePair<ulong, Enemies> kvp in inst.enemies) {
            Vector3 cameraPoint = inst.camera.WorldToViewportPoint(kvp.Value.transform.position);
            if(cameraPoint.x >= 0f && cameraPoint.y >= 0f && cameraPoint.x <= 1 && cameraPoint.y <= 1f && cameraPoint.z > 0f) {
                action(kvp.Value);
            }
        }
    }

    public static void ForEachCloseEnemy (Action<Enemies> action, Vector2 playerPos, float radius) {
        foreach(KeyValuePair<ulong, Enemies> kvp in inst.enemies) {
            float distSq = (new Vector2(kvp.Value.transform.position.x, kvp.Value.transform.position.z) - playerPos).sqrMagnitude;
            if(distSq < radius * radius) {
                action(kvp.Value);
            }
        }
    }

    public static ulong RegisterEnemy (Enemies enemy) {
        inst.enemies.Add(inst.currentEnemyId, enemy);
        inst.currentEnemyId++;
        return inst.currentEnemyId - 1;
    }

    public static void UnregisterEnemy (Enemies enemy) {
        if(inst == null)
            return;
        if(inst.enemies == null)
            return;
        inst.enemies.Remove(enemy.id);
    }



    public static PlayerGameObject GetClosestPlayer (Vector3 pos, out float distance) {
        float minDistance = float.PositiveInfinity;
        ulong minPlayerIndex = 0;

        foreach(KeyValuePair<ulong, LocalPlayer> kvp in Lobby.PlayerDictionary) {
            Vector3 pPos = kvp.Value.playerObject.transform.position;
            float dist = (pos - pPos).sqrMagnitude;
            if(dist < minDistance) {
                minDistance = dist;
                minPlayerIndex = kvp.Value.OwnerClientId;
            }
        }

        if(minDistance != float.PositiveInfinity) {
            distance = minDistance;
            return Lobby.PlayerDictionary[minPlayerIndex].playerObject;
        } else {
            distance = 0f;
            return null;
        }
    }
}
