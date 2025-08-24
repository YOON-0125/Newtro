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
        baseDamage = 5f;
        cooldown = 1f;
        damageTag = DamageTag.Fire; // 화염 데미지 설정
        if (projectilePrefab == null)
        {
            projectilePrefab = new GameObject("FireballProjectile");
            
            // SpriteRenderer 추가
            var sr = projectilePrefab.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            
            projectilePrefab.SetActive(false);
        }
        
        // FireballEffectPrefab에 필요한 컴포넌트 확인하고 추가
        if (projectilePrefab != null)
        {
            // 파이어볼 크기 조정
            projectilePrefab.transform.localScale = Vector3.one * 0.5f; // 50% 크기로 축소
            
            if (projectilePrefab.GetComponent<Collider2D>() == null)
            {
                var collider = projectilePrefab.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.3f; // 작아진 크기에 맞춰 콜라이더도 축소
                Debug.Log("[Fireball] FireballEffectPrefab에 Collider2D 추가됨");
            }
            
            if (projectilePrefab.GetComponent<PooledProjectile>() == null)
            {
                projectilePrefab.AddComponent<PooledProjectile>();
                Debug.Log("[Fireball] FireballEffectPrefab에 PooledProjectile 추가됨");
            }
        }
    }

    protected override void ExecuteAttack()
    {
        Debug.Log($"[Fireball] 🔥 ExecuteAttack 호출! Level: {Level}, Damage: {Damage}, Cooldown: {cooldown}");
        
        Transform target = FindNearestTargetFromPlayer();
        if (target == null)
        {
            Debug.Log($"[Fireball] ❌ 타겟이 없어 공격 취소");
            OnAttackComplete();
            return;
        }
        
        // 플레이어 위치에서 발사
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 firePosition = player != null ? player.transform.position : transform.position;
        
        Debug.Log($"[Fireball] 🎯 타겟 발견: {target.name}, 발사 위치: {firePosition}");

        Vector2 dir = (target.position - firePosition).normalized;
        
        // 진행 방향으로 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        GameObject proj = SimpleObjectPool.Instance != null ?
            SimpleObjectPool.Instance.Get(projectilePrefab, firePosition, rotation) :
            Instantiate(projectilePrefab, firePosition, rotation);
        
        var p = proj.GetComponent<PooledProjectile>();
        if (p == null)
        {
            Debug.LogError("[Fireball] PooledProjectile 컴포넌트가 없습니다! 프리팹 설정을 확인하세요.");
            Destroy(proj);
            OnAttackComplete();
            return;
        }
        var effect = new StatusEffect
        {
            type = StatusType.Fire,
            magnitude = statusMagnitude,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        p.Initialize(Damage, projectileSpeed, projectileLifetime, dir, DamageTag.Fire, effect, 1, (enemy) =>
        {
            if (enemy != null)
                SpawnSplit(proj.transform.position, effect);
        });

        OnAttackComplete();
    }

    /// <summary>
    /// 플레이어 위치에서 가장 가까운 적 찾기 (ChainLightning과 동일한 방식)
    /// </summary>
    private Transform FindNearestTargetFromPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Fireball] 플레이어를 찾을 수 없습니다!");
            return null;
        }
        
        Vector3 playerPosition = player.transform.position;
        float attackRange = GetAttackRange();
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerPosition, attackRange, LayerMask.GetMask("Enemy"));
        
        Debug.Log($"[Fireball] 플레이어 위치 ({playerPosition})에서 범위 {attackRange} 내 적 탐지 결과: {enemies.Length}마리");
        
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
        
        Debug.Log($"[Fireball] 가장 가까운 적: {nearestEnemy?.name}, 거리: {nearestDistance:F2}");
        return nearestEnemy;
    }

    private void SpawnSplit(Vector3 pos, StatusEffect effect)
    {
        float angleStep = 360f / splitCount;
        float spawnDistance = 1.5f; // 충돌 위치에서 1.5유닛 떨어진 곳에서 생성
        
        for (int i = 0; i < splitCount; i++)
        {
            float angle = angleStep * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            
            // 분열 위치를 적에서 조금 떨어뜨림
            Vector3 spawnPos = pos + (Vector3)(dir * spawnDistance);
            
            // 분열된 투사체도 진행 방향으로 회전
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            GameObject proj = SimpleObjectPool.Instance != null ?
                SimpleObjectPool.Instance.Get(projectilePrefab, spawnPos, rotation) :
                Instantiate(projectilePrefab, spawnPos, rotation);
            var p = proj.GetComponent<PooledProjectile>();
            if (p == null) p = proj.AddComponent<PooledProjectile>();
            p.Initialize(Damage * 0.7f, projectileSpeed, projectileLifetime, dir, DamageTag.Fire, effect, 1, null);
        }
        
        Debug.Log($"[Fireball] 💥 {pos}에서 {splitCount}개로 분열 생성");
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
