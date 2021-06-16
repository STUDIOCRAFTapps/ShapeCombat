using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finless : Enemies {

    [Header("Parameters")]
    public float attackInterval = 3f;


    private float lastAttackTime;

    public override void UpdateVisuals () {
        animator.SetLoopAnimationId((int)navigator.enemyNavigatorState, navigator.isGoingRight);
        base.UpdateVisuals();
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
