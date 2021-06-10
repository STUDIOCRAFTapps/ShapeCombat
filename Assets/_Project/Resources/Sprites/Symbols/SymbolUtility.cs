using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SymbolSpriteSize {
    Small,
    Medium,
    Large
}

public class SymbolUtility : MonoBehaviour {

    [Header("Parameters")]
    public List<Sprite> smallSymbol;
    public List<Sprite> mediumSymbol;
    public List<Sprite> largeSymbol;

    private Dictionary<Symbols, Sprite> smallPair; // The best kind
    private Dictionary<Symbols, Sprite> mediumPair;
    private Dictionary<Symbols, Sprite> largePair; 

    private static SymbolUtility inst;
    void Awake () {
        inst = this;
        smallPair = new Dictionary<Symbols, Sprite>();
        mediumPair = new Dictionary<Symbols, Sprite>();
        largePair = new Dictionary<Symbols, Sprite>();
        int i = 0;
        foreach(Sprite _ in smallSymbol) {
            smallPair.Add((Symbols)i, smallSymbol[i]);
            mediumPair.Add((Symbols)i, mediumSymbol[i]);
            largePair.Add((Symbols)i, largeSymbol[i]);
            i++;
        }
    }

    public static Sprite GetSymbolSprite (Symbols key, SymbolSpriteSize size) {
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
