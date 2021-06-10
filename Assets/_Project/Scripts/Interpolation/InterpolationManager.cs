using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-30)]
public class InterpolationManager : MonoBehaviour {

    public float offsetSmooth = 0.2f;
    public float offsetSpeed = 30f;

    public float timeScale = 1f;
    private Dictionary<int, InterpolationTransform> transforms;

    public static event Action postInterpolation;


    public static InterpolationManager inst;
    private void Awake () {
        inst = this;

        transforms = new Dictionary<int, InterpolationTransform>();
        
        lastFixedUpdateTimes = new float[2];
        newTimeIndex = 0;
    }

    private void Start () {
        RenderPipelineManager.beginCameraRendering += PreRenderEvent;
        RenderPipelineManager.endCameraRendering += PostRenderEvent;
    }

    public void RegisterTransform (InterpolationTransform interpTransform) {
        if(!transforms.ContainsKey(interpTransform.GetInstanceID()))
            transforms.Add(interpTransform.GetInstanceID(), interpTransform);
    }

    public void UnregisterTransform (InterpolationTransform interpTransform) {
        transforms.Remove(interpTransform.GetInstanceID());
    }

    private void OnDestroy () {
        RenderPipelineManager.beginCameraRendering -= PreRenderEvent;
        RenderPipelineManager.endCameraRendering -= PostRenderEvent;
    }



    #region Preparing Interpolation
    public static float interpolationFactor { get; private set; }
    private float[] lastFixedUpdateTimes;
    bool hasFixedUpdatePassed;
    private void FixedUpdate () {
        hasFixedUpdatePassed = true;

        newTimeIndex = oldTimeIndex;
        lastFixedUpdateTimes[newTimeIndex] = Time.fixedTime;

        float newerTime = lastFixedUpdateTimes[newTimeIndex];
        float olderTime = lastFixedUpdateTimes[oldTimeIndex];
    }

    private void Update () {
        Time.timeScale = timeScale;

        float newerTime = lastFixedUpdateTimes[newTimeIndex];
        float olderTime = lastFixedUpdateTimes[oldTimeIndex];

        if(newerTime != olderTime) {
            interpolationFactor = (Time.time - newerTime) / (newerTime - olderTime);
        } else {
            interpolationFactor = 1;
        }
    }

    private int newTimeIndex;
    private int oldTimeIndex {
        get {
            return (newTimeIndex == 0 ? 1 : 0);
        }
    }
    #endregion



    private void PreRenderEvent (ScriptableRenderContext context, Camera cam) {
        if(hasFixedUpdatePassed) {
            hasFixedUpdatePassed = false;
            foreach(KeyValuePair<int, InterpolationTransform> kvp in transforms) {
                kvp.Value.CopyState();
            }
        }
        foreach(KeyValuePair<int, InterpolationTransform> kvp in transforms) {
            kvp.Value.ApplyInterpolatedState();
        }
        postInterpolation?.Invoke();
    }

    private void PostRenderEvent (ScriptableRenderContext context, Camera cam) {
        foreach(KeyValuePair<int, InterpolationTransform> kvp in transforms) {
            kvp.Value.ApplyState();
        }
    }
}
