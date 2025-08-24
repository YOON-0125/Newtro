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
        Debug.Log($"[ElectricField] 💥 펄스 발동! 데미지: {damage}, 취약성배율: {vulnerabilityMultiplier}");
        Tick();
    }

    protected override void ApplyTick(EnemyBase enemy)
    {
        float finalDamage = damage * vulnerabilityMultiplier;
        Debug.Log($"[ElectricField] ⚡ 전기장 데미지! 대상: {enemy.name}, 기본: {damage:F1}, 취약성: {vulnerabilityMultiplier:F2}, 최종: {finalDamage:F1}");
        ApplyDamage(enemy, finalDamage);
    }
}
