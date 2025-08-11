using System.Collections.Generic;
using UnityEngine;

public enum StatusType { Fire, Ice, Lightning }
public enum DamageTag { Physical, Fire, Ice, Lightning }

public struct StatusEffect
{
    public StatusType type;
    public float magnitude;
    public float duration;
    public float tickInterval;
    public int stacks;
}

public interface IStatusReceiver
{
    void ApplyStatus(StatusEffect e);
    bool HasStatus(StatusType t);
    void RemoveStatus(StatusType t);
}

/// <summary>
/// Controls application and ticking of status effects.
/// </summary>
public class StatusController : MonoBehaviour, IStatusReceiver
{
    private class ActiveStatus
    {
        public StatusEffect effect;
        public float timer;
        public float tickTimer;
    }

    [SerializeField] private float miniChainRange = 2f;
    [SerializeField] private float miniChainDamage = 1f;
    [SerializeField] private int freezeThreshold = 5;

    private readonly Dictionary<StatusType, ActiveStatus> statuses = new();

    public void ApplyStatus(StatusEffect e)
    {
        if (statuses.TryGetValue(e.type, out var active))
        {
            switch (e.type)
            {
                case StatusType.Fire:
                    active.effect.magnitude += e.magnitude;
                    active.effect.duration = Mathf.Max(active.effect.duration, e.duration);
                    active.effect.tickInterval = e.tickInterval;
                    break;
                case StatusType.Ice:
                    if (e.magnitude > active.effect.magnitude)
                        active.effect.magnitude = e.magnitude;
                    active.effect.stacks += e.stacks;
                    active.effect.duration = Mathf.Max(active.effect.duration, e.duration);
                    break;
                case StatusType.Lightning:
                    active.effect.stacks += e.stacks;
                    active.effect.magnitude = Mathf.Max(active.effect.magnitude, e.magnitude);
                    active.effect.duration = Mathf.Max(active.effect.duration, e.duration);
                    active.effect.tickInterval = e.tickInterval;
                    break;
            }
            active.timer = 0f;
        }
        else
        {
            statuses[e.type] = new ActiveStatus { effect = e, timer = 0f, tickTimer = 0f };
        }
    }

    public bool HasStatus(StatusType t) => statuses.ContainsKey(t);

    public void RemoveStatus(StatusType t) => statuses.Remove(t);

    public float GetSpeedMultiplier()
    {
        float mult = 1f;
        if (statuses.TryGetValue(StatusType.Ice, out var ice))
        {
            mult *= Mathf.Clamp01(1f - ice.effect.magnitude);
            if (ice.effect.stacks >= freezeThreshold)
                mult = 0f;
        }
        return mult;
    }

    public float GetDamageTakenMultiplier(DamageTag tag)
    {
        float mult = 1f;
        if (tag == DamageTag.Lightning && statuses.TryGetValue(StatusType.Lightning, out var l))
        {
            mult *= 1f + l.effect.magnitude;
        }
        return mult;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        var remove = new List<StatusType>();
        foreach (var kvp in statuses)
        {
            var st = kvp.Value;
            st.timer += dt;
            if (st.timer >= st.effect.duration)
            {
                remove.Add(kvp.Key);
                continue;
            }
            if (st.effect.tickInterval > 0f)
            {
                st.tickTimer += dt;
                if (st.tickTimer >= st.effect.tickInterval)
                {
                    st.tickTimer = 0f;
                    switch (kvp.Key)
                    {
                        case StatusType.Fire:
                            var enemy = GetComponent<EnemyBase>();
                            if (enemy != null)
                            {
                                float dmg = st.effect.magnitude;
                                enemy.TakeDamage(dmg);
                            }
                            break;
                        case StatusType.Lightning:
                            ChainLightning(st.effect);
                            break;
                    }
                }
            }
            kvp.Value.timer = st.timer;
            kvp.Value.tickTimer = st.tickTimer;
        }
        foreach (var t in remove)
            statuses.Remove(t);
    }

    private void ChainLightning(StatusEffect e)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, miniChainRange, LayerMask.GetMask("Enemy"));
        foreach (var c in cols)
        {
            if (c.gameObject == gameObject) continue;
            var enemy = c.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(miniChainDamage);
                break;
            }
        }
    }
}
