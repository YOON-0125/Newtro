using UnityEngine;

/// <summary>
/// 번개 낙뢰와 전기 지대를 생성
/// </summary>
public class Thunder : WeaponBase
{
    [Header("Thunder Settings")]
    [SerializeField] private GameObject lightningPrefab;
    [SerializeField] private GameObject fieldPrefab;
    [SerializeField] private float strikeDamage = 5f;
    [SerializeField] private float fieldRadius = 3f;
    [SerializeField] private float fieldTickPerSec = 0.5f;
    [SerializeField] private float fieldDuration = 4f;
    [SerializeField] private float vulnMultiplier = 1.1f;
    [Header("Status Effect")]
    [SerializeField] private float statusMagnitude = 0f;
    [SerializeField] private float statusDuration = 1f;
    [SerializeField] private float statusTickInterval = 1f;
    [SerializeField] private int statusStacks = 1;

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        baseDamage = strikeDamage;
        if (fieldPrefab == null)
        {
            fieldPrefab = new GameObject("ThunderField");
            fieldPrefab.AddComponent<ElectricField>();
            fieldPrefab.SetActive(false);
        }
    }

    protected override void ExecuteAttack()
    {
        Transform target = FindNearestTarget();
        Vector3 pos = target != null ? target.position : transform.position;
        var effect = new StatusEffect
        {
            type = StatusType.Lightning,
            magnitude = statusMagnitude,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        if (target != null)
        {
            var enemy = target.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                var sc = enemy.GetComponent<StatusController>();
                float finalDamage = strikeDamage;
                if (sc != null)
                {
                    finalDamage *= sc.GetDamageTakenMultiplier(DamageTag.Lightning);
                    sc.ApplyStatus(effect);
                }
                enemy.TakeDamage(finalDamage);
            }
        }

        SpawnEffect(pos);

        GameObject fieldObj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(fieldPrefab, pos, Quaternion.identity) :
            Instantiate(fieldPrefab, pos, Quaternion.identity);
        var field = fieldObj.GetComponent<ElectricField>();
        if (field == null) field = fieldObj.AddComponent<ElectricField>();
        field.Setup(fieldRadius, fieldDuration, 1f / fieldTickPerSec, strikeDamage);
        field.ConfigureEffect(DamageTag.Lightning, effect);
        field.SetVulnerability(vulnMultiplier);

        OnAttackComplete();
    }

    private void SpawnEffect(Vector3 pos)
    {
        if (lightningPrefab != null)
        {
            GameObject fx = SimpleObjectPool.Instance != null ?
                SimpleObjectPool.Instance.Get(lightningPrefab, pos, Quaternion.identity) :
                Instantiate(lightningPrefab, pos, Quaternion.identity);
            if (SimpleObjectPool.Instance != null)
                SimpleObjectPool.Instance.Release(fx, 0.3f);
            else
                Destroy(fx, 0.3f);
        }
        else
        {
            GameObject line = new GameObject("Lightning");
            var lr = line.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, pos);
            lr.startWidth = lr.endWidth = 0.1f;
            if (SimpleObjectPool.Instance != null)
                SimpleObjectPool.Instance.Release(line, 0.2f);
            else
                Destroy(line, 0.2f);
        }
    }

    [System.Obsolete("Use AddPercentDamageBonus instead")]
    public override void ApplyDamageMultiplier(float m)
    {
        base.ApplyDamageMultiplier(m);
        strikeDamage *= m;
    }

    public override string GetWeaponInfo()
    {
        return $"{weaponName} Lv.{level}\nStrike: {strikeDamage:F1}\nField DPS: {strikeDamage * fieldTickPerSec * vulnMultiplier:F1}";
    }

    public void DebugFire() => TryAttack();

    protected override float GetAttackRange() => 10f;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fieldRadius);
    }
}
