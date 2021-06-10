using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupDecal : MonoBehaviour
{
    public MeshRenderer material;

    // Start is called before the first frame update
    void Update () {
        material.material.SetMatrix("_InverseView", Camera.main.cameraToWorldMatrix);
    }
}
