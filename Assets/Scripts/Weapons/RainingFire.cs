using UnityEngine;

/// <summary>
/// 랜덤 위치에 화염구를 떨어뜨려 화염 지대를 생성
/// </summary>
public class RainingFire : WeaponBase
{
    [Header("Raining Fire Settings")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private GameObject fieldPrefab;
    [SerializeField] private float fallRate = 1f;
    [SerializeField] private float impactDamage = 10f;
    [SerializeField] private float fieldRadius = 2f;
    [SerializeField] private float fieldDamage = 3f;
    [SerializeField] private float fieldDuration = 3f;
    [SerializeField] private float fieldTickPerSec = 1f;
    [SerializeField] private float spawnRange = 6f;
    [Header("Status Effect")]
    [SerializeField] private float statusMagnitude = 0f;
    [SerializeField] private float statusDuration = 1f;
    [SerializeField] private float statusTickInterval = 1f;
    [SerializeField] private int statusStacks = 1;

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        cooldown = 1f / fallRate;
        damage = impactDamage;
        if (fireballPrefab == null)
        {
            fireballPrefab = new GameObject("Fireball");
            fireballPrefab.AddComponent<PooledProjectile>();
            var sr = fireballPrefab.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            fireballPrefab.SetActive(false);
        }
        if (fieldPrefab == null)
        {
            fieldPrefab = new GameObject("FireField");
            fieldPrefab.AddComponent<FieldBase>();
            fieldPrefab.SetActive(false);
        }
    }

    protected override void ExecuteAttack()
    {
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRange;
        Vector2 startPos = spawnPos + Vector2.up * 5f;
        float speed = 10f;
        float lifetime = 5f / speed; // 높이 5 기준

        GameObject proj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(fireballPrefab, startPos, Quaternion.identity) :
            Instantiate(fireballPrefab, startPos, Quaternion.identity);

        var pooled = proj.GetComponent<PooledProjectile>();
        if (pooled == null) pooled = proj.AddComponent<PooledProjectile>();

        var effect = new StatusEffect
        {
            type = StatusType.Fire,
            magnitude = statusMagnitude,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        pooled.Initialize(impactDamage, speed, lifetime, Vector2.down, DamageTag.Fire, effect, 1, (enemy) =>
        {
            SpawnField(proj.transform.position, effect);
        });

        OnAttackComplete();
    }

    private void SpawnField(Vector3 pos, StatusEffect effect)
    {
        GameObject fieldObj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(fieldPrefab, pos, Quaternion.identity) :
            Instantiate(fieldPrefab, pos, Quaternion.identity);
        var field = fieldObj.GetComponent<FieldBase>();
        if (field == null) field = fieldObj.AddComponent<FieldBase>();
        field.Setup(fieldRadius, fieldDuration, 1f / fieldTickPerSec, fieldDamage);
        field.ConfigureEffect(DamageTag.Fire, effect);
    }

    public override void ApplyDamageMultiplier(float m)
    {
        base.ApplyDamageMultiplier(m);
        impactDamage *= m;
        fieldDamage *= m;
    }

    public override void ApplyCooldownMultiplier(float m)
    {
        base.ApplyCooldownMultiplier(m);
        fallRate = 1f / cooldown;
    }

    public override string GetWeaponInfo()
    {
        return $"{weaponName} Lv.{level}\nImpact: {impactDamage:F1}\nField DPS: {fieldDamage * fieldTickPerSec:F1}\nCooldown: {cooldown:F2}s";
    }

    public void DebugFire() => TryAttack();

    protected override float GetAttackRange() => spawnRange;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRange);
    }
}
