using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SymbolSpriteSize {
    Small,
    Medium,
    Large
}

public class DrawingUtility : MonoBehaviour {

    [Header("Parameters")]
    public List<string> symbolKeys;
    public List<Sprite> smallSymbol;
    public List<Sprite> mediumSymbol;
    public List<Sprite> largeSymbol;

    private Dictionary<string, Sprite> smallPair; // The best kind
    private Dictionary<string, Sprite> mediumPair;
    private Dictionary<string, Sprite> largePair; 

    private static DrawingUtility inst;
    void Awake () {
        inst = this;
        smallPair = new Dictionary<string, Sprite>();
        mediumPair = new Dictionary<string, Sprite>();
        largePair = new Dictionary<string, Sprite>();
        int i = 0;
        foreach(string key in symbolKeys) {
            smallPair.Add(key, smallSymbol[i]);
            mediumPair.Add(key, mediumSymbol[i]);
            largePair.Add(key, largeSymbol[i]);
            i++;
        }
    }

    public static Sprite GetSymbolSprite (string key, SymbolSpriteSize size) {
        switch(size) {
            case SymbolSpriteSize.Small:
            return inst.smallPair[key];
            case SymbolSpriteSize.Medium:
            return inst.mediumPair[key];
            case SymbolSpriteSize.Large:
            return inst.largePair[key];
        }
        return null;
    }
}
