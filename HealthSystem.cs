using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHearts = 5;
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    
    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int> OnMaxHealthChanged;
    public UnityEvent OnPlayerDeath;
    
    private const int HEALTH_PER_HEART = 4;
    private const int MAX_HEARTS_LIMIT = 10;
    
    private void Awake()
    {
        maxHealth = maxHearts * HEALTH_PER_HEART;
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // UI 업데이트
        OnHealthChanged?.Invoke(currentHealth);
        OnMaxHealthChanged?.Invoke(maxHealth);
    }
    
    /// <summary>
    /// 데미지를 받습니다
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0)
        {
            OnPlayerDeath?.Invoke();
        }
    }
    
    /// <summary>
    /// 체력을 회복합니다
    /// </summary>
    /// <param name="healAmount">회복할 체력 양</param>
    public void Heal(int healAmount)
    {
        if (currentHealth >= maxHealth) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// 최대 체력을 증가시킵니다 (하트 추가)
    /// </summary>
    /// <param name="heartsToAdd">추가할 하트 개수</param>
    public void IncreaseMaxHealth(int heartsToAdd)
    {
        if (maxHearts >= MAX_HEARTS_LIMIT) return;
        
        maxHearts = Mathf.Min(MAX_HEARTS_LIMIT, maxHearts + heartsToAdd);
        int previousMaxHealth = maxHealth;
        maxHealth = maxHearts * HEALTH_PER_HEART;
        
        // 현재 체력도 증가한 만큼 채워줌
        currentHealth += (maxHealth - previousMaxHealth);
        
        OnMaxHealthChanged?.Invoke(maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// 체력을 완전히 회복합니다
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    // 현재 상태 확인용 프로퍼티들
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int MaxHearts => maxHearts;
    public int CurrentHearts => Mathf.CeilToInt((float)currentHealth / HEALTH_PER_HEART);
    public bool IsDead => currentHealth <= 0;
    public bool IsFullHealth => currentHealth >= maxHealth;
    
    /// <summary>
    /// 특정 하트의 채워진 정도를 반환합니다 (0~4)
    /// </summary>
    /// <param name="heartIndex">하트 인덱스 (0부터 시작)</param>
    /// <returns>해당 하트의 채워진 정도 (0~4)</returns>
    public int GetHeartFillAmount(int heartIndex)
    {
        if (heartIndex >= maxHearts) return 0;
        
        int healthForThisHeart = currentHealth - (heartIndex * HEALTH_PER_HEART);
        return Mathf.Clamp(healthForThisHeart, 0, HEALTH_PER_HEART);
    }
}