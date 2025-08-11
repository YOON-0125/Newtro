using UnityEngine;

/// <summary>
/// 전기 속성 필드
/// </summary>
public class ElectricField : FieldBase
{
    [SerializeField] private float vulnerabilityMultiplier = 1f;

    public void SetVulnerability(float m)
    {
        vulnerabilityMultiplier = m;
    }

    public void Pulse(float damage, float tickInterval)
    {
        this.damage = damage;
        this.tickInterval = tickInterval;
        Tick();
    }

    protected override void ApplyTick(EnemyBase enemy)
    {
        ApplyDamage(enemy, damage * vulnerabilityMultiplier);
    }
}
