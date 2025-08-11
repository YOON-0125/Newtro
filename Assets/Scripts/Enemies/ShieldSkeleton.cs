using System.Collections;
using UnityEngine;

/// <summary>
/// 방패 스켈레톤 - 방어/무적 패턴을 가진 적
/// </summary>
public class ShieldSkeleton : EnemyBase
{
    [Header("방패 설정")]
    [SerializeField] private GameObject shieldObject;
    [SerializeField] private float shieldHealth = 50f;
    [SerializeField] private float maxShieldHealth = 50f;
    [SerializeField] private float shieldRegenRate = 5f; // 초당 회복량
    [SerializeField] private float shieldRegenDelay = 3f; // 피해 후 회복 시작 지연
    
    [Header("방어 패턴")]
    [SerializeField] private float blockChance = 0.6f; // 60% 블록 확률
    [SerializeField] private float counterAttackChance = 0.3f; // 30% 반격 확률
    [SerializeField] private float counterDamage = 30f;
    [SerializeField] private bool canReflectProjectiles = true;
    
    [Header("무적 패턴")]
    [SerializeField] private float invulnerabilityDuration = 2f;
    [SerializeField] private float invulnerabilityCooldown = 8f;
    [SerializeField] private float healthThresholdForInvul = 0.3f; // 30% 체력 이하에서 발동
    
