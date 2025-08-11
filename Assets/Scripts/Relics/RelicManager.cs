using System.Collections.Generic;
using UnityEngine;

public class RelicManager : MonoBehaviour
{
    [SerializeField] private List<RelicBase> startingRelics = new();
    private readonly Dictionary<string, int> stacks = new();
    private readonly List<RelicBase> actives = new();
    public RelicContext Ctx { get; private set; }

    private void Awake()
    {
        Ctx = new RelicContext
        {
            player = FindObjectOfType<PlayerHealth>(),
            weaponManager = FindObjectOfType<WeaponManager>(),
            upgradeSystem = FindObjectOfType<UpgradeSystem>(),
            relicManager = this
        };

        foreach (var r in startingRelics)
        {
            Acquire(r, r.initialStacks);
        }
    }

    public int GetStacks(string relicId) => stacks.TryGetValue(relicId, out var v) ? v : 0;

    public void Acquire(RelicBase relic, int addStacks = 1)
    {
        if (relic == null) return;
        var cur = GetStacks(relic.relicId);
        var next = relic.stackable ? Mathf.Min(cur + addStacks, relic.maxStacks) : (cur > 0 ? cur : 1);
        if (next == cur) return;

        if (cur == 0)
        {
            actives.Add(relic);
            relic.OnAcquire(Ctx);
        }
        stacks[relic.relicId] = next;
        BroadcastRefresh();
        Save();
    }

    public void Remove(RelicBase relic)
    {
        if (relic == null) return;
        if (!stacks.ContainsKey(relic.relicId)) return;
        stacks.Remove(relic.relicId);
        actives.Remove(relic);
        relic.OnRemove(Ctx);
        BroadcastRefresh();
        Save();
    }

    public void OnBeforeWeaponFired(WeaponBase w)
    {
        foreach (var r in actives)
            r.OnBeforeWeaponFired(Ctx, w);
    }

    public void ModifyWeaponStats(WeaponBase w, ref float dmg, ref float cd, ref float range)
    {
        foreach (var r in actives)
            r.ModifyWeaponStats(Ctx, w, ref dmg, ref cd, ref range);
    }

    public void OnProjectileSpawned(PooledProjectile p)
    {
        foreach (var r in actives)
            r.OnProjectileSpawned(Ctx, p);
    }

    public void OnDamageDealt(EnemyBase e, ref float fd, ElementTag elem)
    {
        foreach (var r in actives)
            r.OnDamageDealt(Ctx, e, ref fd, elem);
    }

    public void ModifyStatusApplication(ref StatusEffect eff)
    {
        foreach (var r in actives)
            r.ModifyStatusApplication(Ctx, ref eff);
    }

    private void BroadcastRefresh()
    {
        if (Ctx.weaponManager == null) return;
        foreach (var w in Ctx.weaponManager.AllWeapons)
        {
            float d = w.Damage;
            float c = w.Cooldown;
            float r = w.Range;
            ModifyWeaponStats(w, ref d, ref c, ref r);
            if (d != w.Damage)
                w.ApplyDamageMultiplier(Mathf.Max(0.0001f, d / Mathf.Max(0.0001f, w.Damage)));
            if (c != w.Cooldown)
                w.ApplyCooldownMultiplier(Mathf.Max(0.0001f, c / Mathf.Max(0.0001f, w.Cooldown)));
            w.Range = r;
        }
    }

    private void Save()
    {
        // TODO: Implement persistence (PlayerPrefs/Json)
    }

    private void Load()
    {
        // TODO: Load saved relics and reapply via Acquire
    }
}
