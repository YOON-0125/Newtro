using UnityEngine;

/// <summary>
/// 느리게 이동하며 주변에 전기 피해를 주는 구체
/// </summary>
public class ElectricSphere : WeaponBase
{
    [Header("Electric Sphere Settings")]
    [SerializeField] private GameObject corePrefab;
    [SerializeField] private float coreSpeed = 2f;
    [SerializeField] private float coreRadius = 1f;
    [SerializeField] private float tickPerSec = 2f;
    [SerializeField] private float fieldLinkRadius = 3f;
    [SerializeField] private float coreLifetime = 5f;
    [Header("Status Effect")]
    [SerializeField] private float statusMagnitude = 0f;
    [SerializeField] private float statusDuration = 1f;
    [SerializeField] private float statusTickInterval = 0f;
    [SerializeField] private int statusStacks = 1;

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        damage = 5f;
        if (corePrefab == null)
        {
            corePrefab = new GameObject("ElectricSphereCore");
            corePrefab.AddComponent<ElectricSphereCore>();
            var sr = corePrefab.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            corePrefab.SetActive(false);
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

        Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
        GameObject coreObj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(corePrefab, transform.position, Quaternion.identity) :
            Instantiate(corePrefab, transform.position, Quaternion.identity);
        var core = coreObj.GetComponent<ElectricSphereCore>();
        if (core == null) core = coreObj.AddComponent<ElectricSphereCore>();
        var effect = new StatusEffect
        {
            type = StatusType.Lightning,
            magnitude = statusMagnitude,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        core.Initialize(dir, damage, 1f / tickPerSec, coreRadius, coreSpeed, coreLifetime, fieldLinkRadius, effect);

        OnAttackComplete();
    }

    public override string GetWeaponInfo()
    {
        return base.GetWeaponInfo() +
               $"\nRadius: {coreRadius:F1}" +
               $"\nTick/s: {tickPerSec:F1}";
    }

    public void DebugFire() => TryAttack();

    protected override float GetAttackRange()
    {
        return 10f;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coreRadius);
    }
}
