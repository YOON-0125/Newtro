using System.Collections.Generic;
using UnityEngine;

public enum StatusType { Fire, Ice, Lightning }
public enum DamageTag { Physical, Fire, Ice, Lightning }

[System.Serializable]
public struct StatusEffect
{
    public StatusType type;
    public float magnitude;
    public float duration;
    public float tickInterval;
    public int stacks;
}

[System.Serializable]
public struct StatusEffectInfo
{
    public StatusType type;
    public float remainingDuration;
    public int stacks;
    public float magnitude;
}

public interface IStatusReceiver
{
    void ApplyStatus(StatusEffect e);
    bool HasStatus(StatusType t);
    void RemoveStatus(StatusType t);
}

public class StatusController : MonoBehaviour, IStatusReceiver
{
    [Header("General")] public bool isBoss = false;

    [Header("Fire")]
    public float baseDurationFire = 4f;
    public float tickIntervalFire = 0.5f;
    public float baseMagnitudePerTickFire = 1f;
    public float bossDotPenalty = 0.5f;

    [Header("Ice")]
    public float baseDurationIce = 3f;
    [Range(0f, 0.9f)] public float slowPercent = 0.5f;
    public int freezeThreshold = 5;
    public float freezeDuration = 1f;
    [Range(0f, 0.9f)] public float bossSlowCap = 0.3f;

    [Header("Lightning")]
    public float baseDurationLightning = 5f;
    public float ampPerStack = 0.1f;
    public float ampMax = 0.5f;
    public float tickIntervalLightning = 1f;
    public float miniChainDamage = 2f;
    public float miniChainRange = 3f;

    private class StatusState
    {
        public float magnitude;
        public float duration;
        public float tickInterval;
        public int stacks;
        public float tickTimer;
        public float hiddenStacks; // used for freeze progress
    }

    private readonly Dictionary<StatusType, StatusState> active = new();
    private bool isFrozen;
    private float freezeTimer;

    private void Update()
    {
        float dt = Time.deltaTime;
        List<StatusType> toRemove = new();
        foreach (var pair in active)
        {
            StatusType type = pair.Key;
            StatusState state = pair.Value;
            state.duration -= dt;
            state.tickTimer -= dt;

            if (state.duration <= 0f)
            {
                toRemove.Add(type);
                continue;
            }

            if (state.tickTimer <= 0f)
            {
                switch (type)
                {
                    case StatusType.Fire:
                        float dmg = state.magnitude;
                        if (isBoss) dmg *= bossDotPenalty;
                        ApplyDirectDamage(dmg, DamageTag.Fire);
                        state.tickTimer += state.tickInterval;
                        break;
                    case StatusType.Lightning:
                        DoMiniChain();
                        state.tickTimer += state.tickInterval;
                        break;
                }
            }
        }

        if (isFrozen)
        {
            freezeTimer -= dt;
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
            }
        }

