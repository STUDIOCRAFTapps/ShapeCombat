using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SymbolAnimator : MonoBehaviour {
    
    public SpriteRenderer[] symbolRender;
    private float fade;
    
    

    public void OnExecuteSymbol (Symbols symbol) {
        foreach(SpriteRenderer sr in symbolRender) {
            sr.sprite = SymbolUtility.GetSymbolSprite(symbol, SymbolSpriteSize.Large);
        }
        fade = 2f;
    }
    
    void Update () {
        fade = Mathf.Max(fade - Time.deltaTime * 4f, 0f);
        foreach(SpriteRenderer sr in symbolRender) {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Min(1f, fade));
        }
    }
}
