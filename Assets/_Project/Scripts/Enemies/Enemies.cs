using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Enemies : MonoBehaviour {

    public ulong id { get; private set; }
    public ulong symbolHash { get; private set; }
    public int symbolUnitGroup;

    public EnnemyNavigator navigator { get; private set; }
    public EnemyAnimator animator { get; private set; }

    public List<Symbols> keySymbols;

    private void Awake () {
        keySymbols = new List<Symbols>();
        navigator = GetComponent<EnnemyNavigator>();
        animator = GetComponent<EnemyAnimator>();

        keySymbols.Add(Symbols.VLine);
        keySymbols.Add(Symbols.HLine);
        keySymbols.Add(Symbols.VLine);
        keySymbols.Add(Symbols.BottomHat);
        keySymbols.Add(Symbols.TopHat);
    }

    public void CalculateSymbolHash () {
        symbolHash = GetSymbolsHash();
    }

    private ulong GetSymbolsHash () {
        ulong hash = 0;
        for(int i = 0; i < keySymbols.Count; i++) {
            hash |= (ulong)keySymbols[i] << (i * 4);
        }
        return hash;
    }

    private void Start () {
        EnemyManager.RegisterEnemy(this);
        OnSpawn();
    }

    private void OnDestroy () {
        EnemyManager.UnregisterEnemy(this);
    }

    private void Update () {
        UpdateVisuals();
    }


    public void ApplyImpulse (Vector3 impulse) {
        navigator.ApplyImpulse(impulse);
    }

    public bool TryDamage (Symbols symbol) {
        if(keySymbols.Count == 0)
            return false;

        if(symbol == keySymbols[0]) {
            keySymbols.RemoveAt(0);
            if(keySymbols.Count == 0)
                OnDeath();
        } else {
            return false;
        }

        OnDamaged();
        return true;
    }

    public virtual void OnDamaged () {
        animator.Flash(0.1f);
    }

    public virtual void OnDeath () {
        navigator.isFreezed = true;
        animator.PlayDeathAnimation();
    }

    public virtual void OnSpawn () {

    }

    public virtual void OnNavigatorJump () {

    }

    public virtual void OnNavigatorNearTarget () {

    }

    public virtual void UpdateVisuals () {
        animator.UpdateAnimation();
    }

}
