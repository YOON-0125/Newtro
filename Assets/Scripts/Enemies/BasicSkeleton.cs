using UnityEngine;

/// <summary>
/// 기본 스켈레톤 - 단순 추적형 적
/// </summary>
public class BasicSkeleton : EnemyBase
{
    [Header("기본 스켈레톤 설정")]
    [SerializeField] private float contactDamage = 1f;
    [SerializeField] private float contactCooldown = 1f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderInterval = 2f;
    
    private Vector2 wanderTarget;
    private float lastWanderTime;
    private float lastContactDamageTime;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        // 기본 스켈레톤 초기 스탯 설정 (Inspector 설정 사용)
        enemyName = "Basic Skeleton";
        // maxHealth = 50f; // Inspector 설정 사용
        // currentHealth = maxHealth; // 자동 설정됨
        // moveSpeed = 2.5f; // Inspector 설정 사용
        // damage = contactDamage; // Inspector 설정 사용
        // attackRange = 1f; // Inspector 설정 사용
        // attackCooldown = contactCooldown; // Inspector 설정 사용
        // detectionRange = 8f; // Inspector 설정 사용
        // expValue = 10; // Inspector 설정 사용
        
        // 랜덤 배회 시작점 설정
        SetRandomWanderTarget();
    }
    
    protected override void UpdateBehavior()
    {
        if (isDead) return;
        
        // Hurt 상태가 아닐 때만 행동
        if (currentState == EnemyState.Hurt) return;
        
        // 추적 상태라면 무조건 기본 행동 수행 (타겟이 있든 없든)
        if (currentState == EnemyState.Chasing)
        {
            // 타겟이 없다면 다시 찾기 시도
            if (target == null)
            {
                FindTarget();
            }
            
            // 기본 추적 로직 수행 (타겟 추적 및 이동)
            if (target != null)
            {
                ChaseTarget();
            }
        }
        else if (target != null)
        {
            // 타겟이 있으면 기본 행동 수행
            base.UpdateBehavior();
        }
        else
        {
            // 타겟이 없으면 배회
            Wander();
            
            // 주기적으로 타겟 찾기
            FindTarget();
        }
    }
    
    /// <summary>
    /// 배회 행동
    /// </summary>
    private void Wander()
    {
        // 일정 시간마다 새로운 배회 목표 설정
        if (Time.time >= lastWanderTime + wanderInterval)
        {
            SetRandomWanderTarget();
            lastWanderTime = Time.time;
        }
        
        // 배회 목표로 이동
        Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
        float distanceToWander = Vector2.Distance(transform.position, wanderTarget);
        
        if (distanceToWander > 0.5f)
        {
            Move(direction * 0.5f); // 배회는 절반 속도로
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 랜덤 배회 목표 설정
    /// </summary>
    private void SetRandomWanderTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        wanderTarget = (Vector2)transform.position + randomDirection * wanderRadius;
    }
    
    protected override void ExecuteAttack()
    {
        // 기본 스켈레톤은 접촉 공격만 수행
        // 실제 데미지는 OnTriggerEnter2D에서 처리
        
        // 공격 애니메이션 실행
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // 짧은 대기 후 공격 완료
        Invoke(nameof(OnAttackComplete), 0.5f);
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        // 플레이어와 접촉 시 데미지
        if (other.CompareTag("Player"))
        {
            // 접촉 데미지 쿨다운 체크
            if (Time.time >= lastContactDamageTime + contactCooldown)
            {
                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage, DamageTag.Physical);
                    lastContactDamageTime = Time.time;

                    // 접촉 데미지 이벤트
                    events?.OnAttack?.Invoke();
                }
            }
        }
    }
    
    protected override void OnStateChanged(EnemyState newState)
    {
        base.OnStateChanged(newState);
        
        switch (newState)
        {
            case EnemyState.Idle:
                SetRandomWanderTarget();
                break;
                
            case EnemyState.Chasing:
                // 추적 시 속도 증가
                rb.linearDamping = 0f;
                break;
                
            case EnemyState.Hurt:
                // 피격 시 잠시 정지
                rb.linearVelocity = Vector2.zero;
                break;
                
            case EnemyState.Dead:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }
    
    
    protected override void DropItems()
    {
        base.DropItems();
        
        // 기본 스켈레톤 특별 드롭 (낮은 확률로 뼈 아이템)
        if (Random.value <= 0.05f) // 5% 확률
        {
            // 뼈 아이템 드롭 로직 (아이템이 있다면)
            Debug.Log("기본 스켈레톤이 뼈를 떨어뜨렸습니다!");
        }
    }
    
    public override string GetEnemyInfo()
    {
        return base.GetEnemyInfo() + 
               $"\nContact Damage: {contactDamage}" +
               $"\nWander Radius: {wanderRadius}";
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 배회 범위 시각화
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        
        // 현재 배회 목표 표시
        if (Application.isPlaying && target == null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(wanderTarget, 0.3f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }
    }
}