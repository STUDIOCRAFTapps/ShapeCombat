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

    #region Setup
    public override void NetworkStart () {
        DontDestroyOnLoad(this);

        // Setup user data networked variable
        userData.SetNetworkBehaviour(this);
        userData.OnValueChanged += OnUserDataChanged;
        if(NetworkAssistant.IsHost && NetworkAssistant.ClientID == OwnerClientId) {
            userData.Value = new UserData("Generic", 0, OwnerClientId);
        }

        playerAddedToLobby = Lobby.RegisterLocalPlayer(this);
    }

    private void FixedUpdate () {
        if(!playerAddedToLobby) {
            playerAddedToLobby = Lobby.RegisterLocalPlayer(this);
        }
    }

        // Clean ups when this player object gets removed
    private void OnDestroy () {
        DespawnPlayerObject();

        userData.OnValueChanged -= OnUserDataChanged;

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
}
