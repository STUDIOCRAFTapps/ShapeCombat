using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour {

    public Camera menuCamera;
    public Color defaultColor;
    public Color lobbyColor;
    public TMP_InputField ipField;
    public TMP_InputField portField;

    private float lobbyColorFade;
    private void Update () {
        lobbyColorFade = Mathf.Clamp01(lobbyColorFade + (Lobby.DoesLobbyExist ? 1f : -1f) * Time.deltaTime);
        menuCamera.backgroundColor = Color.Lerp(defaultColor, lobbyColor, lobbyColorFade);
    }


    public void StartLobby () {
        NetworkAssistant.inst.StartHost(ipField.text, ushort.Parse(portField.text));
    }

    public void JoinLobby () {
        NetworkAssistant.inst.StartClient(ipField.text, ushort.Parse(portField.text));
    }

    public void StartMatch () {
        Lobby.StartMatch();
    }
}
