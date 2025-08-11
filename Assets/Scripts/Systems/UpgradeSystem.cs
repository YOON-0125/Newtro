using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 업그레이드 시스템 - 확장 가능한 레벨업 업그레이드 관리
/// </summary>
[System.Serializable]
public enum UpgradeType
{
    WeaponUpgrade,      // 기존 무기 강화
    NewWeapon,          // 새 무기 획득
    PlayerUpgrade,      // 플레이어 능력치 향상
    SpecialUpgrade      // 특수 업그레이드
}

/// <summary>
/// 업그레이드 옵션 데이터
/// </summary>
[System.Serializable]
public class UpgradeOption
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public UpgradeType type;
    
    [Header("조건")]
    public int minLevel = 1;
    public int maxLevel = 99;
    public int weight = 100;  // 선택 확률 가중치
    public bool canRepeat = true;
    public List<string> prerequisites = new List<string>(); // 선행 조건 업그레이드 ID
    public List<string> excludes = new List<string>(); // 동시 선택 불가 업그레이드 ID
    
    [Header("효과")]
    public float value1;  // 주 효과 값
    public float value2;  // 부 효과 값  
    public float value3;  // 추가 효과 값
    public string targetId; // 대상 (무기 이름, 스탯 이름 등)
}

/// <summary>
/// 플레이어가 획득한 업그레이드 기록
/// </summary>
[System.Serializable]
public class AcquiredUpgrade
{
    public string upgradeId;
    public int count; // 중복 획득 횟수
    public int levelAcquired; // 획득한 레벨
    public float totalValue1; // 누적 효과값
    public float totalValue2;
    public float totalValue3;
}

/// <summary>
/// 업그레이드 시스템 관리자
/// </summary>
public class UpgradeSystem : MonoBehaviour
{
    [Header("업그레이드 데이터베이스")]
    [SerializeField] private List<UpgradeOption> allUpgrades = new List<UpgradeOption>();
    
    [Header("시스템 설정")]
    [SerializeField] private int optionsPerLevelUp = 3;
    [SerializeField] private bool allowDuplicateOptions = false;
    [SerializeField] private float rarityBonusMultiplier = 0.1f;
    
    // 획득한 업그레이드 추적
    private Dictionary<string, AcquiredUpgrade> acquiredUpgrades = new Dictionary<string, AcquiredUpgrade>();
    private List<UpgradeOption> lastOfferedOptions = new List<UpgradeOption>();
    
    // 참조
    private WeaponManager weaponManager;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    
    // 싱글톤
    public static UpgradeSystem Instance { get; private set; }
    
    // 프로퍼티
    public IReadOnlyDictionary<string, AcquiredUpgrade> AcquiredUpgrades => acquiredUpgrades;
    public IReadOnlyList<UpgradeOption> LastOfferedOptions => lastOfferedOptions.AsReadOnly();
    
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
        
        InitializeReferences();
        InitializeDefaultUpgrades();
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        weaponManager = FindFirstObjectByType<WeaponManager>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        gameManager = FindFirstObjectByType<GameManager>();
        
