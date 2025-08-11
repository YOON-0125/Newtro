using UnityEngine;

/// <summary>
/// 발사체 무기 (파이어볼 등)
/// </summary>
public class ProjectileWeapon : WeaponBase
{
    [Header("발사체 설정")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spreadAngle = 0f;
    
    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        
        // 발사 포인트가 없다면 자동 생성
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = Vector3.zero;
            firePoint = firePointObj.transform;
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
        
        // 여러 발사체 생성
        for (int i = 0; i < projectileCount; i++)
        {
            CreateProjectile(target, i);
        }
        
        OnAttackComplete();
    }
    
    /// <summary>
    /// 발사체 생성
    /// </summary>
    private void CreateProjectile(Transform target, int index)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{weaponName}: projectilePrefab이 설정되지 않았습니다!");
            return;
        }
        
        Vector3 spawnPosition = firePoint.position;
        Vector3 direction = (target.position - spawnPosition).normalized;
        
        // 다중 발사체일 경우 각도 분산
        if (projectileCount > 1)
        {
            float angleOffset = (index - (projectileCount - 1) * 0.5f) * spreadAngle;
            direction = Quaternion.Euler(0, 0, angleOffset) * direction;
        }
        
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        
        // 발사체 설정
        SetupProjectile(projectile, direction);
    }
    
    /// <summary>
    /// 발사체 설정
    /// </summary>
    private void SetupProjectile(GameObject projectile, Vector3 direction)
    {
        // Rigidbody2D 설정
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = projectile.AddComponent<Rigidbody2D>();
            
        rb.gravityScale = 0;
        rb.linearVelocity = direction * projectileSpeed;

        // 발사체 컴포넌트 설정
        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        if (projectileComponent == null)
            projectileComponent = projectile.AddComponent<Projectile>();
            
        projectileComponent.Initialize(damage, projectileLifetime, damageTag, statusEffect);
        // 회전 설정 (발사 방향으로)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

        var relicManager = FindObjectOfType<RelicManager>();
        if (relicManager != null)
            relicManager.OnProjectileSpawned(projectileComponent);
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // 레벨에 따른 개선
        switch (level)
        {
            case 2:
                projectileCount = 2;
                spreadAngle = 15f;
                break;
            case 3:
                projectileSpeed *= 1.2f;
                break;
            case 4:
                projectileCount = 3;
                spreadAngle = 25f;
                break;
            case 5:
                cooldown *= 0.8f;
                break;
            case 6:
                projectileLifetime *= 1.5f;
                break;
            case 7:
                projectileCount = 4;
                spreadAngle = 35f;
                break;
            case 8:
                projectileSpeed *= 1.3f;
                break;
            case 9:
                cooldown *= 0.7f;
                break;
            case 10:
                projectileCount = 5;
                spreadAngle = 45f;
                damage *= 1.5f;
                break;
        }
    }
    
    protected override float GetAttackRange()
    {
        return 12f; // 발사체 무기는 좀 더 긴 범위
    }
    
    public override string GetWeaponInfo()
    {
        return base.GetWeaponInfo() + 
               $"\nProjectiles: {projectileCount}" +
               $"\nSpeed: {projectileSpeed:F1}" +
               $"\nLifetime: {projectileLifetime:F1}s";
    }
}

/// <summary>
/// 발사체 컴포넌트
/// </summary>
public class Projectile : PooledProjectile
{
    private float damage;
    private float lifetime;
    private float startTime;
    private DamageTag damageTag;
    private StatusEffect statusEffect;
    
    public void Initialize(float damage, float lifetime, DamageTag tag, StatusEffect effect)
    {
        this.damage = damage;
        this.lifetime = lifetime;
        this.damageTag = tag;
        this.statusEffect = effect;
        this.startTime = Time.time;
    }
    
    private void Update()
    {
        // 수명 체크
        if (Time.time >= startTime + lifetime)
        {
            DestroyProjectile();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적과 충돌 시
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                float finalDamage = damage;
                var relicManager = FindObjectOfType<RelicManager>();
                if (relicManager != null)
                {
                    relicManager.OnDamageDealt(enemy, ref finalDamage, ElementTag.Physical);
                }
                enemy.TakeDamage(finalDamage, damageTag);
                var status = enemy.GetComponent<IStatusReceiver>();
                status?.ApplyStatus(statusEffect);
            }

            Pierce--;
            if (Pierce <= 0)
                DestroyProjectile();
        }
        // 벽과 충돌 시
        else if (other.CompareTag("Wall"))
        {
            DestroyProjectile();
        }
    }
    
    private void DestroyProjectile()
    {
        // 파괴 이펙트나 사운드 추가 가능
        Destroy(gameObject);
    }
}