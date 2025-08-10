using System.Collections;
using UnityEngine;

/// <summary>
/// 듀얼 블레이드 스켈레톤 - 고속 추적형 적
/// </summary>
public class DualBladeSkeleton : EnemyBase
{
    [Header("듀얼 블레이드 설정")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashRange = 8f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float comboAttackDamage = 25f;
    [SerializeField] private int comboHits = 3;
    [SerializeField] private float comboDuration = 1.5f;
    
    [Header("특수 능력")]
    [SerializeField] private float criticalChance = 0.2f; // 20% 크리티컬
    [SerializeField] private float criticalMultiplier = 2f;
    [SerializeField] private bool canDodge = true;
    [SerializeField] private float dodgeChance = 0.15f; // 15% 회피
    
    [Header("이펙트")]
    [SerializeField] private GameObject dashEffect;
    [SerializeField] private GameObject bladeTrail;
    [SerializeField] private GameObject criticalHitEffect;
    
    private bool isDashing;
    private bool isComboAttacking;
    private float lastDashTime;
    private Vector2 dashDirection;
    private int currentComboHit;
    private TrailRenderer[] bladeTrails;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        // 듀얼 블레이드 스켈레톤 초기 스탯 설정
        enemyName = "Dual Blade Skeleton";
        maxHealth = 80f;
        currentHealth = maxHealth;
        moveSpeed = 4f; // 빠른 이동
        damage = comboAttackDamage;
        attackRange = 2f;
        attackCooldown = 2.5f;
        detectionRange = 12f; // 넓은 감지 범위
        expValue = 40;
        
        // 블레이드 트레일 설정
        SetupBladeTrails();
    }
    
    /// <summary>
    /// 블레이드 트레일 설정
    /// </summary>
    private void SetupBladeTrails()
    {
        if (bladeTrail != null)
        {
            // 좌우 블레이드용 트레일 생성
            GameObject leftBlade = Instantiate(bladeTrail, transform);
            GameObject rightBlade = Instantiate(bladeTrail, transform);
            
            leftBlade.transform.localPosition = new Vector3(-0.3f, 0, 0);
            rightBlade.transform.localPosition = new Vector3(0.3f, 0, 0);
            
            bladeTrails = new TrailRenderer[]
            {
                leftBlade.GetComponent<TrailRenderer>(),
                rightBlade.GetComponent<TrailRenderer>()
            };
            
            // 초기에는 트레일 비활성화
            foreach (var trail in bladeTrails)
            {
                if (trail != null)
                    trail.enabled = false;
            }
        }
    }
    