        foreach (var t in toRemove)
            active.Remove(t);
    }

    private void ApplyDirectDamage(float amount, DamageTag tag)
    {
        var enemy = GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(amount, tag);
            return;
        }
        var player = GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(amount, tag);
        }
    }

    private void DoMiniChain()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, miniChainRange, LayerMask.GetMask("Enemy"));
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;
            var enemy = h.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(miniChainDamage, DamageTag.Lightning);
                break;
            }
        }
    }

    public void ApplyStatus(StatusEffect e)
    {
        switch (e.type)
        {
            case StatusType.Fire:
                ApplyFire(e);
                break;
            case StatusType.Ice:
                ApplyIce(e);
                break;
            case StatusType.Lightning:
                ApplyLightning(e);
                break;
        }
    }

    private void ApplyFire(StatusEffect e)
    {
        float magnitude = e.magnitude > 0 ? e.magnitude : baseMagnitudePerTickFire;
        float duration = e.duration > 0 ? e.duration : baseDurationFire;
        float interval = e.tickInterval > 0 ? e.tickInterval : tickIntervalFire;

        if (active.TryGetValue(StatusType.Fire, out var state))
        {
            state.magnitude += magnitude;
            state.duration = duration;
            state.tickInterval = interval;
            state.tickTimer = 0f;
        }
        else
        {
            active[StatusType.Fire] = new StatusState
            {
                magnitude = magnitude,
                duration = duration,
                tickInterval = interval,
                tickTimer = 0f
            };
        }
    }

    private void ApplyIce(StatusEffect e)
    {
        float magnitude = e.magnitude > 0 ? e.magnitude : slowPercent;
        float duration = e.duration > 0 ? e.duration : baseDurationIce;
        magnitude = Mathf.Clamp01(magnitude);
        if (isBoss) magnitude = Mathf.Min(magnitude, bossSlowCap);

        if (active.TryGetValue(StatusType.Ice, out var state))
        {
            state.magnitude = Mathf.Max(state.magnitude, magnitude);
            state.duration = duration;
        }
        else
        {
            state = new StatusState { magnitude = magnitude, duration = duration };
            active[StatusType.Ice] = state;
        }

        state.hiddenStacks += e.stacks > 0 ? e.stacks : 1;
        if (!isBoss && state.hiddenStacks >= freezeThreshold)
        {
            isFrozen = true;
            freezeTimer = freezeDuration;
            state.hiddenStacks = 0f;
        }
    }

    private void ApplyLightning(StatusEffect e)
    {
        float duration = e.duration > 0 ? e.duration : baseDurationLightning;
        float interval = e.tickInterval > 0 ? e.tickInterval : tickIntervalLightning;
        int addStacks = e.stacks > 0 ? e.stacks : 1;

        if (active.TryGetValue(StatusType.Lightning, out var state))
        {
            state.stacks += addStacks;
            state.duration = duration;
            state.tickInterval = interval;
        }
        else
        {
            active[StatusType.Lightning] = new StatusState
            {
                stacks = addStacks,
                duration = duration,
                tickInterval = interval,
                tickTimer = 0f
            };
        }
    }

    public bool HasStatus(StatusType t)
    {
        if (t == StatusType.Ice && isFrozen) return true;
        return active.ContainsKey(t);
    }

    public void RemoveStatus(StatusType t)
    {
        active.Remove(t);
        if (t == StatusType.Ice)
        {
            isFrozen = false;
        }
    }

    public float GetSpeedMultiplier()
    {
        if (isFrozen) return 0f;
        if (active.TryGetValue(StatusType.Ice, out var state))
        {
            return 1f - Mathf.Clamp01(state.magnitude);
        }
        return 1f;
    }

    public float GetDamageTakenMultiplier(DamageTag tag)
    {
        if (tag == DamageTag.Lightning && active.TryGetValue(StatusType.Lightning, out var state))
        {
            float amp = ampPerStack * state.stacks;
            amp = Mathf.Min(amp, ampMax);
            float multiplier = 1f + amp;
            Debug.Log($"[StatusController] {gameObject.name} - 번개 데미지 증폭: x{multiplier:F2} (스택: {state.stacks})");
            return multiplier;
        }
        return 1f;
    }
    
    /// <summary>
    /// BossUI용 - 현재 활성화된 모든 상태효과 목록 반환
    /// </summary>
    public Dictionary<StatusType, StatusEffectInfo> GetActiveStatusEffects()
    {
        var result = new Dictionary<StatusType, StatusEffectInfo>();
        
        foreach (var pair in active)
        {
            StatusType type = pair.Key;
            StatusState state = pair.Value;
            
            result[type] = new StatusEffectInfo
            {
                type = type,
                remainingDuration = state.duration,
                stacks = state.stacks,
                magnitude = state.magnitude
            };
        }
        
        // Freeze 상태 추가
        if (isFrozen)
        {
            result[StatusType.Ice] = new StatusEffectInfo
            {
                type = StatusType.Ice,
                remainingDuration = freezeTimer,
                stacks = 1,
                magnitude = 1f // 완전 정지
            };
        }
        
        return result;
    }
    
    /// <summary>
    /// 특정 상태효과의 남은 지속시간 반환
    /// </summary>
    public float GetStatusDuration(StatusType statusType)
    {
        if (statusType == StatusType.Ice && isFrozen)
        {
            return freezeTimer;
        }
        
        if (active.TryGetValue(statusType, out var state))
        {
            return state.duration;
        }
        
        return 0f;
    }
}

