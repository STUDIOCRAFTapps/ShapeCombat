using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Spawning;
using MLAPI.Serialization.Pooled;
using System;

[DefaultExecutionOrder(1)]
public class NetworkAssistant : MonoBehaviour {

    private Lobby lobby;

    public static NetworkAssistant inst;
    private void Awake () {
        if(inst != null) {
            return;
        }
        inst = this;

        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    #region Getters
    public static bool IsServer {
        get {
            return NetworkManager.Singleton.IsServer;
        }
    }

    public static float Time {
        get {
            return NetworkManager.Singleton.NetworkTime;
        }
    }

    public static float Ping {
        get {
            var transp = ((MLAPI.Transports.UNET.UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
            return transp.GetCurrentRtt(ClientID);
        }
    }

    public static bool IsHost {
        get {
            return NetworkManager.Singleton.IsHost;
        }
    }

    public static bool IsClient {
        get {
            return NetworkManager.Singleton.IsClient;
        }
    }

    public static bool IsClientNotHost {
        get {
            return NetworkManager.Singleton.IsClient && !IsHost;
        }
    }

    public static ulong ClientID {
        get {
            return NetworkManager.Singleton.LocalClientId;
        }
    }

    public static int PlayerCount {
        get {
            return NetworkManager.Singleton.ConnectedClients.Count;
        }
    }

    public static LocalPlayer LocalPlayer {
        get {
            return NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<LocalPlayer>();
        }
    }

    public static int FrameIndex {
        get {
            return (int)math.floor(NetworkManager.Singleton.NetworkTime * 60f);
        }
    }

    public static ulong GetPlayerRTT (ulong clientID) {
        var transp = ((MLAPI.Transports.UNET.UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
        if(IsHost && clientID == ClientID) {
            return 0;
        }
        return transp.GetCurrentRtt(clientID);
    }
    #endregion


    #region Connection Approval Events
    private void ApprovalCheck (byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback) {
        //Your logic here
        bool approve = true;
        bool createPlayerObject = true;

        //If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, null, approve, Vector3.zero, Quaternion.identity);
    }
    #endregion

    #region Client Connection Events
    private void OnClientConnect (ulong clientID) {
    }

    private void OnClientDisconnected (ulong clientID) {
    }
    #endregion

    #region Scene Events
    private void OnSceneLoaded (Scene scene, LoadSceneMode mode) {
    }

    private void OnSceneUnloaded (Scene scene) {
    }
    #endregion


    #region Start Connections
    public void StartClient (string ip, ushort port) {
        //SceneManager.LoadScene("Main");

        var transp = ((MLAPI.Transports.UNET.UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkManager.Singleton.NetworkConfig;

        transp.ConnectAddress = ip;
        transp.ConnectPort = port;
        NetworkManager.Singleton.StartClient();
    }

    public void StartHost (string ip, ushort port) {
        //SceneManager.LoadScene("Main");

        var transp = ((MLAPI.Transports.UNET.UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkManager.Singleton.NetworkConfig;

        transp.ConnectAddress = ip;
        transp.ConnectPort = port;

        NetworkManager.Singleton.StartHost(Vector3.zero, Quaternion.identity, true, NetworkSpawnManager.GetPrefabHashFromGenerator("LocalPlayer"));
        DontDestroyOnLoad(NetworkSpawnManager.GetLocalPlayerObject().gameObject);

        lobby = Instantiate(AssetsManager.inst.lobbyObjectPrefab);
        DontDestroyOnLoad(lobby);
        lobby.GetComponent<NetworkObject>().Spawn();

        OnClientConnect(ClientID);
        //lobby.RegisterUserWithData(ClientID, selfUserData);
    }
    #endregion

    #region Exit Connection

    #endregion
}
