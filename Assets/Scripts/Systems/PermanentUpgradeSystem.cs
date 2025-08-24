using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영구 업그레이드 타입
/// </summary>
public enum PermanentUpgradeType
{
    MaxHealth,      // 최대 체력 증가
    Damage,         // 데미지 증가
    MoveSpeed,      // 이동속도 증가
    ExpMultiplier   // 경험치 배율 증가
}

/// <summary>
/// 영구 업그레이드 데이터
/// </summary>
[System.Serializable]
public class PermanentUpgrade
{
    [Header("기본 정보")]
    public PermanentUpgradeType type;
    public string displayName;
    public string description;
    public int cost;
    
    [Header("효과")]
    public float effectValue;           // 증가량 (예: 4.0f = 4체력, 0.1f = 10%)
    public bool isPercentage = false;   // true면 퍼센트, false면 절댓값
    
    [Header("레벨 제한")]
    public int maxLevel = 10;          // 최대 구매 가능 레벨
    
    [Header("가격 증가")]
    public float costMultiplier = 1.2f; // 레벨당 가격 증가 배율
}

/// <summary>
/// 영구 업그레이드 시스템 - 골드로 구매하는 능력치 강화
/// </summary>
public class PermanentUpgradeSystem : MonoBehaviour
{
    [Header("업그레이드 목록")]
    [SerializeField] private List<PermanentUpgrade> availableUpgrades = new List<PermanentUpgrade>();
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 싱글톤
    public static PermanentUpgradeSystem Instance { get; private set; }
    
    // 참조
    private GoldSystem goldSystem;
    
    // 업그레이드 레벨 저장 (PlayerPrefs 사용)
    private Dictionary<PermanentUpgradeType, int> upgradeLevels = new Dictionary<PermanentUpgradeType, int>();
    
