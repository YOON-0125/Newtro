using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모든 적의 기본이 되는 추상 클래스
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("기본 스탯")]
    [SerializeField] protected string enemyName = "Enemy";
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackCooldown = 1f;
    
    [Header("행동 설정")]
    [SerializeField] protected float detectionRange = 10f;
    [SerializeField] protected float loseTargetRange = 15f;
    [SerializeField] protected LayerMask playerLayer = 1;
    
    [Header("드롭 아이템")]
    [SerializeField] protected int expValue = 10;
    [SerializeField] protected GameObject expOrbPrefab;    // EXP 오브 프리팹
    [SerializeField] protected float dropChance = 0.1f;
    [SerializeField] protected GameObject[] dropItems;
    
    [Header("사운드")]
    [SerializeField] protected AudioClip attackSound;
    [SerializeField] protected AudioClip hurtSound;
    [SerializeField] protected AudioClip deathSound;
    
    // 이벤트
    [System.Serializable]
    public class EnemyEvents
    {
        public UnityEvent OnSpawn;
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent<float> OnDamage;
        public UnityEvent OnDeath;
        public UnityEvent<Transform> OnTargetAcquired;
        public UnityEvent OnTargetLost;
        public UnityEvent OnAttack;
    }
    
    [Header("이벤트")]
    [SerializeField] public EnemyEvents events;
    
    // 컴포넌트
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected Animator animator;
    protected AudioSource audioSource;
    protected StatusController statusController;
    
    // 상태 관련
    protected Transform target;
    protected float lastAttackTime;
    protected bool isDead;
    protected bool isAttacking;
    
    // 상태 열거형
    public enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Hurt,
        Dead
    }
    
    protected EnemyState currentState = EnemyState.Idle;
    
    // 프로퍼티
    public string EnemyName => enemyName;
    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsAttacking => isAttacking;
    public Transform Target => target;
    public EnemyState CurrentState => currentState;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        statusController = GetComponent<StatusController>();
        
        // 기본 설정
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // StatusController 없으면 자동 추가
        if (statusController == null)
        {
            statusController = gameObject.AddComponent<StatusController>();
            Debug.Log($"[EnemyBase] {enemyName}에 StatusController 자동 추가");
        }
        
        rb.gravityScale = 0f;
        currentHealth = maxHealth;
    }
    
    protected virtual void Start()
    {
        Initialize();
        events?.OnSpawn?.Invoke();
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        UpdateBehavior();
        UpdateAnimation();
    }
    
    /// <summary>
    /// 적 초기화
    /// </summary>
    protected virtual void Initialize()
    {
        // 각 적별 초기화 로직
    }
    
    /// <summary>
    /// 행동 업데이트
    /// </summary>
    protected virtual void UpdateBehavior()
    {
        // 타겟 찾기
        if (target == null)
        {
            FindTarget();
        }
        else
        {
            // 타겟과의 거리 체크
            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            
            if (distanceToTarget > loseTargetRange)
            {
                LoseTarget();
                return;
            }
            
            // 상태에 따른 행동
            switch (currentState)
            {
                case EnemyState.Idle:
                    if (distanceToTarget <= detectionRange)
                    {
                        SetState(EnemyState.Chasing);
                    }
                    break;
                    
                case EnemyState.Chasing:
                    if (distanceToTarget <= attackRange && CanAttack())
                    {
                        SetState(EnemyState.Attacking);
                    }
                    else
                    {
                        ChaseTarget();
                    }
                    break;
                    
                case EnemyState.Attacking:
                    if (!isAttacking)
                    {
                        if (distanceToTarget <= attackRange && CanAttack())
                        {
                            AttackTarget();
                        }
                        else
                        {
                            SetState(EnemyState.Chasing);
                        }
                    }
                    break;
            }
        }
    }
    
    /// <summary>
    /// 애니메이션 업데이트
    /// </summary>
    protected virtual void UpdateAnimation()
    {
        if (animator == null) return;
        
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDead", isDead);
        animator.SetInteger("State", (int)currentState);
    }
    
    /// <summary>
    /// 타겟 찾기
    /// </summary>
    protected virtual void FindTarget()
    {
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);
        
        if (players.Length > 0)
        {
            target = players[0].transform;
            SetState(EnemyState.Chasing);
            events?.OnTargetAcquired?.Invoke(target);
        }
    }
    
    /// <summary>
    /// 타겟 추적
    /// </summary>
    protected virtual void ChaseTarget()
    {
        if (target == null) return;
        
        Vector2 direction = (target.position - transform.position).normalized;
        Move(direction);
    }
    
    /// <summary>
    /// 이동 처리
    /// </summary>
    protected virtual void Move(Vector2 direction)
    {
        float speedMul = statusController != null ? statusController.GetSpeedMultiplier() : 1f;
        Vector2 targetVelocity = direction * moveSpeed * speedMul;
        rb.linearVelocity = targetVelocity;
        
        // 이동 방향으로 회전 (2D에서는 스프라이트 플립으로 처리 가능)
        if (direction.x != 0)
        {
            transform.localScale = new Vector3(direction.x > 0 ? 1 : -1, 1, 1);
        }
    }
    
    /// <summary>
    /// 공격 가능 여부
    /// </summary>
    protected virtual bool CanAttack()
    {
        return !isAttacking && Time.time >= lastAttackTime + attackCooldown;
    }
    
    /// <summary>
    /// 타겟 공격
    /// </summary>
    protected virtual void AttackTarget()
    {
        if (!CanAttack()) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        
        ExecuteAttack();
        PlaySound(attackSound);
        events?.OnAttack?.Invoke();
    }
    
    /// <summary>
    /// 실제 공격 로직 (각 적별로 구현)
    /// </summary>
    protected abstract void ExecuteAttack();
    
    /// <summary>
    /// 공격 완료 처리
    /// </summary>
    protected virtual void OnAttackComplete()
    {
        isAttacking = false;
        SetState(EnemyState.Chasing);
    }
    
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public virtual void TakeDamage(float damageAmount, DamageTag tag = DamageTag.Physical)
    {
        if (isDead) return;

        if (statusController != null)
        {
            damageAmount *= statusController.GetDamageTakenMultiplier(tag);
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        events?.OnHealthChanged?.Invoke(currentHealth);
        events?.OnDamage?.Invoke(damageAmount);
        
        PlaySound(hurtSound);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            OnHurt();
        }
    }
    
    /// <summary>
    /// 피해를 받았을 때 처리
    /// </summary>
    protected virtual void OnHurt()
    {
        // 짧은 피격 상태
        SetState(EnemyState.Hurt);
        Invoke(nameof(RecoverFromHurt), 0.2f);
    }
    
    /// <summary>
    /// 피격 상태 회복
    /// </summary>
    protected virtual void RecoverFromHurt()
    {
        if (!isDead)
            SetState(EnemyState.Chasing);
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    protected virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        SetState(EnemyState.Dead);
        
        rb.linearVelocity = Vector2.zero;
        col.enabled = false;
        
        PlaySound(deathSound);
        events?.OnDeath?.Invoke();
        
        DropItems();
        
        // 사망 애니메이션 후 파괴
        Invoke(nameof(DestroyEnemy), 2f);
    }
    
    /// <summary>
    /// 아이템 드롭
    /// </summary>
    protected virtual void DropItems()
    {
        // 경험치는 항상 드롭
        DropExperience();
        
        // 기타 아이템 드롭
        if (dropItems != null && dropItems.Length > 0 && Random.value <= dropChance)
        {
            GameObject dropItem = dropItems[Random.Range(0, dropItems.Length)];
            if (dropItem != null)
            {
                Instantiate(dropItem, transform.position, Quaternion.identity);
            }
        }
    }
    
    /// <summary>
    /// 경험치 드롭
    /// </summary>
    protected virtual void DropExperience()
    {
        // ExpOrbManager를 통해 생성 (추천)
        ExpOrbManager expOrbManager = ExpOrbManager.Instance;
        if (expOrbManager != null)
        {
            expOrbManager.CreateExpOrb(transform.position, expValue);
        }
        // 개별 프리팹이 설정되어 있으면 그것을 우선 사용
        else if (expOrbPrefab != null)
        {
            GameObject expOrb = Instantiate(expOrbPrefab, transform.position, Quaternion.identity);
            ExpOrb expOrbScript = expOrb.GetComponent<ExpOrb>();
            
            if (expOrbScript != null)
            {
                expOrbScript.SetExpValue(expValue);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: ExpOrb 프리팹에 ExpOrb 스크립트가 없습니다!");
            }
        }
        else
        {
            // 마지막 백업: 동적 생성
            GameObject expOrb = CreateExpOrb();
            if (expOrb != null)
            {
                expOrb.GetComponent<ExpOrb>()?.SetExpValue(expValue);
            }
        }
    }
    
    /// <summary>
    /// 런타임에서 EXP 오브 생성 (프리팹 없는 경우)
    /// </summary>
    private GameObject CreateExpOrb()
    {
        GameObject expOrb = new GameObject("ExpOrb");
        expOrb.transform.position = transform.position;
        
        // ExpOrb 스크립트 추가
        ExpOrb expOrbScript = expOrb.AddComponent<ExpOrb>();
        
        return expOrb;
    }
    
    /// <summary>
    /// 적 파괴
    /// </summary>
    protected virtual void DestroyEnemy()
    {
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    protected virtual void SetState(EnemyState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        OnStateChanged(newState);
    }
    
    /// <summary>
    /// 상태 변경 시 처리
    /// </summary>
    protected virtual void OnStateChanged(EnemyState newState)
    {
        // 각 적별로 오버라이드하여 사용
    }
    
    /// <summary>
    /// 타겟 잃기
    /// </summary>
    protected virtual void LoseTarget()
    {
        target = null;
        SetState(EnemyState.Idle);
        events?.OnTargetLost?.Invoke();
        rb.linearVelocity = Vector2.zero;
    }
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    protected virtual void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 적 정보 반환
    /// </summary>
    public virtual string GetEnemyInfo()
    {
        return $"{enemyName}\nHP: {currentHealth:F0}/{maxHealth:F0}\nState: {currentState}";
    }
    
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 타겟 잃는 범위
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
    
    // 충돌 처리
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        Debug.Log($"{gameObject.name}: Collision detected with {other.name}, Tag: {other.tag}");
        
        // 플레이어와 충돌 시 데미지
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name}: Attacking player! State: {currentState}");
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"{gameObject.name}: Dealing {damage} damage to player");
                playerHealth.TakeDamage(damage, DamageTag.Physical);
            }
            else
            {
                Debug.LogError($"{gameObject.name}: PlayerHealth component not found on {other.name}!");
            }
        }
    }
}