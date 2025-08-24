using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어 체력 시스템
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] private float maxHealth = 12f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float healthRegenRate = 0.5f; // 초당 회복량
    [SerializeField] private float regenDelay = 30f; // 피해 후 회복 시작 지연
    
    [Header("방어 설정")]
    [SerializeField] private float armor = 0f;
    [SerializeField] private float damageReduction = 0f; // 0~1 (백분율)
    [SerializeField] private bool hasInvincibility = false;
    [SerializeField] private float invincibilityDuration = 1f;
    
    // 이벤트
    [System.Serializable]
    public class HealthEvents
    {
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent<float> OnDamageTaken;
        public UnityEvent<float> OnHealthRestored;
        public UnityEvent OnDeath;
        public UnityEvent OnRevive;
        public UnityEvent OnInvincibilityStart;
        public UnityEvent OnInvincibilityEnd;
    }
    
    [Header("이벤트")]
    [SerializeField] public HealthEvents events;
    
    // 내부 변수
    private bool isDead = false;
    private bool isInvincible = false;
    private float lastDamageTime;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private SPUM_Prefabs spumController;
    
    // 프로퍼티
    public float Health => currentHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;
    public float Armor => armor;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spumController = GetComponentInChildren<SPUM_Prefabs>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Events 초기화 (Inspector에서 설정되지 않은 경우)
        if (events == null)
        {
            events = new HealthEvents();
        }
    }
    
    private void Update()
    {
        if (!isDead)
        {
            HandleHealthRegen();
        }
    }
    
    /// <summary>
    /// 체력 재생 처리
    /// </summary>
    private void HandleHealthRegen()
    {
        if (currentHealth < maxHealth && Time.time >= lastDamageTime + regenDelay)
        {
            float regenAmount = healthRegenRate * Time.deltaTime;
            RestoreHealth(regenAmount);
        }
    }
    
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float damageAmount, DamageTag tag = DamageTag.Physical)
    {
        Debug.Log($"PlayerHealth: TakeDamage called with {damageAmount}. Current health: {currentHealth}, isDead: {isDead}, isInvincible: {isInvincible}");

        if (isDead || isInvincible)
        {
            Debug.Log("PlayerHealth: Damage blocked (dead or invincible)");
            return;
        }

        // 방어력 및 상태 이상 계산
        var status = GetComponent<StatusController>();
        if (status != null)
        {
            damageAmount *= status.GetDamageTakenMultiplier(tag);
        }

        float finalDamage = CalculateFinalDamage(damageAmount);
        Debug.Log($"PlayerHealth: Final damage after armor: {finalDamage}");
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        lastDamageTime = Time.time;
        
        Debug.Log($"PlayerHealth: New health: {currentHealth}");
        
        // 이벤트 발생
        Debug.Log($"PlayerHealth: Invoking events - events null: {events == null}");
        if (events != null)
        {
            Debug.Log($"PlayerHealth: OnHealthChanged null: {events.OnHealthChanged == null}");
            events.OnHealthChanged?.Invoke(currentHealth);
            events.OnDamageTaken?.Invoke(finalDamage);
        }
        
        // SPUM 애니메이션 재생
        if (spumController != null)
        {
            spumController.PlayAnimation(PlayerState.DAMAGED, 0);
        }
        
        // 데미지 시각 효과
        StartCoroutine(DamageFlashCoroutine());
        
        // 무적 시간 시작
        if (hasInvincibility && !isInvincible)
        {
            StartCoroutine(InvincibilityCoroutine());
        }
        
        // 사망 체크
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 최종 데미지 계산
    /// </summary>
    private float CalculateFinalDamage(float baseDamage)
    {
        // 방어력에 의한 데미지 감소
        float armorReduction = armor / (armor + 100f);
        float damageAfterArmor = baseDamage * (1f - armorReduction);
        
        // 추가 데미지 감소
        float finalDamage = damageAfterArmor * (1f - damageReduction);
        
        return Mathf.Max(1f, finalDamage); // 최소 1 데미지
    }
    
    /// <summary>
    /// 체력 회복
    /// </summary>
    public void RestoreHealth(float amount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        if (currentHealth > oldHealth)
        {
            events?.OnHealthChanged?.Invoke(currentHealth);
            events?.OnHealthRestored?.Invoke(amount);
        }
    }
    
    /// <summary>
    /// 최대 체력 증가
    /// </summary>
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // 현재 체력도 함께 증가
        events?.OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// 완전 회복
    /// </summary>
    public void FullHeal()
    {
        RestoreHealth(maxHealth);
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // SPUM 애니메이션 재생
        if (spumController != null)
        {
            spumController.PlayAnimation(PlayerState.DEATH, 0);
        }
        
        // 게임 매니저에 사망 알림
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerDied();
        }
        
        events?.OnDeath?.Invoke();
    }
    
    /// <summary>
    /// 부활
    /// </summary>
    public void Revive(float healthAmount = -1f)
    {
        if (!isDead) return;
        
        isDead = false;
        currentHealth = healthAmount > 0 ? healthAmount : maxHealth * 0.5f; // 기본적으로 절반 체력으로 부활
        
        events?.OnRevive?.Invoke();
        events?.OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// 무적 시간 코루틴
    /// </summary>
    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        events?.OnInvincibilityStart?.Invoke();
        
        // 깜빡임 효과
        StartCoroutine(InvincibilityBlinkCoroutine());
        
        yield return new WaitForSeconds(invincibilityDuration);
        
        isInvincible = false;
        events?.OnInvincibilityEnd?.Invoke();
        
        // 원래 색상으로 복원
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    /// <summary>
    /// 무적 시간 깜빡임 효과
    /// </summary>
    private System.Collections.IEnumerator InvincibilityBlinkCoroutine()
    {
        float blinkInterval = 0.1f;
        float elapsedTime = 0f;
        
        while (isInvincible && elapsedTime < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            }
            
            yield return new WaitForSeconds(blinkInterval);
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            
            yield return new WaitForSeconds(blinkInterval);
            
            elapsedTime += blinkInterval * 2;
        }
    }
    
    /// <summary>
    /// 데미지 받을 때 번쩍임 효과
    /// </summary>
    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
    
    /// <summary>
    /// 방어력 설정
    /// </summary>
    public void SetArmor(float newArmor)
    {
        armor = Mathf.Max(0, newArmor);
    }
    
    /// <summary>
    /// 데미지 감소 설정
    /// </summary>
    public void SetDamageReduction(float reduction)
    {
        damageReduction = Mathf.Clamp01(reduction);
    }
    
    /// <summary>
    /// 최대 체력 설정 (영구 업그레이드용)
    /// </summary>
    /// <param name="newMaxHealth">새로운 최대 체력</param>
    public void SetMaxHealth(int newMaxHealth)
    {
        float healthRatio = (float)currentHealth / maxHealth;
        maxHealth = Mathf.Max(1, newMaxHealth);
        
        // 체력바 UI 업데이트
        events?.OnHealthChanged?.Invoke(currentHealth);
        
        Debug.Log($"[PlayerHealth] 최대 체력 설정: {maxHealth} (하트 {maxHealth/4}개)");
    }
    
    /// <summary>
    /// 현재 체력 직접 설정 (영구 업그레이드용)
    /// </summary>
    /// <param name="newCurrentHealth">새로운 현재 체력</param>
    public void SetCurrentHealth(int newCurrentHealth)
    {
        currentHealth = Mathf.Clamp(newCurrentHealth, 0, maxHealth);
        
        // 체력바 UI 업데이트
        events?.OnHealthChanged?.Invoke(currentHealth);
        
        Debug.Log($"[PlayerHealth] 현재 체력 설정: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 체력 재생 설정
    /// </summary>
    public void SetHealthRegen(float newRate)
    {
        healthRegenRate = Mathf.Max(0, newRate);
    }
    
    /// <summary>
    /// 플레이어 정보 반환
    /// </summary>
    public string GetHealthInfo()
    {
        return $"체력: {currentHealth:F0}/{maxHealth:F0}\n" +
               $"방어력: {armor:F0}\n" +
               $"데미지 감소: {damageReduction * 100:F0}%\n" +
               $"체력 재생: {healthRegenRate:F1}/s\n" +
               $"상태: {(isDead ? "사망" : isInvincible ? "무적" : "정상")}";
    }
}