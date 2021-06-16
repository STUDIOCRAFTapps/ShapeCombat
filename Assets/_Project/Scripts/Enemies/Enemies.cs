using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Enemies : MonoBehaviour {

    public float displayHeight = 1.5f;
    public ushort id { get; private set; }
    public ulong symbolHash { get; private set; }
    public int symbolUnitGroup;
    private byte framesUntilPosUpdate;
    public bool DoUpdatePos { get { return true/*framesUntilPosUpdate >= 25*/; } }

    public EnnemyNavigator navigator { get; private set; }
    public InterpolationTransform interpolationTransform { get; private set; }
    public EnemyAnimator animator { get; private set; }

    public List<Symbols> keySymbols;

    public void Init (ushort id) {
        this.id = id;

        keySymbols = new List<Symbols>();
        navigator = GetComponent<EnnemyNavigator>();
        animator = GetComponent<EnemyAnimator>();
        interpolationTransform = GetComponent<InterpolationTransform>();
        animator.enemy = this;

        keySymbols.Add(Symbols.VLine);
        keySymbols.Add(Symbols.HLine);
        keySymbols.Add(Symbols.VLine);
        keySymbols.Add(Symbols.BottomHat);
        keySymbols.Add(Symbols.TopHat);

        EnemyManager.RegisterEnemy(this);
        interpolationTransform.CopyState();

        OnSpawn();
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
    

    private void OnDestroy () {
        EnemyManager.UnregisterEnemy(this);
    }

    public void OnUpdate () {
        UpdateVisuals();
    }

    public void OnFixedUpdate () {
        if(framesUntilPosUpdate < 25) {
            framesUntilPosUpdate++;
        }
    }

    public void ResetPosUpdate () {
        framesUntilPosUpdate = 0;
    }



    public void ApplyImpulse (Vector3 impulse) {
        navigator.ApplyImpulse(impulse);
    }

    public bool TryDamage (Symbols symbol) {
        if(keySymbols.Count == 0)
            return false;

        if(symbol == keySymbols[0]) {
            keySymbols.RemoveAt(0);
            if(keySymbols.Count == 0) {
                OnDeath();
                return true;
            }
        } else {
            return false;
        }

        OnDamaged();
        return true;
    }

    public virtual void OnDamaged () {
        animator.Flash(0.2f);
    }

    public virtual void OnDeath () {
        navigator.isFreezed = true;
        animator.PlayDeathAnimation();
    }

    public void OnDeathAnimationDone () {
        EnemyManager.PlayParticle(transform.position);
        Destroy(gameObject);
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
