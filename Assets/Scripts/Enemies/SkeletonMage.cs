using System.Collections;
using UnityEngine;

/// <summary>
/// 스켈레톤 마법사 - 원거리 공격형 적
/// </summary>
public class SkeletonMage : EnemyBase
{
    [Header("마법사 설정")]
    [SerializeField] private GameObject magicProjectilePrefab;
    [SerializeField] private Transform castPoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 20f;
    [SerializeField] private float castTime = 1f;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spreadAngle = 0f;
    
    [Header("행동 패턴")]
    [SerializeField] private float preferredDistance = 6f;
    [SerializeField] private float tooCloseDistance = 3f;
    [SerializeField] private float retreatSpeed = 3f;
    [SerializeField] private int maxAttacksBeforeRetreat = 3;
    
    [Header("마법 이펙트")]
    [SerializeField] private GameObject castingEffect;
    [SerializeField] private GameObject muzzleFlash;
    
    private int attackCount;
    private bool isCasting;
    private bool isRetreating;
    private Vector2 retreatDirection;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        // 스켈레톤 마법사 초기 스탯 설정
        enemyName = "Skeleton Mage";
        maxHealth = 35f;
        currentHealth = maxHealth;
        moveSpeed = 1.5f; // 느림
        damage = projectileDamage;
        attackRange = 8f; // 원거리
        attackCooldown = 2f;
        detectionRange = 10f;
        expValue = 25;
        
