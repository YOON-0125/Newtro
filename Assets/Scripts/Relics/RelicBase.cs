using UnityEngine;

public abstract class RelicBase : ScriptableObject
{
    [Header("Meta")]
    public string relicId;
    public string displayName;
    [TextArea] public string description;
    public RelicCategory category;
    public Sprite icon;

    [Header("Stacking")]
    public bool stackable = true;
    public int maxStacks = 999;
    [Min(0)] public int initialStacks = 1;

    public virtual void OnAcquire(RelicContext ctx) {}
    public virtual void OnRemove(RelicContext ctx) {}
    public virtual void OnBeforeWeaponFired(RelicContext ctx, WeaponBase weapon) {}
    public virtual void ModifyWeaponStats(RelicContext ctx, WeaponBase weapon, ref float damage, ref float cooldown, ref float range) {}
    public virtual void OnProjectileSpawned(RelicContext ctx, PooledProjectile proj) {}
    public virtual void OnDamageDealt(RelicContext ctx, EnemyBase enemy, ref float finalDamage, ElementTag element) {}
    public virtual void ModifyStatusApplication(RelicContext ctx, ref StatusEffect effect) {}
}
