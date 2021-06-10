using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buddeye : Enemies {

    [Header("Parameters")]
    public float attackInterval = 0.8f;


    private float lastAttackTime;

    public override void UpdateVisuals () {
        animator.SetLoopAnimationId((int)navigator.enemyNavigatorState, navigator.isGoingRight);
        base.UpdateVisuals();
    }

    public override void OnNavigatorJump () {
        animator.PlayAnimation(0);
    }

    public override void OnNavigatorNearTarget () {
        if(Time.fixedTime - lastAttackTime > attackInterval) {
            lastAttackTime = Time.fixedTime;

            animator.PlayAnimation(1);
        }
    }
}