    // 이벤트
    public System.Action<PermanentUpgradeType, int> OnUpgradePurchased;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeUpgrades();
        LoadUpgradeLevels();
    }
    
    private void Start()
    {
        InitializeReferences();
    }
    
    /// <summary>
    /// 기본 업그레이드 목록 초기화
    /// </summary>
    private void InitializeUpgrades()
    {
        if (availableUpgrades.Count == 0)
        {
            availableUpgrades.AddRange(new List<PermanentUpgrade>
            {
                new PermanentUpgrade
                {
                    type = PermanentUpgradeType.MaxHealth,
                    displayName = "튼튼한 체력",
                    description = "최대 체력을 1하트 증가시킵니다",
                    cost = 2500,
                    effectValue = 4f, // 1하트 = 4체력
                    isPercentage = false,
                    maxLevel = 7, // 최대 10하트까지 (기본 3하트 + 7하트)
                    costMultiplier = 1.3f
                },
                new PermanentUpgrade
                {
                    type = PermanentUpgradeType.Damage,
                    displayName = "강력한 타격",
                    description = "모든 무기 데미지를 10% 증가시킵니다",
                    cost = 2500,
                    effectValue = 0.1f, // 10%
                    isPercentage = true,
                    maxLevel = 10,
                    costMultiplier = 1.25f
                },
                new PermanentUpgrade
                {
                    type = PermanentUpgradeType.MoveSpeed,
                    displayName = "신속한 발걸음",
                    description = "이동속도를 10% 증가시킵니다",
                    cost = 2500,
                    effectValue = 0.1f, // 10%
                    isPercentage = true,
                    maxLevel = 8,
                    costMultiplier = 1.2f
                },
                new PermanentUpgrade
                {
                    type = PermanentUpgradeType.ExpMultiplier,
                    displayName = "풍부한 지식",
                    description = "경험치 획득량을 15% 증가시킵니다",
                    cost = 2500,
                    effectValue = 0.15f, // 15%
                    isPercentage = true,
                    maxLevel = 10,
                    costMultiplier = 1.4f
                }
            });
        }
        
        // 업그레이드 레벨 Dictionary 초기화
        foreach (var upgrade in availableUpgrades)
        {
            if (!upgradeLevels.ContainsKey(upgrade.type))
            {
                upgradeLevels[upgrade.type] = 0;
            }
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        goldSystem = GoldSystem.Instance;
        if (goldSystem == null)
        {
            Debug.LogError("[PermanentUpgradeSystem] GoldSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 업그레이드 레벨 로드
    /// </summary>
    private void LoadUpgradeLevels()
    {
        foreach (PermanentUpgradeType upgradeType in System.Enum.GetValues(typeof(PermanentUpgradeType)))
        {
            string key = $"PermanentUpgrade_{upgradeType}";
            int level = PlayerPrefs.GetInt(key, 0);
            upgradeLevels[upgradeType] = level;
            
            if (enableDebugLogs && level > 0)
            {
                Debug.Log($"[PermanentUpgradeSystem] 로드: {upgradeType} = 레벨 {level}");
            }
        }
    }
    
    /// <summary>
    /// 업그레이드 레벨 저장
    /// </summary>
    private void SaveUpgradeLevels()
    {
        foreach (var kvp in upgradeLevels)
        {
            string key = $"PermanentUpgrade_{kvp.Key}";
            PlayerPrefs.SetInt(key, kvp.Value);
        }
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 업그레이드 구매
    /// </summary>
    /// <param name="upgradeType">구매할 업그레이드 타입</param>
    /// <returns>구매 성공 여부</returns>
    public bool PurchaseUpgrade(PermanentUpgradeType upgradeType)
    {
        PermanentUpgrade upgrade = GetUpgradeByType(upgradeType);
        if (upgrade == null)
        {
            Debug.LogError($"[PermanentUpgradeSystem] 업그레이드 타입을 찾을 수 없습니다: {upgradeType}");
            return false;
        }
        
        int currentLevel = GetUpgradeLevel(upgradeType);
        
        // 최대 레벨 확인
        if (currentLevel >= upgrade.maxLevel)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PermanentUpgradeSystem] {upgrade.displayName}은(는) 이미 최대 레벨입니다.");
            }
            return false;
        }
        
        // 가격 계산
        int currentCost = GetUpgradeCost(upgradeType);
        
        // 골드 확인 및 차감
        if (goldSystem == null || goldSystem.CurrentGold < currentCost)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PermanentUpgradeSystem] 골드가 부족합니다. 필요: {currentCost}, 보유: {goldSystem?.CurrentGold ?? 0}");
            }
            return false;
        }
        
        // 골드 차감
        goldSystem.SpendGold(currentCost);
        
        // 레벨 증가
        upgradeLevels[upgradeType] = currentLevel + 1;
        SaveUpgradeLevels();
        
        // 이벤트 발생
        OnUpgradePurchased?.Invoke(upgradeType, upgradeLevels[upgradeType]);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[PermanentUpgradeSystem] ✅ 업그레이드 구매 완료: {upgrade.displayName} 레벨 {upgradeLevels[upgradeType]} (비용: {currentCost})");
        }
        
        return true;
    }
    
    /// <summary>
    /// 업그레이드 타입으로 업그레이드 데이터 조회
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>업그레이드 데이터</returns>
    public PermanentUpgrade GetUpgradeByType(PermanentUpgradeType upgradeType)
    {
        foreach (var upgrade in availableUpgrades)
        {
            if (upgrade.type == upgradeType)
                return upgrade;
        }
        return null;
    }
    
    /// <summary>
    /// 업그레이드 현재 레벨 조회
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>현재 레벨</returns>
    public int GetUpgradeLevel(PermanentUpgradeType upgradeType)
    {
        return upgradeLevels.ContainsKey(upgradeType) ? upgradeLevels[upgradeType] : 0;
    }
    
    /// <summary>
    /// 업그레이드 현재 가격 조회
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>현재 가격</returns>
    public int GetUpgradeCost(PermanentUpgradeType upgradeType)
    {
        PermanentUpgrade upgrade = GetUpgradeByType(upgradeType);
        if (upgrade == null) return 0;
        
        int currentLevel = GetUpgradeLevel(upgradeType);
        float cost = upgrade.cost * Mathf.Pow(upgrade.costMultiplier, currentLevel);
        return Mathf.RoundToInt(cost);
    }
    
    /// <summary>
    /// 업그레이드 총 효과값 계산
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>총 효과값</returns>
    public float GetTotalUpgradeValue(PermanentUpgradeType upgradeType)
    {
        PermanentUpgrade upgrade = GetUpgradeByType(upgradeType);
        if (upgrade == null) return 0f;
        
        int level = GetUpgradeLevel(upgradeType);
        return upgrade.effectValue * level;
    }
    
    /// <summary>
    /// 업그레이드 구매 가능 여부 확인
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    /// <returns>구매 가능 여부</returns>
    public bool CanPurchaseUpgrade(PermanentUpgradeType upgradeType)
    {
        PermanentUpgrade upgrade = GetUpgradeByType(upgradeType);
        if (upgrade == null) return false;
        
        int currentLevel = GetUpgradeLevel(upgradeType);
        if (currentLevel >= upgrade.maxLevel) return false;
        
        int cost = GetUpgradeCost(upgradeType);
        return goldSystem != null && goldSystem.CurrentGold >= cost;
    }
    
    /// <summary>
    /// 모든 업그레이드 목록 조회
    /// </summary>
    /// <returns>업그레이드 목록</returns>
    public List<PermanentUpgrade> GetAllUpgrades()
    {
        return new List<PermanentUpgrade>(availableUpgrades);
    }
    
    /// <summary>
    /// 업그레이드 데이터 리셋 (디버그용)
    /// </summary>
    [ContextMenu("업그레이드 데이터 리셋")]
    public void ResetAllUpgrades()
    {
        foreach (PermanentUpgradeType upgradeType in System.Enum.GetValues(typeof(PermanentUpgradeType)))
        {
            string key = $"PermanentUpgrade_{upgradeType}";
            PlayerPrefs.DeleteKey(key);
            upgradeLevels[upgradeType] = 0;
        }
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
        {
            Debug.Log("[PermanentUpgradeSystem] 🔄 모든 업그레이드 데이터가 리셋되었습니다.");
        }
    }
}