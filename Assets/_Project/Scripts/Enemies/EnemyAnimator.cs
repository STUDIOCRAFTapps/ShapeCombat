using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour {

    public float framesPerSecond = 15f;
    public EnemyAnimationAsset animationAsset;
    new public SpriteRenderer renderer;
    public Enemies enemy;

    private int loopAnimationId;
    private float time;
    private float blinkTime;
    private int singleAnimationId = -1;
    private float timeSingle;
    private bool isDying;
    private float timeDeath;

    private float flashTime;

    const float maxBlinkTime = 4f;

    public void Flash (float flashTime) {
        this.flashTime = flashTime;
    }

    public void SetLoopAnimationId (int id, bool isLookingRight) {
        if(id != loopAnimationId) {
            loopAnimationId = id;
            time = 0f;
        }
        renderer.flipX = isLookingRight;
    }

    public void PlayAnimation (int id) {
        singleAnimationId = id;
        timeSingle = 0f;
    }

    public void PlayDeathAnimation () {
        isDying = true;
        timeDeath = 0f;
    }



    public void UpdateAnimation () {
        if(flashTime > 0f) {
            flashTime = Mathf.Max(0f, flashTime - Time.deltaTime);
            renderer.color = Color.white;
        } else {
            renderer.color = Color.clear;
        }

        if(isDying) {
            if(timeDeath < animationAsset.deathAnimation.Length) {
                timeDeath += Time.deltaTime * framesPerSecond;
                
                if(timeDeath >= animationAsset.deathAnimation.Length) {
                    enemy.OnDeathAnimationDone();
                } else {
                    renderer.sprite = animationAsset.deathAnimation[(int)timeDeath];
                }
            }
            return;
        }

        if(singleAnimationId != -1) {
            Sprite[] sprites = animationAsset.actionClips[singleAnimationId].frames;
            renderer.sprite = sprites[(int)timeSingle];

            timeSingle += Time.deltaTime * animationAsset.actionClips[singleAnimationId].customFramePerSecond;

            if(timeSingle >= sprites.Length) {
                singleAnimationId = -1;
            }
            return;
        }

        if(animationAsset.isFirstRepeatClipBlinking && loopAnimationId == 0) {
            blinkTime = Mathf.Repeat(blinkTime, maxBlinkTime);
            int roundedFrame = Mathf.FloorToInt(blinkTime * framesPerSecond);
            int spriteIndex = 0;
            if(roundedFrame <= 2) {
                spriteIndex = roundedFrame;
            } else if(roundedFrame <= 4) {
                spriteIndex = 5 - roundedFrame;
            } else {
                spriteIndex = 0;
            }
            blinkTime = Mathf.Repeat(blinkTime + Time.deltaTime, maxBlinkTime);

            Sprite[] sprites = animationAsset.repeatClips[loopAnimationId].frames;
            renderer.sprite = sprites[spriteIndex];
        } else {
            Sprite[] sprites = animationAsset.repeatClips[loopAnimationId].frames;
            time = Mathf.Repeat(time, sprites.Length);
            renderer.sprite = sprites[(int)time];

            time = Mathf.Repeat(time + Time.deltaTime * framesPerSecond, sprites.Length);
        }
    }
}
