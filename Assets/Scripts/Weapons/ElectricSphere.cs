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
    
    [Header("Range Indicator")]
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private Color indicatorColor = new Color(1f, 1f, 0f, 0.25f); // 노란색

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        baseDamage = 5f;
        if (corePrefab == null)
        {
            corePrefab = new GameObject("ElectricSphereCore");
            corePrefab.AddComponent<ElectricSphereCore>();
            var sr = corePrefab.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            corePrefab.SetActive(false);
        }
        
    }
    

    protected override void ExecuteAttack()
    {
        Transform target = FindNearestTargetFromPlayer();
        if (target == null)
        {
            OnAttackComplete();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPosition = player != null ? player.transform.position : transform.position;
        
        
        Vector2 dir = ((Vector2)(target.position - playerPosition)).normalized;
        GameObject coreObj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(corePrefab, playerPosition, Quaternion.identity) :
            Instantiate(corePrefab, playerPosition, Quaternion.identity);
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
        core.Initialize(dir, Damage, 1f / tickPerSec, coreRadius, coreSpeed, coreLifetime, fieldLinkRadius, effect);
        
        Debug.Log($"[ElectricSphere] 🔥 구체 발사! 데미지: {Damage}, 틱간격: {1f / tickPerSec:F2}초, 반지름: {coreRadius}, 연결범위: {fieldLinkRadius}");
        
        // 발사체에 범위 인디케이터 설정 (1초만 표시)
        if (showRangeIndicator)
        {
            var indicator = coreObj.GetComponent<CircleIndicator>();
            if (indicator == null) indicator = coreObj.AddComponent<CircleIndicator>();
            indicator.ShowIndicator(fieldLinkRadius, indicatorColor, 1f); // 1초만 표시
            Debug.Log($"[ElectricSphere] 발사체에 범위 인디케이터 설정: 반지름={fieldLinkRadius}, 표시시간=1초");
        }

        OnAttackComplete();
    }

    /// <summary>
    /// 플레이어 위치에서 가장 가까운 적 찾기
    /// </summary>
    private Transform FindNearestTargetFromPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[ElectricSphere] 플레이어를 찾을 수 없습니다!");
            return null;
        }
        
        Vector3 playerPosition = player.transform.position;
        float attackRange = GetAttackRange();
        
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerPosition, attackRange, LayerMask.GetMask("Enemy"));
        
        if (enemies.Length == 0)
            return null;
            
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector2.Distance(playerPosition, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }
        
        return nearestEnemy;
    }

    public override bool LevelUp()
    {
        if (!base.LevelUp()) return false;
        
        // ElectricSphere 레벨업 효과
        AddFlatDamageBonus(3f); // 데미지 증가
        coreRadius += 0.2f; // 구체 반경 증가
        tickPerSec += 0.5f; // 틱 속도 증가
        fieldLinkRadius += 0.5f; // 전기장 범위 증가
        
        Debug.Log($"[ElectricSphere] 레벨업! 다음 발사체부터 새로운 범위 적용: {fieldLinkRadius}");
        
        Debug.Log($"[ElectricSphere] 레벨업! 레벨: {Level}, 데미지: {Damage}, 반경: {coreRadius}");
        return true;
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
