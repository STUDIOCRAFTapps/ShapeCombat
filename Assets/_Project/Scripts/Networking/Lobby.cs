using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Messaging;

public class Lobby : NetworkBehaviour {

    public Dictionary<ulong, LocalPlayer> localPlayers { private set; get; }
    private bool wasInitied;

    public static bool DoesLobbyExist { get { return inst != null; } }
    public static Dictionary<ulong, LocalPlayer> PlayerDictionary { get { return inst.localPlayers; } }

    #region Init Lobby
    private static Lobby inst;
    public void Awake () {
        inst = this;
        localPlayers = new Dictionary<ulong, LocalPlayer>();

        // Add already connected clients (There should only be a host)
        foreach(MLAPI.Connection.NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) {
            LocalPlayer localPlayer = client.PlayerObject.GetComponent<LocalPlayer>();
            localPlayers.Add(localPlayer.OwnerClientId, localPlayer);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        if(!wasInitied)
            OnLobbyOpen();
        wasInitied = true;

        DontDestroyOnLoad(this);
    }

    public override void NetworkStart () {
        if(!wasInitied)
            OnLobbyOpen();
        wasInitied = true;
    }
    #endregion

    #region Exit Lobby
    private void OnDestroy () {
        OnLobbyClose();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    #endregion

    #region Users
    public static bool RegisterLocalPlayer (LocalPlayer localPlayer) {
        if(inst == null)
            return false;
        if(inst.localPlayers.ContainsKey(localPlayer.OwnerClientId))
            return true;
        inst.OnPlayerJoin(localPlayer);
        inst.localPlayers.Add(localPlayer.OwnerClientId, localPlayer);
        return true;
    }

    public static void RemoveLocalPlayer (LocalPlayer localPlayer) {
        if(inst == null)
            return;
        inst.OnPlayerLeave(localPlayer);
        inst.localPlayers.Remove(localPlayer.OwnerClientId);
    }
    #endregion

    #region Match
    public static void StartMatch () {
        inst.StartMatchClientRPC();
    }

    [ClientRpc]
    private void StartMatchClientRPC () {
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }
    #endregion



    #region Events
    void OnPlayerJoin (LocalPlayer localPlayer) {

    }
    void OnPlayerLeave (LocalPlayer localPlayer) {

    }
    void OnLobbyOpen () {

    }
    void OnLobbyClose () {

    }
    void OnSceneLoaded (Scene scene, LoadSceneMode lsm) {
        if(scene.name == "Main") {
            foreach(KeyValuePair<ulong, LocalPlayer> kvp in localPlayers) {
                kvp.Value.OnStartMatch();
            }
        }
    }
    void OnSceneUnloaded (Scene scene) {
        if(scene.name == "Main") {
            foreach(KeyValuePair<ulong, LocalPlayer> kvp in localPlayers) {
                kvp.Value.OnEndMatch();
            }
        }
    }
    #endregion
}
