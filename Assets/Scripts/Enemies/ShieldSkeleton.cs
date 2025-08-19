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
    
    [Header("방패 블록 시각 효과")]
    [SerializeField] private Color rippleColor = Color.cyan;
    [SerializeField] private float rippleMaxRadius = 2f;
    [SerializeField] private float rippleDuration = 0.5f;
    [SerializeField] private float rippleWidth = 0.1f;
    
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
        
        // 방패 스켈레톤 초기 스탯 설정 (Inspector 설정 사용)
        enemyName = "Shield Skeleton";
        // maxHealth = 120f; // Inspector 설정 사용
        // currentHealth = maxHealth; // 자동 설정됨
        // moveSpeed = 1.8f; // Inspector 설정 사용
        // damage는 Inspector 설정 사용 (하드코딩 제거)
        // attackRange = 1.8f; // Inspector 설정 사용
        // attackCooldown = 2f; // Inspector 설정 사용
        // detectionRange = 8f; // Inspector 설정 사용
        // expValue = 50; // Inspector 설정 사용
        
        // 컴포넌트 설정
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // 방패 오브젝트 설정
        Debug.Log($"[ShieldSkeleton] Initialize: shieldObject={shieldObject}, isShieldActive={isShieldActive}");
        if (shieldObject != null)
        {
            shieldObject.SetActive(isShieldActive);
            Debug.Log($"[ShieldSkeleton] 방패 오브젝트 활성화 설정: {isShieldActive}");
        }
        else
        {
            Debug.LogWarning("[ShieldSkeleton] shieldObject가 null입니다!");
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
        Debug.Log($"[ShieldSkeleton] ExecuteAttack 호출: 데미지={damage}, 공격범위={attackRange}");
        
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
                Debug.Log($"[ShieldSkeleton] 플레이어에게 {damage} 데미지 적용!");
                playerHealth.TakeDamage(damage, DamageTag.Physical);
            }
        }
        
        Invoke(nameof(OnAttackComplete), 0.8f);
    }
    
    public override void TakeDamage(float damageAmount, DamageTag tag = DamageTag.Physical)
    {
        Debug.Log($"[ShieldSkeleton] TakeDamage 호출: {damageAmount} 데미지, 방패활성: {isShieldActive}, 무적: {isInvulnerable}");
        
        if (isDead || isInvulnerable) return;
        
        // 방패가 활성화된 경우 블록 체크
        if (isShieldActive && Random.value <= blockChance)
        {
            Debug.Log($"[ShieldSkeleton] 방패 블록 성공! 블록 확률: {blockChance}");
            HandleBlock(damageAmount, tag);
            return;
        }
        
        Debug.Log($"[ShieldSkeleton] 방패 블록 실패 또는 비활성, 일반 데미지 처리");
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
        
        // 방패 스프라이트 + 원형 파동 블록 효과
        StartCoroutine(ShieldBlockEffect());
        
        // 기존 블록 이펙트도 유지 (설정되어 있다면)
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
    /// 방패 블록 시각 효과 (방패 스프라이트 + 원형 파동)
    /// </summary>
    private IEnumerator ShieldBlockEffect()
    {
        if (shieldObject == null) yield break;
        
        // 1. 방패 스프라이트 효과
        SpriteRenderer shieldRenderer = shieldObject.GetComponent<SpriteRenderer>();
        if (shieldRenderer != null)
        {
            StartCoroutine(ShieldGlowEffect(shieldRenderer));
        }
        
        // 2. 원형 파동 생성
        CreateShieldRipple();
        
        yield return null;
    }
    
    /// <summary>
    /// 방패 스프라이트 빛나기 + 진동 효과
    /// </summary>
    private IEnumerator ShieldGlowEffect(SpriteRenderer shieldRenderer)
    {
        Color originalColor = shieldRenderer.color;
        Vector3 originalScale = shieldObject.transform.localScale;
        
        float duration = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 방패가 시안색으로 빛나고 진동
            float intensity = Mathf.Sin(progress * Mathf.PI * 3) * 0.5f + 0.5f;
            shieldRenderer.color = Color.Lerp(originalColor, Color.cyan, intensity * 0.7f);
            
            // 미세한 진동 효과
            float shake = Mathf.Sin(progress * Mathf.PI * 10) * 0.05f;
            shieldObject.transform.localScale = originalScale * (1f + shake);
            
            yield return null;
        }
        
        // 원래 상태로 복원
        shieldRenderer.color = originalColor;
        shieldObject.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 방패 주변 원형 파동 생성
    /// </summary>
    private void CreateShieldRipple()
    {
        // 동적으로 원형 파동 생성
        GameObject ripple = new GameObject("ShieldRipple");
        ripple.transform.position = transform.position;
        
        // LineRenderer로 원 그리기
        LineRenderer lr = ripple.AddComponent<LineRenderer>();
        Material rippleMaterial = new Material(Shader.Find("Sprites/Default"));
        rippleMaterial.color = rippleColor;
        lr.material = rippleMaterial;
        lr.startWidth = rippleWidth;
        lr.endWidth = rippleWidth;
        lr.positionCount = 50;
        lr.useWorldSpace = false;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = 10; // 다른 오브젝트 위에 표시
        
        StartCoroutine(AnimateRipple(ripple, lr, rippleMaterial));
    }
    
    /// <summary>
    /// 원형 파동 애니메이션
    /// </summary>
    private IEnumerator AnimateRipple(GameObject ripple, LineRenderer lr, Material rippleMaterial)
    {
        float elapsed = 0f;
        
        while (elapsed < rippleDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rippleDuration;
            
            // 원의 크기 증가 (0 -> Inspector 설정값)
            float radius = progress * rippleMaxRadius;
            
            // 원 점들 계산
            for (int i = 0; i < lr.positionCount; i++)
            {
                float angle = i * 2f * Mathf.PI / lr.positionCount;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius, 
                    Mathf.Sin(angle) * radius, 
                    0
                );
                lr.SetPosition(i, pos);
            }
            
            // 투명도 감소 (1 -> 0)
            Color color = rippleMaterial.color;
            color.a = 1f - progress;
            rippleMaterial.color = color;
            
            yield return null;
        }
        
        // 파동 제거
        Destroy(ripple);
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
        Debug.Log($"[ShieldSkeleton] ActivateShield 호출: {activate}");
        isShieldActive = activate;
        
        if (shieldObject != null)
        {
            shieldObject.SetActive(activate);
            Debug.Log($"[ShieldSkeleton] 방패 오브젝트 상태 변경: {activate}");
        }
        else
        {
            Debug.LogWarning("[ShieldSkeleton] ActivateShield: shieldObject가 null입니다!");
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
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        
        Collider2D other = collision.collider;
        
        // 돌진 중 플레이어와 충돌
        if (isCharging && other.CompareTag("Player"))
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(chargeDamage, DamageTag.Physical);
            }
        }
        
        // 부모 클래스의 충돌 처리도 호출
        base.OnCollisionEnter2D(collision);
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