using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGameObject : MonoBehaviour {

    public SymbolAnimator symbolAnimator;
    public PlayerController controller;
    public UserData userData;
    public bool isSelfControlled;
    public ulong clientId;

    public void Setup () {
        if(isSelfControlled)
            World.inst.camFollow.target = transform;
    }
}
