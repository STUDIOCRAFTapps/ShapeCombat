using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-120)]
public class AutoStarter : MonoBehaviour {

    private static bool hasAutoStarted;
    private void Awake () {
        if(hasAutoStarted)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;

        if(NetworkAssistant.inst == null) {
            DontDestroyOnLoad(this);
        }

        SceneManager.LoadScene("Lobby");
    }

    private void OnSceneLoaded (Scene scene, LoadSceneMode mode) {
        if(scene.name == "Lobby") {
            hasAutoStarted = true;

            StartCoroutine(OnLoadSceneCoroutine());
        }
    }

    IEnumerator OnLoadSceneCoroutine () {
        yield return new WaitForEndOfFrame();
        AutoStart();
    }

    private void AutoStart () {
        MainMenu.inst.StartLobby();
        MainMenu.inst.StartMatch();
    }

    private void OnDestroy () {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
