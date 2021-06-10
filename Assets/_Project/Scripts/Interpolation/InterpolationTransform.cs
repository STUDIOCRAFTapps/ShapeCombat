using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[DefaultExecutionOrder(-29)]
public class InterpolationTransform : MonoBehaviour {

    private void OnEnable () {
        InterpolationManager.inst.RegisterTransform(this);
    }

    private void OnDisable () {
        InterpolationManager.inst?.UnregisterTransform(this);
    }

    private bool hasBeenInitied;
    private TransformState oldState;
    private TransformState newState;
    private Vector3 posOffset;

    public void SetOffset (Vector3 offset) {
        posOffset += offset;
    }

    public void CopyState () {
        oldState = newState;
        newState = new TransformState(transform);
        if(!hasBeenInitied) {
            hasBeenInitied = true;
            oldState = newState;
        }
    }

    public void ApplyState () {
        newState.Apply(transform);
    }

    public void ApplyInterpolatedState () {
        /*float lerpBlend = 1f - math.pow(1f - InterpolationManager.inst.offsetSmooth, Time.deltaTime * InterpolationManager.inst.offsetSpeed);
        posOffset = math.lerp(posOffset, float3.zero, lerpBlend);*/
        posOffset = UnityEngine.Vector3.MoveTowards(posOffset, float3.zero, InterpolationManager.inst.offsetSmooth);

        transform.position = Vector3.Lerp(
                                    oldState.position,
                                    newState.position,
                                    InterpolationManager.interpolationFactor) + posOffset;
        transform.rotation = Quaternion.Slerp(
                                    oldState.rotation,
                                    newState.rotation,
                                    InterpolationManager.interpolationFactor);
    }
}



struct TransformState {
    public Vector3 position;
    public Quaternion rotation;

    public TransformState (Transform transform) {
        position = transform.position;
        rotation = transform.rotation;
    }

    public void Apply (Transform transform) {
        transform.position = position;
        transform.rotation = rotation;
    }

    public static TransformState empty => new TransformState() { position = Vector3.zero, rotation = Quaternion.identity };
}
