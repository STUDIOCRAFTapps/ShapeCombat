using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hectopus : Enemies {

    [Header("Parameters")]
    public float attackInterval = 3f;
    public float dashLoop = 2f;
    public float maxDashSpeed = 50f;

    private float lastAttackTime;
    private float dashLoopTime;

    public override void UpdateVisuals () {
        animator.SetLoopAnimationId((int)navigator.enemyNavigatorState, navigator.isGoingRight);
        base.UpdateVisuals();
    }

    public override void OnFixedUpdate () {
        if(!navigator.isGrounded) {
            navigator.maxMoveStepPerSecond = 64;
        } else {
            dashLoopTime = Mathf.Repeat(dashLoopTime + Time.deltaTime, dashLoop);
            float valueTime = dashLoopTime / dashLoop;
            navigator.maxMoveStepPerSecond = Mathf.Lerp(0f, maxDashSpeed, valueTime * valueTime * valueTime);
        }
    }

    public override void OnNavigatorJump () {
    }

    public override void OnNavigatorNearTarget () {
        if(Time.fixedTime - lastAttackTime > attackInterval) {
            lastAttackTime = Time.fixedTime;

            animator.PlayAnimation(0);
        }
    }
}