        // 캐스팅 포인트 설정
        if (castPoint == null)
        {
            GameObject castPointObj = new GameObject("CastPoint");
            castPointObj.transform.SetParent(transform);
            castPointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            castPoint = castPointObj.transform;
        }
    }
    
    protected override void UpdateBehavior()
    {
        if (isDead || isCasting) return;
        
        if (target == null)
        {
            FindTarget();
            return;
        }
        
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // 타겟을 잃었는지 확인
        if (distanceToTarget > loseTargetRange)
        {
            LoseTarget();
            return;
        }
        
        // 상태별 행동
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToTarget <= detectionRange)
                {
                    SetState(EnemyState.Chasing);
                }
                break;
                
            case EnemyState.Chasing:
                HandleCombatMovement(distanceToTarget);
                
                if (distanceToTarget <= attackRange && CanAttack())
                {
                    SetState(EnemyState.Attacking);
                }
                break;
                
            case EnemyState.Attacking:
                if (!isAttacking && CanAttack())
                {
                    AttackTarget();
                }
                break;
        }
    }
    
    /// <summary>
    /// 전투 이동 처리
    /// </summary>
    private void HandleCombatMovement(float distanceToTarget)
    {
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        Vector2 moveDirection = Vector2.zero;
        
        if (distanceToTarget < tooCloseDistance)
        {
            // 너무 가까우면 후퇴
            moveDirection = -directionToTarget;
            isRetreating = true;
            retreatDirection = moveDirection;
        }
        else if (distanceToTarget > preferredDistance)
        {
            // 선호 거리보다 멀면 접근
            moveDirection = directionToTarget * 0.7f; // 천천히 접근
            isRetreating = false;
        }
        else if (isRetreating || attackCount >= maxAttacksBeforeRetreat)
        {
            // 계속 후퇴하거나 공격 횟수 초과 시
            moveDirection = retreatDirection;
            
            // 충분히 후퇴했으면 멈춤
            if (distanceToTarget >= preferredDistance)
            {
                isRetreating = false;
                attackCount = 0;
            }
        }
        
        if (moveDirection.magnitude > 0)
        {
            Move(moveDirection * (isRetreating ? retreatSpeed : 1f));
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    protected override void ExecuteAttack()
    {
        if (target == null || isCasting) return;
        
        StartCoroutine(CastSpellCoroutine());
    }
    
    /// <summary>
    /// 마법 시전 코루틴
    /// </summary>
    private IEnumerator CastSpellCoroutine()
    {
        isCasting = true;
        rb.linearVelocity = Vector2.zero; // 시전 중 이동 금지
        
        // 시전 이펙트 시작
        GameObject castEffect = null;
        if (castingEffect != null)
        {
            castEffect = Instantiate(castingEffect, transform);
        }
        
        // 시전 애니메이션
        if (animator != null)
        {
            animator.SetBool("IsCasting", true);
        }
        
        // 시전 시간 대기
        yield return new WaitForSeconds(castTime);
        
        // 타겟이 여전히 유효한지 확인
        if (target != null && !isDead)
        {
            FireProjectiles();
            attackCount++;
        }
        
        // 시전 이펙트 정리
        if (castEffect != null)
        {
            Destroy(castEffect);
        }
        
        if (animator != null)
        {
            animator.SetBool("IsCasting", false);
        }
        
        isCasting = false;
        OnAttackComplete();
    }
    
    /// <summary>
    /// 발사체 생성
    /// </summary>
    private void FireProjectiles()
    {
        if (magicProjectilePrefab == null || target == null)
        {
            Debug.LogWarning("마법 발사체 프리팹이나 타겟이 없습니다!");
            return;
        }
        
        Vector3 directionToTarget = (target.position - castPoint.position).normalized;
        
        // 머즐 플래시 이펙트
        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(muzzleFlash, castPoint.position, Quaternion.identity);
            Destroy(flash, 0.5f);
        }
        
        // 여러 발사체 생성
        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 projectileDirection = directionToTarget;
            
            // 다중 발사체 시 각도 분산
            if (projectileCount > 1)
            {
                float angleOffset = (i - (projectileCount - 1) * 0.5f) * spreadAngle;
                projectileDirection = Quaternion.Euler(0, 0, angleOffset) * projectileDirection;
            }
            
            CreateMagicProjectile(castPoint.position, projectileDirection);
        }
    }
    
    /// <summary>
    /// 마법 발사체 생성
    /// </summary>
    private void CreateMagicProjectile(Vector3 position, Vector3 direction)
    {
        GameObject projectile = Instantiate(magicProjectilePrefab, position, Quaternion.identity);
        
        // 발사체 설정
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
            projectileRb = projectile.AddComponent<Rigidbody2D>();
            
        projectileRb.gravityScale = 0;
        projectileRb.linearVelocity = direction * projectileSpeed;
        
        // 마법 발사체 컴포넌트 설정
        MagicProjectile magicComp = projectile.GetComponent<MagicProjectile>();
        if (magicComp == null)
            magicComp = projectile.AddComponent<MagicProjectile>();
            
        magicComp.Initialize(projectileDamage, 5f); // 5초 수명
        
        // 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    protected override void OnStateChanged(EnemyState newState)
    {
        base.OnStateChanged(newState);
        
        switch (newState)
        {
            case EnemyState.Attacking:
                rb.linearVelocity = Vector2.zero;
                break;
                
            case EnemyState.Hurt:
                // 시전 중단
                if (isCasting)
                {
                    StopAllCoroutines();
                    isCasting = false;
                    if (animator != null)
                        animator.SetBool("IsCasting", false);
                }
                break;
        }
    }
    
    protected override void OnHurt()
    {
        base.OnHurt();
        
        // 피격 시 후퇴 행동 시작
        if (target != null)
        {
            retreatDirection = (transform.position - target.position).normalized;
            isRetreating = true;
        }
    }
    
    protected override void DropItems()
    {
        base.DropItems();
        
        // 마법사 특별 드롭 (마법 구슬)
        if (Random.value <= 0.15f) // 15% 확률
        {
            Debug.Log("스켈레톤 마법사가 마법 구슬을 떨어뜨렸습니다!");
        }
    }
    
    public override string GetEnemyInfo()
    {
        return base.GetEnemyInfo() + 
               $"\nProjectile Damage: {projectileDamage}" +
               $"\nProjectile Count: {projectileCount}" +
               $"\nCast Time: {castTime}s" +
               $"\nAttack Count: {attackCount}";
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 선호 거리 시각화
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
        
        // 너무 가까운 거리 시각화
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);
        
        // 캐스팅 포인트 표시
        if (castPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(castPoint.position, 0.2f);
        }
    }
}

/// <summary>
/// 마법 발사체 컴포넌트
/// </summary>
public class MagicProjectile : MonoBehaviour
{
    private float damage;
    private float lifetime;
    private float startTime;
    
    [Header("이펙트")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private TrailRenderer trail;
    
    public void Initialize(float damage, float lifetime)
    {
        this.damage = damage;
        this.lifetime = lifetime;
        this.startTime = Time.time;
        
        // 트레일 설정
        if (trail == null)
            trail = GetComponent<TrailRenderer>();
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
        // 플레이어와 충돌 시
        if (other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            CreateHitEffect();
            DestroyProjectile();
        }
        // 벽과 충돌 시
        else if (other.CompareTag("Wall"))
        {
            CreateHitEffect();
            DestroyProjectile();
        }
    }
    
    private void CreateHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
    
    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}