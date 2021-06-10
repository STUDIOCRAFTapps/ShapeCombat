using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationAsset", menuName = "Custom/Animation/Enemy Animation Asset")]
public class EnemyAnimationAsset : ScriptableObject {

    public bool isFirstRepeatClipBlinking;
    public AnimationAssetClip[] repeatClips;
    public AnimationAssetClip[] actionClips;
    public Sprite[] deathAnimation;
}
