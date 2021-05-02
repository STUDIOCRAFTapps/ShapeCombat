using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollowPixelTarget : MonoBehaviour {

    public Camera camera;
    public Vector2 unitTarget;
    public float left;
    public float right;
    public float top;
    public float bottom;


    void Update () {
        camera.projectionMatrix = Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);

        transform.position = new Vector3(Mathf.FloorToInt(unitTarget.x * 16) / 16, 0f, Mathf.FloorToInt(unitTarget.y * 16) / 16);
    }
}