    protected override void UpdateBehavior()
    {
        if (isDead || isDashing || isComboAttacking) return;
        
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
                // 대시 공격 가능한지 확인
                if (CanDash() && distanceToTarget <= dashRange && distanceToTarget > attackRange)
                {
                    StartDashAttack();
                }
                else if (distanceToTarget <= attackRange && CanAttack())
                {
                    SetState(EnemyState.Attacking);
                }
                else
                {
                    ChaseTarget();
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
    /// 대시 가능 여부
    /// </summary>
    private bool CanDash()
    {
        return !isDashing && Time.time >= lastDashTime + dashCooldown;
    }
    
    /// <summary>
    /// 대시 공격 시작
    /// </summary>
    private void StartDashAttack()
    {
        if (target == null) return;
        
        dashDirection = (target.position - transform.position).normalized;
        StartCoroutine(DashAttackCoroutine());
    }
    
    /// <summary>
    /// 대시 공격 코루틴
    /// </summary>
    private IEnumerator DashAttackCoroutine()
    {
        isDashing = true;
        lastDashTime = Time.time;
        
        // 대시 이펙트 생성
        GameObject dashEffectObj = null;
        if (dashEffect != null)
        {
            dashEffectObj = Instantiate(dashEffect, transform);
        }
        
        // 블레이드 트레일 활성화
        EnableBladeTrails(true);
        
        // 대시 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }
        
        float dashStartTime = Time.time;
        Vector2 startPosition = transform.position;
        
        // 대시 이동
        while (Time.time < dashStartTime + dashDuration)
        {
            float dashProgress = (Time.time - dashStartTime) / dashDuration;
            rb.linearVelocity = dashDirection * dashSpeed * (1f - dashProgress * 0.3f); // 점진적 감속
            
            yield return null;
        }
        
        rb.linearVelocity = Vector2.zero;
        
        // 대시 완료 후 콤보 공격 시작
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange * 1.5f)
        {
            StartCoroutine(ComboAttackCoroutine());
        }
        
        // 이펙트 정리
        if (dashEffectObj != null)
        {
            Destroy(dashEffectObj);
        }
        
        EnableBladeTrails(false);
        isDashing = false;
    }
    
    protected override void ExecuteAttack()
    {
        StartCoroutine(ComboAttackCoroutine());
    }
    
    /// <summary>
    /// 콤보 공격 코루틴
    /// </summary>
    private IEnumerator ComboAttackCoroutine()
    {
        isComboAttacking = true;
        currentComboHit = 0;
        
        EnableBladeTrails(true);
        
        float comboStartTime = Time.time;
        float timeBetweenHits = comboDuration / comboHits;
        
        while (currentComboHit < comboHits && Time.time < comboStartTime + comboDuration)
        {
            // 콤보 히트 실행
            PerformComboHit();
            currentComboHit++;
            
            // 다음 히트까지 대기
            if (currentComboHit < comboHits)
            {
                yield return new WaitForSeconds(timeBetweenHits);
            }
        }
        
        EnableBladeTrails(false);
        isComboAttacking = false;
        OnAttackComplete();
    }
    
    /// <summary>
    /// 콤보 히트 실행
    /// </summary>
    private void PerformComboHit()
    {
        if (target == null) return;
        
        // 공격 애니메이션
        if (animator != null)
        {
            animator.SetTrigger($"ComboHit{currentComboHit + 1}");
        }
        
        // 타겟이 범위 내에 있는지 확인
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            // 크리티컬 체크
            bool isCritical = Random.value <= criticalChance;
            float finalDamage = comboAttackDamage * (isCritical ? criticalMultiplier : 1f);
            
            // 플레이어에게 데미지
            var playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(finalDamage);
                
                // 크리티컬 이펙트
                if (isCritical && criticalHitEffect != null)
                {
                    GameObject critEffect = Instantiate(criticalHitEffect, target.position, Quaternion.identity);
                    Destroy(critEffect, 1f);
                }
            }
            
            events?.OnAttack?.Invoke();
        }
    }
    
    /// <summary>
    /// 블레이드 트레일 활성화/비활성화
    /// </summary>
    private void EnableBladeTrails(bool enable)
    {
        if (bladeTrails != null)
        {
            foreach (var trail in bladeTrails)
            {
                if (trail != null)
                    trail.enabled = enable;
            }
        }
    }
    
    protected override void OnStateChanged(EnemyState newState)
    {
        base.OnStateChanged(newState);
        
        switch (newState)
        {
            case EnemyState.Chasing:
                // 추적 시 속도 증가
                if (!isDashing)
                    moveSpeed = 4f;
                break;
                
            case EnemyState.Attacking:
                rb.linearVelocity = Vector2.zero;
                break;
                
            case EnemyState.Hurt:
                // 회피 체크
                if (canDodge && Random.value <= dodgeChance)
                {
                    PerformDodge();
                }
                break;
                
            case EnemyState.Dead:
                EnableBladeTrails(false);
                break;
        }
    }
    
    /// <summary>
    /// 회피 동작
    /// </summary>
    private void PerformDodge()
    {
        Vector2 dodgeDirection = Random.insideUnitCircle.normalized;
        rb.AddForce(dodgeDirection * 5f, ForceMode2D.Impulse);
        
        if (animator != null)
        {
            animator.SetTrigger("Dodge");
        }
    }
    
    public override void TakeDamage(float damageAmount)
    {
        // 회피 체크
        if (canDodge && Random.value <= dodgeChance)
        {
            // 회피 성공
            if (animator != null)
            {
                animator.SetTrigger("Dodge");
            }
            return;
        }
        
        // 일반 데미지 처리
        base.TakeDamage(damageAmount);
    }
    
    protected override void DropItems()
    {
        base.DropItems();
        
        // 듀얼 블레이드 특별 드롭 (블레이드 파편)
        if (Random.value <= 0.25f) // 25% 확률
        {
            Debug.Log("듀얼 블레이드 스켈레톤이 블레이드 파편을 떨어뜨렸습니다!");
        }
    }
    
    public override string GetEnemyInfo()
    {
        return base.GetEnemyInfo() + 
               $"\nDash Speed: {dashSpeed}" +
               $"\nCombo Hits: {comboHits}" +
               $"\nCritical Chance: {criticalChance * 100:F0}%" +
               $"\nDodge Chance: {dodgeChance * 100:F0}%";
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 대시 범위 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dashRange);
        
        // 대시 방향 표시 (실행 중일 때)
        if (isDashing && Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, dashDirection * 3f);
        }
    }
}