    [Header("돌진 공격")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDamage = 40f;
    [SerializeField] private float chargeRange = 6f;
    [SerializeField] private float chargeCooldown = 5f;
    
    [Header("이펙트")]
    [SerializeField] private GameObject blockEffect;
    [SerializeField] private GameObject invulnerabilityEffect;
    [SerializeField] private GameObject chargeEffect;
    
    // 상태 변수
    private bool isShieldActive = true;
    private bool isInvulnerable = false;
    private bool isCharging = false;
    private float lastShieldDamageTime;
    private float lastInvulnerabilityTime;
    private float lastChargeTime;
    private Material originalMaterial;
    private SpriteRenderer spriteRenderer;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        // 방패 스켈레톤 초기 스탯 설정
        enemyName = "Shield Skeleton";
        maxHealth = 120f;
        currentHealth = maxHealth;
        moveSpeed = 1.8f; // 느린 이동 (방패 때문에)
        damage = 20f;
        attackRange = 1.8f;
        attackCooldown = 2f;
        detectionRange = 8f;
        expValue = 50;
        
        // 컴포넌트 설정
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // 방패 오브젝트 설정
        if (shieldObject != null)
        {
            shieldObject.SetActive(isShieldActive);
        }
        
        shieldHealth = maxShieldHealth;
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isDead)
        {
            UpdateShieldRegen();
            UpdateInvulnerability();
            CheckInvulnerabilityTrigger();
        }
    }
    
    /// <summary>
    /// 방패 재생 업데이트
    /// </summary>
    private void UpdateShieldRegen()
    {
        if (shieldHealth < maxShieldHealth && Time.time >= lastShieldDamageTime + shieldRegenDelay)
        {
            shieldHealth += shieldRegenRate * Time.deltaTime;
            shieldHealth = Mathf.Min(shieldHealth, maxShieldHealth);
            
            // 방패 재활성화
            if (shieldHealth > 0 && !isShieldActive)
            {
                ActivateShield(true);
            }
        }
    }
    
    /// <summary>
    /// 무적 상태 업데이트
    /// </summary>
    private void UpdateInvulnerability()
    {
        // 무적 상태는 코루틴에서 관리
    }
    
    /// <summary>
    /// 무적 발동 조건 체크
    /// </summary>
    private void CheckInvulnerabilityTrigger()
    {
        if (!isInvulnerable && HealthPercentage <= healthThresholdForInvul && 
            Time.time >= lastInvulnerabilityTime + invulnerabilityCooldown)
        {
            StartCoroutine(ActivateInvulnerabilityCoroutine());
        }
    }
    
    protected override void UpdateBehavior()
    {
        if (isDead || isInvulnerable || isCharging) return;
        
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
        
        // 돌진 공격 가능한지 확인
        if (CanCharge() && distanceToTarget <= chargeRange && distanceToTarget > attackRange)
        {
            StartChargeAttack();
            return;
        }
        
        // 기본 행동
        base.UpdateBehavior();
    }
    
    /// <summary>
    /// 돌진 가능 여부
    /// </summary>
    private bool CanCharge()
    {
        return !isCharging && Time.time >= lastChargeTime + chargeCooldown;
    }
    
    /// <summary>
    /// 돌진 공격 시작
    /// </summary>
    private void StartChargeAttack()
    {
        if (target == null) return;
        
        StartCoroutine(ChargeAttackCoroutine());
    }
    
    /// <summary>
    /// 돌진 공격 코루틴
    /// </summary>
    private IEnumerator ChargeAttackCoroutine()
    {
        isCharging = true;
        lastChargeTime = Time.time;
        
        Vector2 chargeDirection = (target.position - transform.position).normalized;
        
        // 돌진 이펙트
        GameObject chargeEffectObj = null;
        if (chargeEffect != null)
        {
            chargeEffectObj = Instantiate(chargeEffect, transform);
        }
        
        // 돌진 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Charge");
        }
        
        // 돌진 이동
        float chargeDuration = 1f;
        float chargeStartTime = Time.time;
        
        while (Time.time < chargeStartTime + chargeDuration)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;
            yield return null;
        }
        
        rb.linearVelocity = Vector2.zero;
        
        // 이펙트 정리
        if (chargeEffectObj != null)
        {
            Destroy(chargeEffectObj);
        }
        
        isCharging = false;
    }
    
    protected override void ExecuteAttack()
    {
        // 기본 방패 공격
        if (animator != null)
        {
            animator.SetTrigger("ShieldBash");
        }
        
        // 근처 플레이어에게 데미지
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange)
        {
            var playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, DamageTag.Physical);
            }
        }
        
        Invoke(nameof(OnAttackComplete), 0.8f);
    }
    
    public override void TakeDamage(float damageAmount, DamageTag tag = DamageTag.Physical)
    {
        if (isDead || isInvulnerable) return;
        
        // 방패가 활성화된 경우 블록 체크
        if (isShieldActive && Random.value <= blockChance)
        {
            HandleBlock(damageAmount, tag);
            return;
        }
        
        // 일반 데미지 처리
        base.TakeDamage(damageAmount, tag);
    }
    
    /// <summary>
    /// 블록 처리
    /// </summary>
    private void HandleBlock(float damageAmount, DamageTag tag)
    {
        // 방패 데미지
        shieldHealth -= damageAmount * 0.5f; // 방패는 데미지 50%만 받음
        lastShieldDamageTime = Time.time;
        
        // 블록 이펙트
        if (blockEffect != null)
        {
            GameObject effect = Instantiate(blockEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 방패 파괴 체크
        if (shieldHealth <= 0)
        {
            ActivateShield(false);
            
            // 나머지 데미지는 본체가 받음
            float remainingDamage = Mathf.Abs(shieldHealth);
            base.TakeDamage(remainingDamage, tag);
        }
        
        // 반격 체크
        if (Random.value <= counterAttackChance)
        {
            PerformCounterAttack();
        }
    }
    
    /// <summary>
    /// 반격 실행
    /// </summary>
    private void PerformCounterAttack()
    {
        if (target == null) return;
        
        if (animator != null)
        {
            animator.SetTrigger("Counter");
        }
        
        // 타겟이 범위 내에 있으면 데미지
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange * 1.2f)
        {
            var playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(counterDamage, DamageTag.Physical);
            }
        }
    }
    
    /// <summary>
    /// 방패 활성화/비활성화
    /// </summary>
    private void ActivateShield(bool activate)
    {
        isShieldActive = activate;
        
        if (shieldObject != null)
        {
            shieldObject.SetActive(activate);
        }
        
        // 방패 파괴 시 이동 속도 증가
        moveSpeed = activate ? 1.8f : 2.5f;
    }
    
    /// <summary>
    /// 무적 상태 코루틴
    /// </summary>
    private IEnumerator ActivateInvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        lastInvulnerabilityTime = Time.time;
        
        // 무적 이펙트
        GameObject invulEffect = null;
        if (invulnerabilityEffect != null)
        {
            invulEffect = Instantiate(invulnerabilityEffect, transform);
        }
        
        // 스프라이트 효과 (반짝임)
        StartCoroutine(InvulnerabilityVisualEffect());
        
        // 무적 시간 대기
        yield return new WaitForSeconds(invulnerabilityDuration);
        
        isInvulnerable = false;
        
        // 이펙트 정리
        if (invulEffect != null)
        {
            Destroy(invulEffect);
        }
    }
    
    /// <summary>
    /// 무적 시각 효과
    /// </summary>
    private IEnumerator InvulnerabilityVisualEffect()
    {
        float effectDuration = invulnerabilityDuration;
        float blinkInterval = 0.1f;
        
        while (effectDuration > 0)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            yield return new WaitForSeconds(blinkInterval);
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray;
            }
            
            yield return new WaitForSeconds(blinkInterval);
            
            effectDuration -= blinkInterval * 2;
        }
        
        // 원래 색상으로 복원
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }
    
    // 돌진 중 충돌 처리
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        // 돌진 중 플레이어와 충돌
        if (isCharging && other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(chargeDamage, DamageTag.Physical);
            }
        }
        else
        {
            base.OnTriggerEnter2D(other);
        }
    }
    
    protected override void DropItems()
    {
        base.DropItems();
        
        // 방패 스켈레톤 특별 드롭 (방패 파편)
        if (Random.value <= 0.3f) // 30% 확률
        {
            Debug.Log("방패 스켈레톤이 방패 파편을 떨어뜨렸습니다!");
        }
    }
    
    public override string GetEnemyInfo()
    {
        return base.GetEnemyInfo() + 
               $"\nShield Health: {shieldHealth:F0}/{maxShieldHealth:F0}" +
               $"\nBlock Chance: {blockChance * 100:F0}%" +
               $"\nInvulnerable: {(isInvulnerable ? "Yes" : "No")}";
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 돌진 범위 시각화
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chargeRange);
        
        // 무적 상태 표시
        if (isInvulnerable && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}