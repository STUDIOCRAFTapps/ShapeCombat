using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SymbolUnitSprites : MonoBehaviour {
    public SpriteRenderer main;
    public SpriteRenderer outline0;
    public SpriteRenderer outline1;
    public SpriteRenderer outline2;
    public SpriteRenderer outline3;

    public void SetSprites (Sprite sprite) {
        main.sprite = sprite;
        outline0.sprite = sprite;
        outline1.sprite = sprite;
        outline2.sprite = sprite;
        outline3.sprite = sprite;
    }
}
