using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour {


    public ShapeDrawSystem4 shapeDraw;
    public PlayerController playerController;
    new public SpriteRenderer renderer;
    public AnimationAsset playerAnimationAsset;

    private bool wasMoving;
    private bool wasDrawing;
    private float time;
    private float blinkTime;

    const float maxBlinkTime = 4f;

    void Update () {
        if(wasDrawing != shapeDraw.isDrawing) {
            time = 0f;
        } else if(!wasMoving && playerController.isMoving) {
            time = 0f;
        }

        if(shapeDraw.isDrawing) {
            Sprite[] summonSprites = playerAnimationAsset.clips[0].frames;
            time = Mathf.Repeat(time, summonSprites.Length);
            renderer.sprite = summonSprites[(int)time];
            time = Mathf.Repeat(time + Time.deltaTime * playerAnimationAsset.framesPerSecond, summonSprites.Length);
        } else if(!playerController.isMoving) {
            blinkTime = Mathf.Repeat(blinkTime, maxBlinkTime);
            int roundedFrame = Mathf.FloorToInt(blinkTime * playerAnimationAsset.framesPerSecond);
            int spriteIndex = 0;
            if(roundedFrame <= 2) {
                spriteIndex = roundedFrame;
            } else if(roundedFrame <= 4) {
                spriteIndex = 5 - roundedFrame;
            } else {
                spriteIndex = 0;
            }
            blinkTime = Mathf.Repeat(blinkTime + Time.deltaTime, maxBlinkTime);

            Sprite[] idleSprites = playerAnimationAsset.clipsDirectional[0].GetSpriteArrayFromDirection(playerController.visualDirection);
            renderer.sprite = idleSprites[spriteIndex];
        } else if(playerController.isMoving) {
            Sprite[] walkSprites = playerAnimationAsset.clipsDirectional[1].GetSpriteArrayFromDirection(playerController.visualDirection);
            time = Mathf.Repeat(time, walkSprites.Length); 
            renderer.sprite = walkSprites[(int)time];
            if(!playerController.isGrounded) {
                time = Mathf.Repeat(time + Time.deltaTime * playerAnimationAsset.framesPerSecond * 1.5f, walkSprites.Length);
            } else {
                time = Mathf.Repeat(time + Time.deltaTime * playerAnimationAsset.framesPerSecond, walkSprites.Length);
            }
        }

        wasMoving = playerController.isMoving;
        wasDrawing = shapeDraw.isDrawing;
    }
}
