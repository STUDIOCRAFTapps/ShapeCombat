using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class SymbolUnit : MonoBehaviour {

    public SymbolUnitSprites symbolsPrefab;
    public List<SymbolUnitSprites> symbolsRoots = new List<SymbolUnitSprites>();

    public void GenerateSymbols (List<Symbols> symbolsList) {
        for(int i = 0; i < symbolsRoots.Count; i++) {
            if(i >= symbolsList.Count) {
                symbolsRoots[i].gameObject.SetActive(false);
            }
        }
        int symbolsToGenerate = math.max(0, symbolsList.Count - symbolsRoots.Count);
        for(int i = 0; i < symbolsToGenerate; i++) {
            symbolsRoots.Add(Instantiate(symbolsPrefab, transform));
        }
        for(int i = 0; i < symbolsList.Count; i++) {
            if(!symbolsRoots[i].gameObject.activeSelf)
                symbolsRoots[i].gameObject.SetActive(true);
            symbolsRoots[i].SetSprites(SymbolUtility.GetSymbolSprite(symbolsList[i], SymbolSpriteSize.Small));
            symbolsRoots[i].transform.localPosition = (symbolsList.Count * -0.25f + i * 0.5f + 0.25f) * Vector3.right;
        }
    }
}
