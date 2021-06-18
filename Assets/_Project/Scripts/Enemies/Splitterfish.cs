using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Splitterfish : Enemies {

    [Header("Parameters")]
    public float attackInterval = 3f;


    private float lastAttackTime;

    public override void UpdateVisuals () {
        animator.SetLoopAnimationId(0, navigator.isGoingRight);
        base.UpdateVisuals();
    }

    public override void OnNavigatorJump () {
    }

    public override void OnNavigatorNearTarget () {
    }
}