        if (weaponManager == null) Debug.LogWarning("UpgradeSystem: WeaponManager를 찾을 수 없습니다!");
        if (playerHealth == null) Debug.LogWarning("UpgradeSystem: PlayerHealth를 찾을 수 없습니다!");
        if (gameManager == null) Debug.LogWarning("UpgradeSystem: GameManager를 찾을 수 없습니다!");
    }
    
    /// <summary>
    /// 기본 업그레이드 옵션들 초기화 (확장 가능)
    /// </summary>
    private void InitializeDefaultUpgrades()
    {
        Debug.Log($"[UpgradeSystem] 현재 업그레이드 개수: {allUpgrades.Count}");
        
        if (allUpgrades.Count == 0)
        {
            Debug.Log("[UpgradeSystem] 기본 업그레이드 옵션들 추가 중...");
            // 기본 업그레이드 옵션들 추가
            // AddDefaultUpgrades();
            Debug.Log($"[UpgradeSystem] 기본 업그레이드 추가 완료. 총 개수: {allUpgrades.Count}");
        }
        else
        {
            Debug.Log("[UpgradeSystem] Inspector에서 설정된 업그레이드 사용");
        }
    }
    
    /// <summary>
    /// 기본 업그레이드들 추가 (Inspector에서 설정하지 않은 경우)
    /// </summary>
    /*
    private void AddDefaultUpgrades()
    {
        // 무기 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "weapon_damage_boost",
            displayName = "무기 데미지 강화",
            description = "모든 무기의 데미지가 20% 증가합니다.",
            type = UpgradeType.WeaponUpgrade,
            value1 = 1.2f, // 데미지 배율
            weight = 100,
            canRepeat = true
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "weapon_speed_boost",
            displayName = "공격 속도 강화",
            description = "모든 무기의 공격 속도가 15% 증가합니다.",
            type = UpgradeType.WeaponUpgrade,
            value1 = 0.85f, // 쿨다운 배율 (85% = 15% 빨라짐)
            weight = 100,
            canRepeat = true
        });
        
        // 플레이어 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "health_boost",
            displayName = "체력 강화",
            description = "최대 체력이 1하트(4HP) 증가합니다.",
            type = UpgradeType.PlayerUpgrade,
            value1 = 4f, // 체력 증가량
            weight = 80,
            canRepeat = true
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "movement_speed_boost",
            displayName = "이동 속도 강화",
            description = "이동 속도가 20% 증가합니다.",
            type = UpgradeType.PlayerUpgrade,
            targetId = "movement_speed",
            value1 = 1.2f, // 이동속도 배율
            weight = 90,
            canRepeat = true
        });
        
        // 새 무기
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_fireball",
            displayName = "파이어볼 획득",
            description = "화염 투사체를 발사하는 파이어볼 무기를 획득합니다.",
            type = UpgradeType.NewWeapon,
            targetId = "Fireball",
            weight = 60,
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_chain_lightning",
            displayName = "라이트닝 체인 획득",
            description = "연쇄 번개로 여러 적을 동시에 공격하는 무기를 획득합니다.",
            type = UpgradeType.NewWeapon,
            targetId = "ChainLightning",
            weight = 60,
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_electric_sphere",
            displayName = "전기 구체 획득",
            description = "주변에 전기 피해를 주는 구체를 생성하는 무기를 획득합니다.",
            type = UpgradeType.NewWeapon,
            targetId = "ElectricSphere",
            weight = 50,
            minLevel = 2,
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_frost_nova",
            displayName = "프로스트 노바 획득",
            description = "플레이어 주변으로 얼음 폭발을 일으키는 무기를 획득합니다.",
            type = UpgradeType.NewWeapon,
            targetId = "FrostNova",
            weight = 50,
            minLevel = 3,
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_raining_fire",
            displayName = "레이닝 파이어 획득",
            description = "하늘에서 화염구를 떨어뜨려 화염 지대를 만드는 무기를 획득합니다.",
            type = UpgradeType.NewWeapon,
            targetId = "RainingFire",
            weight = 40,
            minLevel = 4,
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_thunder",
            displayName = "썬더 획득",
            description = "번개를 떨어뜨려 전기 지대를 생성하는 무기를 획득합니다.",
            type = UpgradeType.NewWeapon,
            targetId = "Thunder",
            weight = 40,
            minLevel = 5,
            canRepeat = false
        });
        
        // 특수 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "heal_on_kill",
            displayName = "처치 시 회복",
            description = "적을 처치할 때마다 체력이 1/4칸 회복됩니다.",
            type = UpgradeType.SpecialUpgrade,
            value1 = 1f, // 회복량
            weight = 50,
            canRepeat = false
        });
        
        // 개별 무기 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "fireball_level_up",
            displayName = "파이어볼 레벨업",
            description = "파이어볼의 데미지와 분열 효과가 강화됩니다.",
            type = UpgradeType.WeaponUpgrade,
            targetId = "Fireball",
            weight = 40,
            canRepeat = true,
            prerequisites = new List<string> { "new_fireball" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "chain_lightning_level_up",
            displayName = "라이트닝 체인 레벨업",
            description = "라이트닝 체인의 연쇄 수와 범위가 증가합니다.",
            type = UpgradeType.WeaponUpgrade,
            targetId = "ChainLightning",
            weight = 40,
            canRepeat = true,
            prerequisites = new List<string> { "new_chain_lightning" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "electric_sphere_level_up",
            displayName = "전기 구체 레벨업",
            description = "전기 구체의 데미지와 전기장 범위가 증가합니다.",
            type = UpgradeType.WeaponUpgrade,
            targetId = "ElectricSphere",
            weight = 40,
            canRepeat = true,
            prerequisites = new List<string> { "new_electric_sphere" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "frost_nova_level_up",
            displayName = "프로스트 노바 레벨업",
            description = "프로스트 노바의 범위와 냉각 효과가 강화됩니다.",
            type = UpgradeType.WeaponUpgrade,
            targetId = "FrostNova",
            weight = 40,
            canRepeat = true,
            prerequisites = new List<string> { "new_frost_nova" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "raining_fire_level_up",
            displayName = "레이닝 파이어 레벨업",
            description = "레이닝 파이어의 낙하 속도와 화염 지대 지속시간이 증가합니다.",
            type = UpgradeType.WeaponUpgrade,
            targetId = "RainingFire",
            weight = 40,
            canRepeat = true,
            prerequisites = new List<string> { "new_raining_fire" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "thunder_level_up",
            displayName = "썬더 레벨업",
            description = "썬더의 데미지와 전기 지대 효과가 강화됩니다.",
            type = UpgradeType.WeaponUpgrade,
            targetId = "Thunder",
            weight = 40,
            canRepeat = true,
            prerequisites = new List<string> { "new_thunder" }
        });
    }
    */
    
    /// <summary>
    /// 레벨업 시 업그레이드 옵션 생성
    /// </summary>
    public List<UpgradeOption> GenerateUpgradeOptions(int currentLevel)
    {
        List<UpgradeOption> availableUpgrades = GetAvailableUpgrades(currentLevel);
        List<UpgradeOption> selectedOptions = new List<UpgradeOption>();
        
        // 가중치 기반 랜덤 선택
        for (int i = 0; i < optionsPerLevelUp && availableUpgrades.Count > 0; i++)
        {
            UpgradeOption selected = SelectWeightedRandom(availableUpgrades);
            selectedOptions.Add(selected);
            
            if (!allowDuplicateOptions)
            {
                availableUpgrades.Remove(selected);
            }
        }
        
        lastOfferedOptions = selectedOptions;
        return selectedOptions;
    }
    
    /// <summary>
    /// 현재 레벨에서 사용 가능한 업그레이드 필터링
    /// </summary>
    private List<UpgradeOption> GetAvailableUpgrades(int currentLevel)
    {
        List<UpgradeOption> available = new List<UpgradeOption>();
        
        foreach (var upgrade in allUpgrades)
        {
            // 레벨 조건 확인
            if (currentLevel < upgrade.minLevel || currentLevel > upgrade.maxLevel)
                continue;
            
            // 반복 가능 여부 확인
            if (!upgrade.canRepeat && HasUpgrade(upgrade.id))
                continue;
            
            // 선행 조건 확인
            if (!CheckPrerequisites(upgrade))
                continue;
            
            // 제외 조건 확인
            if (CheckExclusions(upgrade))
                continue;
            
            available.Add(upgrade);
        }
        
        return available;
    }
    
    /// <summary>
    /// 가중치 기반 랜덤 선택
    /// </summary>
    private UpgradeOption SelectWeightedRandom(List<UpgradeOption> options)
    {
        if (options.Count == 0) return null;
        
        int totalWeight = 0;
        foreach (var option in options)
        {
            totalWeight += option.weight;
        }
        
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var option in options)
        {
            currentWeight += option.weight;
            if (randomValue < currentWeight)
            {
                return option;
            }
        }
        
        return options[options.Count - 1]; // 안전장치
    }
    
    /// <summary>
    /// 업그레이드 적용
    /// </summary>
    public bool ApplyUpgrade(string upgradeId)
    {
        UpgradeOption upgrade = allUpgrades.Find(u => u.id == upgradeId);
        if (upgrade == null)
        {
            Debug.LogError($"UpgradeSystem: 업그레이드 '{upgradeId}'를 찾을 수 없습니다!");
            return false;
        }
        
        // 업그레이드 기록
        RecordUpgrade(upgrade);
        
        // 타입별 업그레이드 적용
        switch (upgrade.type)
        {
            case UpgradeType.WeaponUpgrade:
                ApplyWeaponUpgrade(upgrade);
                break;
            case UpgradeType.NewWeapon:
                ApplyNewWeapon(upgrade);
                break;
            case UpgradeType.PlayerUpgrade:
                ApplyPlayerUpgrade(upgrade);
                break;
            case UpgradeType.SpecialUpgrade:
                ApplySpecialUpgrade(upgrade);
                break;
        }
        
        Debug.Log($"업그레이드 적용: {upgrade.displayName}");
        return true;
    }
    
    /// <summary>
    /// 업그레이드 기록
    /// </summary>
    private void RecordUpgrade(UpgradeOption upgrade)
    {
        if (acquiredUpgrades.ContainsKey(upgrade.id))
        {
            var acquired = acquiredUpgrades[upgrade.id];
            acquired.count++;
            acquired.totalValue1 += upgrade.value1;
            acquired.totalValue2 += upgrade.value2;
            acquired.totalValue3 += upgrade.value3;
        }
        else
        {
            acquiredUpgrades[upgrade.id] = new AcquiredUpgrade
            {
                upgradeId = upgrade.id,
                count = 1,
                levelAcquired = gameManager?.PlayerLevel ?? 1,
                totalValue1 = upgrade.value1,
                totalValue2 = upgrade.value2,
                totalValue3 = upgrade.value3
            };
        }
    }
    
    /// <summary>
    /// 무기 업그레이드 적용
    /// </summary>
    private void ApplyWeaponUpgrade(UpgradeOption upgrade)
    {
        if (weaponManager == null) return;
        
        switch (upgrade.id)
        {
            case "weapon_damage_boost":
                // 모든 장착된 무기의 데미지 증가
                foreach (var weapon in weaponManager.EquippedWeapons)
                {
                    if (weapon != null)
                    {
                        // WeaponBase에 데미지 배율 적용 메서드가 필요
                        ApplyDamageMultiplier(weapon, upgrade.value1);
                    }
                }
                break;
                
            case "weapon_speed_boost":
                // 모든 장착된 무기의 공격 속도 증가
                foreach (var weapon in weaponManager.EquippedWeapons)
                {
                    if (weapon != null)
                    {
                        ApplyCooldownMultiplier(weapon, upgrade.value1);
                    }
                }
                break;
                
            case "fireball_level_up":
                // 파이어볼 레벨업
                weaponManager.LevelUpWeapon("Fireball");
                break;
                
            case "chain_lightning_level_up":
                // 라이트닝 체인 레벨업
                weaponManager.LevelUpWeapon("ChainLightning");
                break;
                
            case "electric_sphere_level_up":
                // 전기 구체 레벨업
                weaponManager.LevelUpWeapon("ElectricSphere");
                break;
                
            case "frost_nova_level_up":
                // 프로스트 노바 레벨업
                weaponManager.LevelUpWeapon("FrostNova");
                break;
                
            case "raining_fire_level_up":
                // 레이닝 파이어 레벨업
                weaponManager.LevelUpWeapon("RainingFire");
                break;
                
            case "thunder_level_up":
                // 썬더 레벨업
                weaponManager.LevelUpWeapon("Thunder");
                break;
        }
    }
    
    /// <summary>
    /// 새 무기 적용
    /// </summary>
    private void ApplyNewWeapon(UpgradeOption upgrade)
    {
        if (weaponManager == null) return;
        
        weaponManager.AddWeapon(upgrade.targetId);
    }
    
    /// <summary>
    /// 플레이어 업그레이드 적용
    /// </summary>
    private void ApplyPlayerUpgrade(UpgradeOption upgrade)
    {
        switch (upgrade.id)
        {
            case "health_boost":
                if (playerHealth != null)
                {
                    playerHealth.IncreaseMaxHealth(upgrade.value1);
                }
                break;
                
            case "movement_speed_boost":
                // PlayerObj의 이동속도 증가 (구현 필요)
                ApplyMovementSpeedBoost(upgrade.value1);
                break;
        }
    }
    
    /// <summary>
    /// 특수 업그레이드 적용
    /// </summary>
    private void ApplySpecialUpgrade(UpgradeOption upgrade)
    {
        switch (upgrade.id)
        {
            case "heal_on_kill":
                // GameManager의 적 처치 이벤트에 회복 로직 연결 (구현 필요)
                SetupHealOnKill(upgrade.value1);
                break;
        }
    }
    
    /// <summary>
    /// 헬퍼 메서드들 (확장 가능)
    /// </summary>
    private void ApplyDamageMultiplier(WeaponBase weapon, float multiplier)
    {
        // WeaponBase에 공개 메서드 필요하거나 리플렉션 사용
        // 임시로 직접 접근 (WeaponBase 수정 필요)
    }
    
    private void ApplyCooldownMultiplier(WeaponBase weapon, float multiplier)
    {
        // WeaponBase에 쿨다운 배율 적용 메서드 필요
    }
    
    private void ApplyMovementSpeedBoost(float multiplier)
    {
        // PlayerObj의 _charMS 값 증가 (PlayerObj 수정 필요)
        PlayerObj playerObj = FindObjectOfType<PlayerObj>();
        if (playerObj != null)
        {
            playerObj._charMS *= multiplier;
        }
    }
    
    private void SetupHealOnKill(float healAmount)
    {
        // GameManager의 적 처치 이벤트에 회복 함수 연결 (구현 필요)
    }
    
    /// <summary>
    /// 유틸리티 메서드들
    /// </summary>
    public bool HasUpgrade(string upgradeId)
    {
        return acquiredUpgrades.ContainsKey(upgradeId);
    }
    
    public int GetUpgradeCount(string upgradeId)
    {
        return HasUpgrade(upgradeId) ? acquiredUpgrades[upgradeId].count : 0;
    }
    
    private bool CheckPrerequisites(UpgradeOption upgrade)
    {
        foreach (string prerequisite in upgrade.prerequisites)
        {
            if (!HasUpgrade(prerequisite))
                return false;
        }
        return true;
    }
    
    private bool CheckExclusions(UpgradeOption upgrade)
    {
        foreach (string exclusion in upgrade.excludes)
        {
            if (HasUpgrade(exclusion))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 외부에서 새 업그레이드 옵션 추가 (모드 지원)
    /// </summary>
    public void AddUpgradeOption(UpgradeOption newUpgrade)
    {
        if (allUpgrades.Find(u => u.id == newUpgrade.id) == null)
        {
            allUpgrades.Add(newUpgrade);
            Debug.Log($"새 업그레이드 옵션 추가: {newUpgrade.displayName}");
        }
    }
    
    /// <summary>
    /// 게임 리셋 시 업그레이드 초기화
    /// </summary>
    public void ResetUpgrades()
    {
        acquiredUpgrades.Clear();
        lastOfferedOptions.Clear();
    }
}