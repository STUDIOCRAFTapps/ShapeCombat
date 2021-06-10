using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using Unity.Collections;

using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;

[DefaultExecutionOrder(1)]
public class LocalPlayer : NetworkBehaviour {

    // Objects owned by this local player object
    [HideInInspector] public PlayerGameObject playerObject;
    [HideInInspector] public bool IsSelfControlled { private set; get; }
    private bool playerAddedToLobby;

    // Network Variables
    public NetworkVariable<UserData> userData = new NetworkVariable<UserData>(new NetworkVariableSettings() {
        SendNetworkChannel = MLAPI.Transports.NetworkChannel.ReliableRpc,
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public NetworkVariable<bool> symbolDrawState = new NetworkVariable<bool>(new NetworkVariableSettings() {
        SendNetworkChannel = MLAPI.Transports.NetworkChannel.ReliableRpc,
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    #region Setup
    void OnEnable () {
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    void OnDisable () {
        SceneManager.sceneUnloaded += OnSceneUnload;
    }

    void OnSceneLoad (Scene scene, LoadSceneMode mode) {
        if(IsOwner && scene.name == "Main") {
            StartCoroutine(SubscribeSceneEvents());
        }
    }

    void OnSceneUnload (Scene scene) {
        if(IsOwner && scene.name == "Main" && ShapeDrawSystem4.inst != null) {
            ShapeDrawSystem4.inst.executeSymbol -= OnExecuteSymbolLocal;
        }
    }

    IEnumerator SubscribeSceneEvents () {
        yield return new WaitForEndOfFrame();
        if(ShapeDrawSystem4.inst != null)
            ShapeDrawSystem4.inst.executeSymbol += OnExecuteSymbolLocal;
    }

    public override void NetworkStart () {
        DontDestroyOnLoad(this);

        // Setup user data networked variable
        userData.SetNetworkBehaviour(this);
        userData.OnValueChanged += OnUserDataChanged;
        symbolDrawState.OnValueChanged += SymbolDrawStateEvent;
        if(NetworkAssistant.IsHost && NetworkAssistant.ClientID == OwnerClientId) {
            userData.Value = new UserData("Generic", 0, OwnerClientId);
        }

        playerAddedToLobby = Lobby.RegisterLocalPlayer(this);
    }

    private void FixedUpdate () {
        if(!playerAddedToLobby) {
            playerAddedToLobby = Lobby.RegisterLocalPlayer(this);
        }

        if(ShapeDrawSystem4.inst != null) {
            if(IsOwner && symbolDrawState.Value != ShapeDrawSystem4.inst.isDrawing) {
                playerObject.playerAnimator.isDrawing = ShapeDrawSystem4.inst.isDrawing;
                symbolDrawState.Value = ShapeDrawSystem4.inst.isDrawing;
            }
        }

        if(playerObject != null && IsOwner)
            SyncPlayerServerRpc(playerObject.transform.position, playerObject.controller.velocity, new Vector2(playerObject.controller.lastestDirection.x, playerObject.controller.lastestDirection.z));
    }

        // Clean ups when this player object gets removed
    private void OnDestroy () {
        DespawnPlayerObject();

        userData.OnValueChanged -= OnUserDataChanged;
        symbolDrawState.OnValueChanged -= SymbolDrawStateEvent;

        Lobby.RemoveLocalPlayer(this);
    }
    #endregion

    #region Match Events            (Called by lobby)
    // To be called when a match is started
    public void OnStartMatch () {
        SpawnPlayerObject();
    }


    // To be called when a match ends
    public void OnEndMatch () {
        DespawnPlayerObject();
    }
    #endregion
    
    #region Player Object Events    (Self managed by Local Player)
    /// <summary>
    /// Spawn the actual player object and adds it to the entity store
    /// </summary>
    private void SpawnPlayerObject () {
        playerObject = Instantiate(AssetsManager.inst.playerObjectPrefab);
        playerObject.transform.position = new Vector3(-12f, 9f, -5f);
        playerObject.isSelfControlled = IsOwner;
        playerObject.userData = userData.Value;
        playerObject.clientId = OwnerClientId;
        playerObject.Setup();
    }


    /// <summary>
    /// Remove the actual player object and removes it to the entity store
    /// </summary>
    private void DespawnPlayerObject () {
        if(playerObject != null) {
            Destroy(playerObject.gameObject);
        }
    }
    #endregion

    #region User Data
    public void OnUserDataChanged (UserData lastUserData, UserData userData) {
        
    }
    #endregion



    #region Symbol
    void OnExecuteSymbolLocal (Symbols symbol) {
        if(playerObject == null)
            return;

        playerObject.symbolAnimator.OnExecuteSymbol(symbol);
        SyncSymbolServerRpc((byte)symbol);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    private void SyncSymbolServerRpc (byte symbolId, ServerRpcParams rpcParams = default) {
        SyncSymbolClientRpc(symbolId, new ClientRpcParams() {
            Receive = new ClientRpcReceiveParams() {
                UpdateStage = NetworkUpdateStage.PreUpdate
            }
        });

        Vector2 playerPosXZ = new Vector2(playerObject.transform.position.x, playerObject.transform.position.z);
        float knockbackConstXZ = 30f;
        float knockbackConstY = 10f;

        EnemyManager.ForEachCloseEnemy((e) => {
        
            if(e.TryDamage((Symbols)symbolId)) {
                Vector2 impactDirection = -(playerPosXZ - new Vector2(e.transform.position.x, e.transform.position.z)).normalized;

                e.ApplyImpulse(new Vector3(impactDirection.x * knockbackConstXZ, knockbackConstY, impactDirection.y * knockbackConstXZ));
            }

        }, playerPosXZ, 30f);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void SyncSymbolClientRpc (byte symbolId, ClientRpcParams clientRpcParams = default) {
        if(IsOwner)
            return;
        if(playerObject == null)
            return;

        playerObject.symbolAnimator.OnExecuteSymbol((Symbols)symbolId);
    }

    private void SymbolDrawStateEvent (bool oldState, bool newState) {
        if(!IsOwner)
            playerObject.playerAnimator.isDrawing = newState;
    }
    #endregion

    #region Sync Players
    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    private void SyncPlayerServerRpc (Vector3 position, Vector3 velocity, Vector2 direction, ServerRpcParams rpcParams = default) {
        var executingRpcSender = rpcParams.Receive.SenderClientId;
        SyncPlayerClientRpc(position, velocity, direction, new ClientRpcParams() {
            Receive = new ClientRpcReceiveParams() {
                UpdateStage = NetworkUpdateStage.PreUpdate
            }
        });
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void SyncPlayerClientRpc (Vector3 position, Vector3 velocity, Vector2 direction, ClientRpcParams clientRpcParams = default) {
        if(IsOwner)
            return;
        if(playerObject == null)
            return;

        playerObject.controller.SetState(position, velocity, new Vector3(direction.x, 0f, direction.y));
    }
    #endregion
}
