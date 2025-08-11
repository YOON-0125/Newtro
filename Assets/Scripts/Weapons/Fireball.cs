using UnityEngine;

/// <summary>
/// 적중 시 분열하는 파이어볼
/// </summary>
public class Fireball : WeaponBase
{
    [Header("Fireball Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private int splitCount = 3;
    [Header("Status Effect")]
    [SerializeField] private float statusMagnitude = 0f;
    [SerializeField] private float statusDuration = 1f;
    [SerializeField] private float statusTickInterval = 1f;
    [SerializeField] private int statusStacks = 1;

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        damage = 5f;
        cooldown = 1f;
        damageTag = DamageTag.Fire; // 화염 데미지 설정
        if (projectilePrefab == null)
        {
            projectilePrefab = new GameObject("FireballProjectile");
            projectilePrefab.AddComponent<PooledProjectile>();
            var sr = projectilePrefab.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            projectilePrefab.SetActive(false);
        }
    }

    protected override void ExecuteAttack()
    {
        Transform target = FindNearestTarget();
        if (target == null)
        {
            OnAttackComplete();
            return;
        }

        Vector2 dir = (target.position - transform.position).normalized;
        GameObject proj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(projectilePrefab, transform.position, Quaternion.identity) :
            Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var p = proj.GetComponent<PooledProjectile>();
        if (p == null) p = proj.AddComponent<PooledProjectile>();
        var effect = new StatusEffect
        {
            type = StatusType.Fire,
            magnitude = statusMagnitude,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        p.Initialize(damage, projectileSpeed, projectileLifetime, dir, DamageTag.Fire, effect, 1, (enemy) =>
        {
            if (enemy != null)
                SpawnSplit(proj.transform.position, effect);
        });

        OnAttackComplete();
    }

    private void SpawnSplit(Vector3 pos, StatusEffect effect)
    {
        float angleStep = 360f / splitCount;
        for (int i = 0; i < splitCount; i++)
        {
            float angle = angleStep * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            GameObject proj = SimpleObjectPool.Instance != null ?
                SimpleObjectPool.Instance.Get(projectilePrefab, pos, Quaternion.identity) :
                Instantiate(projectilePrefab, pos, Quaternion.identity);
            var p = proj.GetComponent<PooledProjectile>();
            if (p == null) p = proj.AddComponent<PooledProjectile>();
            p.Initialize(damage, projectileSpeed, projectileLifetime, dir, DamageTag.Fire, effect, 1, null);
        }
    }

    public override string GetWeaponInfo()
    {
        return base.GetWeaponInfo() + $"\nSplit: {splitCount}";
    }

    public void DebugFire() => TryAttack();

    protected override float GetAttackRange() => 12f;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}
