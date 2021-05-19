using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(2)]
public class WaterMover : MonoBehaviour {

    new public Transform camera;


    void Update () {
        transform.position = new Vector3(Mathf.RoundToInt(camera.transform.position.x / (10f / 3f)) * (10f / 3f), 0f, Mathf.RoundToInt((camera.transform.position.y * 0.5f + camera.transform.position.z + 20f) / (10f / 3f)) *  (10f / 3f));
    }
}
