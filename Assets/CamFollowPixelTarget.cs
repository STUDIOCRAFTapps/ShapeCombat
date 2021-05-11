using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollowPixelTarget : MonoBehaviour {

    new public Camera camera;
    public Vector2 unitTarget;


    void Update () {
        transform.position = new Vector3(Mathf.FloorToInt(unitTarget.x * 16) / 16f, 0f, (Mathf.FloorToInt(unitTarget.y * 16f) / 8f));
    }
}
