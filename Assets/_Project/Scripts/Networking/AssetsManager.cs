using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetsManager : MonoBehaviour {

    public PlayerGameObject playerObjectPrefab;
    public Lobby lobbyObjectPrefab;

    public static AssetsManager inst { private set; get; }
    void Awake () {
        inst = this;
    }

}
