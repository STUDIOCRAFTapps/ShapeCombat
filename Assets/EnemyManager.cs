using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization.Pooled;

public class EnemyManager : MonoBehaviour {

    new public Camera camera;
    private Dictionary<ushort, Enemies> enemies = new Dictionary<ushort, Enemies>();
    private static EnemyManager inst;
    private ushort currentEnemyId;

    [Header("Prefabs")]
    public Enemies buddeyePrefab;

    [Header("Particles")]
    public ParticleSystem smallExplosion;

    private void Awake () {
        inst = this;
        enemies = new Dictionary<ushort, Enemies>();
    }

    private void Update () {
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            kvp.Value.OnUpdate();
        }
    }

    private void FixedUpdate () {
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            kvp.Value.Internal_OnFixedUpdate();
        }
    }


    public static int DoUpdatePositions () {
        int count = 0;
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            if(kvp.Value.DoUpdatePos) {
                count++;
            }
        }
        return count;
    }

    public static void SerializeEnemyPositions (int count, PooledNetworkWriter writer) {
        writer.WriteInt32(count);
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            if(kvp.Value.DoUpdatePos) {
                writer.WriteUInt16(kvp.Value.id);
                writer.WriteVector3(kvp.Value.transform.position);
                writer.WriteVector3(kvp.Value.navigator.velocity);

                kvp.Value.ResetPosUpdate();
            }
        }
    }

    public static void DeserializeEnemyPositions (PooledNetworkReader reader) {
        int count = reader.ReadInt32();
        for(int i = 0; i < count; i++) {
            ushort id = reader.ReadUInt16();
            Vector3 pos = reader.ReadVector3();
            Vector3 vel = reader.ReadVector3();

            if(inst.enemies.TryGetValue(id, out Enemies value)) {
                Vector3 diff = value.transform.position - pos;
                value.transform.position = pos; // Interpolata :O
                value.interpolationTransform.SetOffset(diff);
                value.navigator.velocity = vel;
            }
        }
    }




    public static void ClearUnitGroups () {
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            kvp.Value.symbolUnitGroup = -1;
        }
    }

    public static void ForEachOnScreenEnemy (Action<Enemies> action) {
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            Vector3 cameraPoint = inst.camera.WorldToViewportPoint(kvp.Value.transform.position);
            if(cameraPoint.x >= 0f && cameraPoint.y >= 0f && cameraPoint.x <= 1f && cameraPoint.y <= 1f && cameraPoint.z > 0f) {
                action(kvp.Value);
            }
        }
    }

    public static void ForEachCloseEnemy (Action<Enemies> action, Vector2 playerPos, float radius) {
        foreach(KeyValuePair<ushort, Enemies> kvp in inst.enemies) {
            float distSq = (new Vector2(kvp.Value.transform.position.x, kvp.Value.transform.position.z) - playerPos).sqrMagnitude;
            if(distSq < radius * radius) {
                action(kvp.Value);
            }
        }
    }

    public static ushort GetEnemyId () {
        inst.currentEnemyId++;
        return (ushort)(inst.currentEnemyId - 1);
    }

    public static void RegisterEnemy (Enemies enemy) {
        inst.enemies.Add(enemy.id, enemy);
    }

    public static void UnregisterEnemy (Enemies enemy) {
        if(inst == null)
            return;
        if(inst.enemies == null)
            return;
        inst.enemies.Remove(enemy.id);
    }

    public static void SpawnEnemy (ushort id, Vector3 position, out int target) {
        Enemies enemy = Instantiate(inst.buddeyePrefab, position, Quaternion.identity);
        enemy.Init(id);
        target = enemy.navigator.SetFirstTarget();
    }

    public static void SetEnemyTarget (ushort id, int playerId) {
        if(inst.enemies.TryGetValue(id, out Enemies enemy)) {
            if(playerId == -1) {
                enemy.navigator.target = null;
            } else if(Lobby.PlayerDictionary.TryGetValue((ushort)playerId, out LocalPlayer localPlayer)) {
                enemy.navigator.target = localPlayer.playerObject.transform;
            } else {
                Debug.Log($"Missing target: {playerId}");
            }
        } else {
            Debug.Log($"Missing enemy: {id}");
        }
    }

    public static void PlayParticle (Vector3 position) {
        inst.smallExplosion.transform.position = position;
        inst.smallExplosion.Emit(1);
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
