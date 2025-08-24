using UnityEngine;

/// <summary>
/// 영구 업그레이드 효과를 게임에 실제 적용하는 클래스
/// </summary>
public class UpgradeEffectApplier : MonoBehaviour
{
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 참조
    private PermanentUpgradeSystem upgradeSystem;
    private PlayerHealth playerHealth;
    private WeaponManager weaponManager;
    
    // 기본값 저장 (업그레이드 계산용)
    private float basePlayerSpeed = 5f;
    private int baseMaxHealth = 12; // 3하트
    
    // 싱글톤
    public static UpgradeEffectApplier Instance { get; private set; }
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializeReferences();
        SubscribeToEvents();
        ApplyAllUpgrades(); // 게임 시작 시 모든 업그레이드 적용
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        upgradeSystem = PermanentUpgradeSystem.Instance;
        playerHealth = FindObjectOfType<PlayerHealth>();
        weaponManager = FindObjectOfType<WeaponManager>();
        
        // 기본값 저장 (첫 실행 시에만)
        if (playerHealth != null && playerHealth.MaxHealth > 0)
        {
            // 이미 업그레이드가 적용된 상태일 수 있으므로 기본값을 역산
            int maxHealthUpgradeLevel = upgradeSystem?.GetUpgradeLevel(PermanentUpgradeType.MaxHealth) ?? 0;
            baseMaxHealth = Mathf.RoundToInt(playerHealth.MaxHealth) - (maxHealthUpgradeLevel * 4);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[UpgradeEffectApplier] 참조 초기화 완료. 기본 체력: {baseMaxHealth}");
        }
    }
    
    /// <summary>
    /// 업그레이드 구매 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradePurchased += OnUpgradePurchased;
        }
    }
    
    /// <summary>
    /// 업그레이드 구매 시 호출되는 콜백
    /// </summary>
    /// <param name="upgradeType">구매된 업그레이드 타입</param>
    /// <param name="newLevel">새 레벨</param>
    private void OnUpgradePurchased(PermanentUpgradeType upgradeType, int newLevel)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[UpgradeEffectApplier] 업그레이드 구매됨: {upgradeType} 레벨 {newLevel}");
        }
        
        // 해당 업그레이드만 다시 적용
        ApplySpecificUpgrade(upgradeType);
    }
    
    /// <summary>
    /// 모든 업그레이드 효과 적용
    /// </summary>
    public void ApplyAllUpgrades()
    {
        if (upgradeSystem == null)
        {
            Debug.LogWarning("[UpgradeEffectApplier] PermanentUpgradeSystem이 없습니다.");
            return;
        }
        
        ApplySpecificUpgrade(PermanentUpgradeType.MaxHealth);
        ApplySpecificUpgrade(PermanentUpgradeType.Damage);
        ApplySpecificUpgrade(PermanentUpgradeType.MoveSpeed);
        ApplySpecificUpgrade(PermanentUpgradeType.ExpMultiplier);
        
        if (enableDebugLogs)
        {
            Debug.Log("[UpgradeEffectApplier] ✅ 모든 업그레이드 적용 완료");
        }
    }
    
    /// <summary>
    /// 특정 업그레이드 효과 적용
    /// </summary>
    /// <param name="upgradeType">적용할 업그레이드 타입</param>
    private void ApplySpecificUpgrade(PermanentUpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case PermanentUpgradeType.MaxHealth:
                ApplyMaxHealthUpgrade();
                break;
                
            case PermanentUpgradeType.Damage:
                ApplyDamageUpgrade();
                break;
                
            case PermanentUpgradeType.MoveSpeed:
                ApplyMoveSpeedUpgrade();
                break;
                
            case PermanentUpgradeType.ExpMultiplier:
                ApplyExpMultiplierUpgrade();
                break;
        }
    }
    
    /// <summary>
    /// 최대 체력 업그레이드 적용
    /// </summary>
    private void ApplyMaxHealthUpgrade()
    {
        if (playerHealth == null || upgradeSystem == null) return;
        
        float upgradeValue = upgradeSystem.GetTotalUpgradeValue(PermanentUpgradeType.MaxHealth);
        int newMaxHealth = Mathf.RoundToInt(baseMaxHealth + upgradeValue);
        
        // 현재 체력 비율 유지
        float healthRatio = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth;
        
        playerHealth.SetMaxHealth(newMaxHealth);
        
        // 체력 비율에 따라 현재 체력 조정 (선택사항)
        int newCurrentHealth = Mathf.RoundToInt(newMaxHealth * healthRatio);
        playerHealth.SetCurrentHealth(newCurrentHealth);
        
        if (enableDebugLogs)
        {
            int heartCount = newMaxHealth / 4;
            Debug.Log($"[UpgradeEffectApplier] 💖 최대 체력 적용: {newMaxHealth} ({heartCount}하트)");
        }
    }
    
    /// <summary>
    /// 데미지 업그레이드 적용
    /// </summary>
    private void ApplyDamageUpgrade()
    {
        if (weaponManager == null || upgradeSystem == null) return;
        
        float upgradeMultiplier = 1f + upgradeSystem.GetTotalUpgradeValue(PermanentUpgradeType.Damage);
        
        // WeaponManager에 데미지 배율 적용 (WeaponManager에 해당 메서드가 있다고 가정)
        if (weaponManager.GetComponent<IDamageMultiplier>() != null)
        {
            weaponManager.GetComponent<IDamageMultiplier>().SetDamageMultiplier(upgradeMultiplier);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[UpgradeEffectApplier] ⚔️ 데미지 배율 적용: x{upgradeMultiplier:F2}");
        }
    }
    
    /// <summary>
    /// 이동속도 업그레이드 적용
    /// </summary>
    private void ApplyMoveSpeedUpgrade()
    {
        if (upgradeSystem == null) return;
        
        float upgradeMultiplier = 1f + upgradeSystem.GetTotalUpgradeValue(PermanentUpgradeType.MoveSpeed);
        float newSpeed = basePlayerSpeed * upgradeMultiplier;
        
        // 플레이어 이동속도 적용 (PlayerObj 등에 적용)
        PlayerObj playerObj = FindObjectOfType<PlayerObj>();
        if (playerObj != null && playerObj.GetComponent<IMovementSpeed>() != null)
        {
            playerObj.GetComponent<IMovementSpeed>().SetMovementSpeed(newSpeed);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[UpgradeEffectApplier] 🏃 이동속도 적용: {newSpeed:F1} (배율: x{upgradeMultiplier:F2})");
        }
    }
    
    /// <summary>
    /// 경험치 배율 업그레이드 적용
    /// </summary>
    private void ApplyExpMultiplierUpgrade()
    {
        if (upgradeSystem == null) return;
        
        float upgradeMultiplier = 1f + upgradeSystem.GetTotalUpgradeValue(PermanentUpgradeType.ExpMultiplier);
        
        // GameManager나 ExperienceSystem에 경험치 배율 적용
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.GetComponent<IExperienceMultiplier>() != null)
        {
            gameManager.GetComponent<IExperienceMultiplier>().SetExperienceMultiplier(upgradeMultiplier);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[UpgradeEffectApplier] 📚 경험치 배율 적용: x{upgradeMultiplier:F2}");
        }
    }
    
    /// <summary>
    /// 현재 적용된 모든 업그레이드 정보 출력 (디버그용)
    /// </summary>
    [ContextMenu("현재 업그레이드 상태 출력")]
    public void PrintCurrentUpgradeStatus()
    {
        if (upgradeSystem == null)
        {
            Debug.Log("[UpgradeEffectApplier] PermanentUpgradeSystem이 없습니다.");
            return;
        }
        
        Debug.Log("=== 현재 업그레이드 상태 ===");
        
        foreach (PermanentUpgradeType upgradeType in System.Enum.GetValues(typeof(PermanentUpgradeType)))
        {
            int level = upgradeSystem.GetUpgradeLevel(upgradeType);
            float value = upgradeSystem.GetTotalUpgradeValue(upgradeType);
            PermanentUpgrade upgrade = upgradeSystem.GetUpgradeByType(upgradeType);
            
            if (level > 0)
            {
                string valueText = upgrade.isPercentage ? $"{value * 100:F0}%" : value.ToString("F0");
                Debug.Log($"  {upgrade.displayName}: 레벨 {level} ({valueText})");
            }
            else
            {
                Debug.Log($"  {upgrade.displayName}: 레벨 0 (미구매)");
            }
        }
        
        Debug.Log("========================");
    }
    
    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradePurchased -= OnUpgradePurchased;
        }
    }
}

// 업그레이드 효과 적용을 위한 인터페이스들
public interface IDamageMultiplier
{
    void SetDamageMultiplier(float multiplier);
}

public interface IMovementSpeed
{
    void SetMovementSpeed(float speed);
}

public interface IExperienceMultiplier
{
    void SetExperienceMultiplier(float multiplier);
}