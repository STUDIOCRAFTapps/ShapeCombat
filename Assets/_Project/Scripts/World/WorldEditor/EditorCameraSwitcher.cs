using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorCameraSwitcher : MonoBehaviour {

    private int currentCamera;
    public GameObject[] cameras;

    void Update () {
        if(Input.GetKeyDown(KeyCode.Alpha1)) {
            currentCamera++;
            if(currentCamera >= cameras.Length)
                currentCamera = 0;
            for(int i = 0; i < cameras.Length; i++) {
                cameras[i].SetActive(i == currentCamera);
            }
        }
    }
}
