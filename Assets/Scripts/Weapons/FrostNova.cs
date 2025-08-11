using UnityEngine;

/// <summary>
/// 플레이어 중심으로 얼음 폭발을 일으켜 적을 느리게 함
/// </summary>
public class FrostNova : WeaponBase
{
    [Header("Frost Nova Settings")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private bool useMultiplier = true;
    [SerializeField] private float slowMultiplier = 0.7f;
    [SerializeField] private float slowFlat = 0.3f;
    [SerializeField] private float ticksPerSec = 0f;
    [SerializeField] private float fieldDuration = 2f;
    [SerializeField] private GameObject fieldPrefab;
    [Header("Status Effect")]
    [SerializeField] private float statusDuration = 2f;
    [SerializeField] private float statusTickInterval = 1f;
    [SerializeField] private int statusStacks = 1;

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        damage = 5f;
        if (fieldPrefab == null)
        {
            fieldPrefab = new GameObject("FrostNovaField");
            fieldPrefab.AddComponent<FieldBase>();
            fieldPrefab.SetActive(false);
        }
    }

    protected override void ExecuteAttack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));
        var effect = new StatusEffect
        {
            type = StatusType.Ice,
            magnitude = useMultiplier ? slowMultiplier : slowFlat,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                var sc = enemy.GetComponent<StatusController>();
                float finalDamage = damage;
                if (sc != null)
                {
                    finalDamage *= sc.GetDamageTakenMultiplier(DamageTag.Ice);
                    sc.ApplyStatus(effect);
                }
                enemy.TakeDamage(finalDamage);
            }
        }

        if (ticksPerSec > 0f)
        {
            GameObject fieldObj = SimpleObjectPool.Instance != null ?
                SimpleObjectPool.Instance.Get(fieldPrefab, transform.position, Quaternion.identity) :
                Instantiate(fieldPrefab, transform.position, Quaternion.identity);
            var field = fieldObj.GetComponent<FieldBase>();
            if (field == null) field = fieldObj.AddComponent<FieldBase>();
            field.Setup(radius, fieldDuration, 1f / ticksPerSec, damage);
            field.ConfigureEffect(DamageTag.Ice, effect);
        }

        OnAttackComplete();
    }

    public override string GetWeaponInfo()
    {
        return $"{weaponName} Lv.{level}\nDamage: {damage:F1}\nRadius: {radius:F1}\nSlow: {(useMultiplier ? slowMultiplier : slowFlat)}";
    }

    public void DebugFire() => TryAttack();

    protected override float GetAttackRange() => radius;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
