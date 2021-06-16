using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;

public class WorldSync : NetworkBehaviour {

    private List<ulong> clientKeys;
    private static WorldSync inst;
    private void Awake () {
        inst = this;
        CustomMessagingManager.RegisterNamedMessageHandler("GetEnemyPositions", GetEnemyPositions);
    }

    private void Start () {
        clientKeys = new List<ulong>();
        foreach(KeyValuePair<ulong, LocalPlayer> kvp in Lobby.PlayerDictionary) {
            clientKeys.Add(kvp.Key);
        }
    }

    private int posClock;
    private void FixedUpdate () {
        if(!IsServer)
            return;

        if(posClock >= 5) {
            posClock = 0;
            int updateCount = EnemyManager.DoUpdatePositions();
            if(updateCount > 0) {
                using PooledNetworkBuffer stream = PooledNetworkBuffer.Get();
                using PooledNetworkWriter writer = PooledNetworkWriter.Get(stream);
                EnemyManager.SerializeEnemyPositions(updateCount, writer);
                CustomMessagingManager.SendNamedMessage("GetEnemyPositions", clientKeys, stream, MLAPI.Transports.NetworkChannel.PositionUpdate);
            }
        } else {
            posClock++;
        }
    }

    private void OnDestroy () {
        
    }

    #region Spawn Enemy RPC
    public static void SpawnEnemySync (Vector3 position) {
        inst.SpawnEnemyServerRpc(position, new ServerRpcParams());
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void SpawnEnemyServerRpc (Vector3 position, ServerRpcParams rpcParams = default) {
        ushort id = EnemyManager.GetEnemyId();
        EnemyManager.SpawnEnemy(id, position, out int target);

        SpawnEnemyClientRpc(id, position, target);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SpawnEnemyClientRpc (ushort id, Vector3 position, int target, ClientRpcParams clientRpcParams = default) {
        if(IsServer)
            return;

        EnemyManager.SpawnEnemy(id, position, out int _);
        EnemyManager.SetEnemyTarget(id, target);
    }
    #endregion

    #region Enemy Target Switch RPC
    public static void SetEnemyTarget (ushort id, int target) {
        inst.SetEnemyTargetClientRpc(id, target);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void SetEnemyTargetClientRpc (ushort id, int target, ClientRpcParams clientRpcParams = default) {
        if(IsServer)
            return;

        EnemyManager.SetEnemyTarget(id, target);
    }
    #endregion

    #region Resync Enemy Position
    private void GetEnemyPositions (ulong senderClientId, Stream stream) {
        using PooledNetworkReader reader = PooledNetworkReader.Get(stream);
        EnemyManager.DeserializeEnemyPositions(reader);
    }
    #endregion
}
