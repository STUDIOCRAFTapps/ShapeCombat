using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationAsset", menuName = "Custom/Animation/Animation Asset")]
public class AnimationAsset : ScriptableObject {
    public int framesPerSecond = 15;

    public AnimationAssetClip[] clips;
    public AnimationAssetDirectionalClip[] clipsDirectional;
}

[Serializable]
public class AnimationAssetClip {
    public string key;
    public Sprite[] frames;
}

[Serializable]
public class AnimationAssetDirectionalClip {
    public string key;
    public Sprite[] framesUp;
    public Sprite[] framesDown;
    public Sprite[] framesLeft;
    public Sprite[] framesRight;

    public Sprite[] GetSpriteArrayFromDirection (Direction direction) {
        switch(direction) {
            case Direction.Up:
            return framesUp;
            case Direction.Down:
            return framesDown;
            case Direction.Left:
            return framesLeft;
            case Direction.Right:
            return framesRight;
        }
        return framesUp;
    }
}