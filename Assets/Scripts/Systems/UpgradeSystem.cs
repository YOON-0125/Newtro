using System.Collections.Generic;
using System.Linq;
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
            Debug.LogWarning("[UpgradeSystem] Inspector에 업그레이드가 설정되지 않았습니다! Inspector에서 All Upgrades 리스트를 채워주세요.");
            Debug.LogWarning("[UpgradeSystem] 모든 값(value1, value2, description 등)은 Inspector에서 설정 가능합니다.");
        }
        else
        {
            Debug.Log("[UpgradeSystem] Inspector에서 설정된 업그레이드 사용");
            Debug.Log("[UpgradeSystem] 모든 value1, value2, description 값들이 Inspector 설정을 우선으로 사용됩니다.");
        }
    }
    
    /// <summary>
    /// 기본 업그레이드들 추가 (Inspector에서 설정하지 않은 경우) - 모든 값은 Inspector에서 설정 권장
    /// </summary>
    private void AddDefaultUpgrades()
    {
        /*
        // 무기 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "WeaponDamageBoost",
            displayName = "Weapon Damage Boost",
            type = UpgradeType.WeaponUpgrade,
            value1 = 1.2f, // 데미지 배율
            canRepeat = true
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "WeaponSpeedBoost",
            displayName = "Attack Speed Boost",
            type = UpgradeType.WeaponUpgrade,
            value1 = 0.85f, // 쿨다운 배율 (85% = 15% 빨라짐)
            canRepeat = true
        });
        
        // 플레이어 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "HealthBoost",
            displayName = "Health Boost",
            type = UpgradeType.PlayerUpgrade,
            value1 = 4f, // 체력 증가량
            canRepeat = true
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "MovementSpeedBoost",
            displayName = "이동 속도 강화",
            type = UpgradeType.PlayerUpgrade,
            targetId = "MovementSpeed",
            value1 = 1.2f, // 이동속도 배율
            canRepeat = true
        });
        
        // 새 무기
        allUpgrades.Add(new UpgradeOption
        {
            id = "NewFireball",
            displayName = "Fireball",
            type = UpgradeType.NewWeapon,
            targetId = "Fireball",
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "NewChainLightning",
            displayName = "Lightning Chain",
            type = UpgradeType.NewWeapon,
            targetId = "ChainLightning",
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "NewElectricSphere",
            displayName = "Electric Sphere",
            type = UpgradeType.NewWeapon,
            targetId = "ElectricSphere",
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "NewFrostNova",
            displayName = "Frost Nova",
            type = UpgradeType.NewWeapon,
            targetId = "FrostNova",
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_raining_fire",
            displayName = "레이닝 파이어 획득",
            type = UpgradeType.NewWeapon,
            targetId = "RainingFire",
            canRepeat = false
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "new_thunder",
            displayName = "썬더 획득",
            type = UpgradeType.NewWeapon,
            targetId = "Thunder",
            canRepeat = false
        });
        
        // 특수 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "heal_on_kill",
            displayName = "처치 시 회복",
            type = UpgradeType.SpecialUpgrade,
            value1 = 1f, // 회복량
            canRepeat = false
        });
        
        // 개별 무기 업그레이드
        allUpgrades.Add(new UpgradeOption
        {
            id = "FireballLevelUp",
            displayName = "Fireball Level Up",
            type = UpgradeType.WeaponUpgrade,
            targetId = "Fireball",
            value1 = 5f, // 데미지 증가량
            value2 = 1f, // 분열 수 증가량
            canRepeat = true,
            prerequisites = new List<string> { "new_fireball" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "ChainLightningLevelUp",
            displayName = "Lightning Chain Level Up",
            type = UpgradeType.WeaponUpgrade,
            targetId = "ChainLightning",
            value1 = 3f, // 데미지 증가량
            value2 = 1f, // 연쇄 수 증가량
            canRepeat = true,
            prerequisites = new List<string> { "NewChainLightning" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "ElectricSphereLevelUp",
            displayName = "Electric Sphere Level Up",
            type = UpgradeType.WeaponUpgrade,
            targetId = "ElectricSphere",
            value1 = 3f, // 데미지 증가량
            value2 = 1.1f, // 초당 틱수 배율 (곱셉연산)
            canRepeat = true,
            prerequisites = new List<string> { "NewElectricSphere" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "FrostNovaLevelUp",
            displayName = "Frost Nova Level Up",
            type = UpgradeType.WeaponUpgrade,
            targetId = "FrostNova",
            value1 = 5f, // 데미지 증가량
            value2 = 1.1f, // 냉각효과 강화 배율 (곱셉연산)
            canRepeat = true,
            prerequisites = new List<string> { "NewFrostNova" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "raining_fire_level_up",
            displayName = "레이닝 파이어 레벨업",
            type = UpgradeType.WeaponUpgrade,
            targetId = "RainingFire",
            value1 = 4f, // 데미지 증가량
            value2 = 1.1f, // 지속시간 증가 배율 (곱셉연산)
            canRepeat = true,
            prerequisites = new List<string> { "new_raining_fire" }
        });
        
        allUpgrades.Add(new UpgradeOption
        {
            id = "thunder_level_up",
            displayName = "썬더 레벨업",
            type = UpgradeType.WeaponUpgrade,
            targetId = "Thunder",
            value1 = 4f, // 데미지 증가량
            value2 = 1.1f, // 전기지대 유지시간 증가 배율 (곱셉연산)
            canRepeat = true,
            prerequisites = new List<string> { "new_thunder" }
        });
        */
        
        Debug.LogWarning("[UpgradeSystem] AddDefaultUpgrades() 메서드가 주석처리되었습니다.");
        Debug.LogWarning("[UpgradeSystem] 모든 업그레이드는 Inspector의 'All Upgrades' 리스트에서 설정해주세요.");
        Debug.LogWarning("[UpgradeSystem] value1, value2, description, weight, minLevel 모두 Inspector에서 조절 가능합니다.");
    }
    
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
    /// Reroll용 새로운 업그레이드 옵션 생성 (이전 옵션들 제외)
    /// </summary>
    public List<UpgradeOption> GenerateNewUpgradeOptions(int currentLevel)
    {
        List<UpgradeOption> availableUpgrades = GetAvailableUpgrades(currentLevel);
        List<UpgradeOption> selectedOptions = new List<UpgradeOption>();
        
        Debug.Log($"[UpgradeSystem] 🎲 리롤 시작 - 사용 가능한 옵션: {availableUpgrades.Count}개");
        
        // 이전에 제공된 옵션들 제외
        if (lastOfferedOptions != null && lastOfferedOptions.Count > 0)
        {
            Debug.Log($"[UpgradeSystem] 🚫 제외할 이전 옵션들: {string.Join(", ", lastOfferedOptions.ConvertAll(o => o.displayName))}");
            
            int beforeCount = availableUpgrades.Count;
            foreach (var lastOption in lastOfferedOptions)
            {
                availableUpgrades.RemoveAll(upgrade => upgrade.id == lastOption.id);
            }
            int afterCount = availableUpgrades.Count;
            
            Debug.Log($"[UpgradeSystem] ✅ 옵션 제외 완료: {beforeCount}개 → {afterCount}개 (제외됨: {beforeCount - afterCount}개)");
        }
        else
        {
            Debug.Log($"[UpgradeSystem] ℹ️ 제외할 이전 옵션이 없음");
        }
        
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
        
        // 옵션이 부족한 경우 경고
        if (selectedOptions.Count < optionsPerLevelUp)
        {
            Debug.LogWarning($"[UpgradeSystem] ⚠️ 리롤 옵션 부족! 요청: {optionsPerLevelUp}개, 생성: {selectedOptions.Count}개");
            
            // 옵션이 너무 부족한 경우 이전 옵션 제외를 무시하고 다시 시도
            if (selectedOptions.Count == 0 && lastOfferedOptions != null && lastOfferedOptions.Count > 0)
            {
                Debug.Log($"[UpgradeSystem] 🔄 옵션이 없어 이전 옵션 제외를 무시하고 재시도");
                return GenerateUpgradeOptions(currentLevel); // 제외 없이 생성
            }
        }
        
        // 새로운 옵션들로 업데이트
        lastOfferedOptions = selectedOptions;
        
        Debug.Log($"[UpgradeSystem] 🎲 리롤 완료 - 새로운 옵션들: {string.Join(", ", selectedOptions.ConvertAll(o => o.displayName))}");
        return selectedOptions;
    }
    
    /// <summary>
    /// 단일 옵션 리롤용 새로운 옵션 생성 (구버전 - 호환성 유지)
    /// </summary>
    public UpgradeOption GenerateSingleNewOption(int currentLevel, string excludeId)
    {
        return GenerateSingleNewOption(currentLevel, excludeId, null);
    }
    
    /// <summary>
    /// 단일 옵션 리롤용 새로운 옵션 생성 (현재 화면 옵션들 제외)
    /// </summary>
    public UpgradeOption GenerateSingleNewOption(int currentLevel, string excludeId, List<UpgradeOption> currentDisplayedOptions)
    {
        List<UpgradeOption> availableUpgrades = GetAvailableUpgrades(currentLevel);
        
        Debug.Log($"[UpgradeSystem] 🎲 개별 리롤 시작 - 사용 가능한 옵션: {availableUpgrades.Count}개");
        
        // 리롤하려는 옵션 제거
        int beforeExclude = availableUpgrades.Count;
        availableUpgrades.RemoveAll(upgrade => upgrade.id == excludeId);
        Debug.Log($"[UpgradeSystem] 🚫 리롤 대상 제외: '{excludeId}' (제거됨: {beforeExclude - availableUpgrades.Count}개)");
        
        // 현재 화면에 표시된 다른 옵션들도 제외
        if (currentDisplayedOptions != null && currentDisplayedOptions.Count > 0)
        {
            beforeExclude = availableUpgrades.Count;
            foreach (var displayedOption in currentDisplayedOptions)
            {
                if (displayedOption.id != excludeId) // 리롤되는 옵션이 아닌 경우만
                {
                    availableUpgrades.RemoveAll(upgrade => upgrade.id == displayedOption.id);
                }
            }
            
            var excludedNames = currentDisplayedOptions
                .Where(o => o.id != excludeId)
                .Select(o => o.displayName)
                .ToArray();
            Debug.Log($"[UpgradeSystem] 🚫 현재 화면 옵션들 제외: [{string.Join(", ", excludedNames)}] (제거됨: {beforeExclude - availableUpgrades.Count}개)");
        }
        else
        {
            Debug.Log($"[UpgradeSystem] ℹ️ 현재 화면 옵션 정보 없음 - lastOfferedOptions 사용");
            
            // 현재 화면 옵션이 제공되지 않은 경우 기존 방식 사용
            if (lastOfferedOptions != null)
            {
                beforeExclude = availableUpgrades.Count;
                foreach (var lastOption in lastOfferedOptions)
                {
                    if (lastOption.id != excludeId)
                    {
                        availableUpgrades.RemoveAll(upgrade => upgrade.id == lastOption.id);
                    }
                }
                Debug.Log($"[UpgradeSystem] 🚫 이전 옵션들 제외 (fallback): (제거됨: {beforeExclude - availableUpgrades.Count}개)");
            }
        }
        
        if (availableUpgrades.Count == 0)
        {
            Debug.LogWarning($"[UpgradeSystem] ⚠️ 개별 리롤 옵션 부족! 제외 조건을 완화하여 재시도");
            
            // 제외 조건을 완화하여 재시도 (리롤 대상만 제외)
            availableUpgrades = GetAvailableUpgrades(currentLevel);
            availableUpgrades.RemoveAll(upgrade => upgrade.id == excludeId);
            
            if (availableUpgrades.Count == 0)
            {
                Debug.LogError($"[UpgradeSystem] ❌ 리롤 불가능! 사용 가능한 옵션이 없습니다!");
                return null;
            }
        }
        
        // 가중치 기반 랜덤 선택
        UpgradeOption selectedOption = SelectWeightedRandom(availableUpgrades);
        
        Debug.Log($"[UpgradeSystem] 🎲 개별 리롤 완료: '{excludeId}' → '{selectedOption?.displayName}'");
        return selectedOption;
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
            
            // 이미 보유한 무기의 "new_weapon" 옵션 제외
            if (upgrade.type == UpgradeType.NewWeapon && weaponManager != null)
            {
                if (weaponManager.HasWeapon(upgrade.targetId))
                {
                    Debug.Log($"[UpgradeSystem] 이미 보유한 무기 제외: {upgrade.targetId}");
                    continue;
                }
            }
            
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
        Debug.Log($"[UpgradeSystem] 🔍 업그레이드 찾기: '{upgradeId}'");
        Debug.Log($"[UpgradeSystem] 전체 업그레이드 개수: {allUpgrades.Count}");
        
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            var u = allUpgrades[i];
            Debug.Log($"[UpgradeSystem] {i}: id='{u.id}', displayName='{u.displayName}', targetId='{u.targetId}'");
        }
        
        UpgradeOption upgrade = allUpgrades.Find(u => u.id == upgradeId);
        
        // ID로 찾지 못했다면 displayName이나 targetId로도 검색
        if (upgrade == null)
        {
            Debug.LogWarning($"[UpgradeSystem] ID로 찾지 못함. displayName으로 검색: '{upgradeId}'");
            upgrade = allUpgrades.Find(u => u.displayName == upgradeId);
        }
        
        if (upgrade == null)
        {
            Debug.LogWarning($"[UpgradeSystem] displayName으로도 찾지 못함. targetId로 검색: '{upgradeId}'");
            upgrade = allUpgrades.Find(u => u.targetId == upgradeId);
        }
        
        // Fireball 특별 처리 - 직접 무기 추가
        if (upgrade == null && upgradeId == "Fireball")
        {
            Debug.Log($"[UpgradeSystem] 🔥 Fireball 특별 처리 - WeaponManager.AddWeapon 직접 호출");
            if (weaponManager != null)
            {
                bool success = weaponManager.AddWeapon("Fireball");
                Debug.Log($"[UpgradeSystem] WeaponManager.AddWeapon('Fireball') 결과: {success}");
                return success;
            }
            else
            {
                Debug.LogError($"[UpgradeSystem] WeaponManager가 null입니다!");
                return false;
            }
        }
        
        if (upgrade == null)
        {
            Debug.LogError($"UpgradeSystem: 업그레이드 '{upgradeId}'를 찾을 수 없습니다!");
            return false;
        }
        
        Debug.Log($"[UpgradeSystem] ✅ 업그레이드 발견: id='{upgrade.id}', type='{upgrade.type}', targetId='{upgrade.targetId}'");
        
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
            case "WeaponDamageBoost":
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
                
            case "WeaponSpeedBoost":
                // 모든 장착된 무기의 공격 속도 증가
                foreach (var weapon in weaponManager.EquippedWeapons)
                {
                    if (weapon != null)
                    {
                        ApplyCooldownMultiplier(weapon, upgrade.value1);
                    }
                }
                break;
                
            case "FireballLevelUp":
                // 파이어볼 레벨업 - value1: splitCount 증가값
                ApplySpecificWeaponUpgrade("Fireball", upgrade);
                break;
                
            case "ChainLightningLevelUp":
                // 라이트닝 체인 레벨업 - value1: chainTargets 증가값, value2: chainRange 배율
                ApplySpecificWeaponUpgrade("ChainLightning", upgrade);
                break;
                
            case "ElectricSphereLevelUp":
                // 전기 구체 레벨업 - value1: radius 증가값, value2: linkRadius 증가값, value3: tickRate 증가값
                ApplySpecificWeaponUpgrade("ElectricSphere", upgrade);
                break;
                
            case "FrostNovaLevelUp":
                // 프로스트 노바 레벨업 - value1: radius 증가값
                ApplySpecificWeaponUpgrade("FrostNova", upgrade);
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
        if (weaponManager == null) 
        {
            Debug.LogError($"[UpgradeSystem] WeaponManager가 null입니다! 새 무기 '{upgrade.targetId}' 추가 실패");
            return;
        }
        
        Debug.Log($"[UpgradeSystem] 🔥 새 무기 추가: '{upgrade.targetId}'");
        bool success = weaponManager.AddWeapon(upgrade.targetId);
        Debug.Log($"[UpgradeSystem] WeaponManager.AddWeapon('{upgrade.targetId}') 결과: {success}");
    }
    
    /// <summary>
    /// 플레이어 업그레이드 적용
    /// </summary>
    private void ApplyPlayerUpgrade(UpgradeOption upgrade)
    {
        Debug.Log($"[UpgradeSystem] 🏥 ApplyPlayerUpgrade 호출: id='{upgrade.id}', value1={upgrade.value1}");
        
        switch (upgrade.id) // ID 통일로 대소문자 구분 제거
        {
            case "HealthBoost":
                if (playerHealth != null)
                {
                    float oldMaxHealth = playerHealth.MaxHealth;
                    float oldCurrentHealth = playerHealth.Health;
                    
                    Debug.Log($"[UpgradeSystem] 하트 증가 + 완전회복 전: MaxHealth={oldMaxHealth}, CurrentHealth={oldCurrentHealth}");
                    
                    // 하트(최대 체력) 증가
                    playerHealth.IncreaseMaxHealth(upgrade.value1);
                    
                    // 체력 100% 완전 회복
                    playerHealth.FullHeal();
                    
                    Debug.Log($"[UpgradeSystem] 하트 증가 + 완전회복 후: MaxHealth={playerHealth.MaxHealth}, CurrentHealth={playerHealth.Health}");
                    Debug.Log($"[UpgradeSystem] 💖 하트 증가 (+{upgrade.value1}포인트) + 체력 100% 회복!");
                    Debug.Log($"[UpgradeSystem] ✅ 체력 업그레이드 적용 완료! (+{upgrade.value1} + Full Heal)");
                }
                else
                {
                    Debug.LogError("[UpgradeSystem] ❌ PlayerHealth가 null입니다!");
                }
                break;
                
            case "MovementSpeedBoost":
                // PlayerObj의 이동속도 증가
                ApplyMovementSpeedBoost(upgrade.value1);
                break;
                
            case "WeaponDamageBoost":
                // 모든 장착된 무기의 데미지 증가 (PlayerUpgrade로도 분류될 수 있음)
                if (weaponManager != null)
                {
                    foreach (var weapon in weaponManager.EquippedWeapons)
                    {
                        if (weapon != null)
                        {
                            ApplyDamageMultiplier(weapon, upgrade.value1);
                        }
                    }
                }
                break;
                
            case "WeaponSpeedBoost":
                // 모든 장착된 무기의 공격 속도 증가 (PlayerUpgrade로도 분류될 수 있음)
                if (weaponManager != null)
                {
                    foreach (var weapon in weaponManager.EquippedWeapons)
                    {
                        if (weapon != null)
                        {
                            ApplyCooldownMultiplier(weapon, upgrade.value1);
                        }
                    }
                }
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
    /// 특정 무기 업그레이드 적용 (value1,2,3 사용)
    /// </summary>
    private void ApplySpecificWeaponUpgrade(string weaponName, UpgradeOption upgrade)
    {
        if (weaponManager == null) return;
        
        // 먼저 기본 레벨업 수행
        bool success = weaponManager.LevelUpWeapon(weaponName);
        if (!success)
        {
            Debug.LogWarning($"[UpgradeSystem] {weaponName} 레벨업 실패");
            return;
        }
        
        // 무기별 추가 업그레이드 적용
        WeaponBase weapon = weaponManager.GetWeapon(weaponName);
        if (weapon == null)
        {
            Debug.LogWarning($"[UpgradeSystem] {weaponName} 무기를 찾을 수 없음");
            return;
        }
        
        Debug.Log($"[UpgradeSystem] 🔧 {weaponName} 커스텀 업그레이드 시작: value1={upgrade.value1}, value2={upgrade.value2}, value3={upgrade.value3}");
        Debug.Log($"[UpgradeSystem] 무기 타입: {weapon.GetType().Name}");
        ApplyCustomWeaponUpgrade(weapon, weaponName, upgrade);
        Debug.Log($"[UpgradeSystem] ✅ {weaponName} 커스텀 업그레이드 완료");
    }
    
    /// <summary>
    /// 무기별 커스텀 업그레이드 적용
    /// </summary>
    private void ApplyCustomWeaponUpgrade(WeaponBase weapon, string weaponName, UpgradeOption upgrade)
    {
        switch (weaponName)
        {
            case "Fireball":
                ApplyFireballUpgrade(weapon, upgrade);
                break;
            case "ChainLightning":
                ApplyChainLightningUpgrade(weapon, upgrade);
                break;
            case "ElectricSphere":
                ApplyElectricSphereUpgrade(weapon, upgrade);
                break;
            case "FrostNova":
                ApplyFrostNovaUpgrade(weapon, upgrade);
                break;
            case "RainingFire":
                ApplyRainingFireUpgrade(weapon, upgrade);
                break;
            case "Thunder":
                ApplyThunderUpgrade(weapon, upgrade);
                break;
            default:
                Debug.LogWarning($"[UpgradeSystem] {weaponName}에 대한 커스텀 업그레이드가 정의되지 않음");
                break;
        }
    }
    
    /// <summary>
    /// 파이어볼 커스텀 업그레이드 (value1: 데미지 증가량, value2: splitCount 증가값)
    /// </summary>
    private void ApplyFireballUpgrade(WeaponBase weapon, UpgradeOption upgrade)
    {
        var fireball = weapon as Fireball;
        if (fireball != null)
        {
            // value1: 데미지 증가량 (합연산)
            if (upgrade.value1 > 0)
            {
                ApplyWeaponFlatBonus(fireball, upgrade.value1);
            }
            
            // value2: 분열 수 증가
            if (upgrade.value2 > 0)
            {
                var splitCountField = typeof(Fireball).GetField("splitCount", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (splitCountField != null)
                {
                    int currentSplitCount = (int)splitCountField.GetValue(fireball);
                    int newSplitCount = currentSplitCount + Mathf.RoundToInt(upgrade.value2);
                    splitCountField.SetValue(fireball, newSplitCount);
                    Debug.Log($"[UpgradeSystem] Fireball splitCount: {currentSplitCount} → {newSplitCount}");
                }
            }
        }
    }
    
    /// <summary>
    /// 체인 라이트닝 커스텀 업그레이드 (value1: 데미지 증가량, value2: chainTargets 증가값)
    /// </summary>
    private void ApplyChainLightningUpgrade(WeaponBase weapon, UpgradeOption upgrade)
    {
        Debug.Log($"[UpgradeSystem] ChainLightning 업그레이드 시작, 무기 타입: {weapon.GetType().Name}");
        
        // ChainWeapon 클래스로 직접 캐스팅
        var chainWeapon = weapon as ChainWeapon;
        Debug.Log($"[UpgradeSystem] ChainWeapon 캐스팅 결과: {(chainWeapon != null ? "성공" : "실패")}");
        
        if (chainWeapon != null)
        {
            // value1: 데미지 증가량 (합연산)
            if (upgrade.value1 > 0)
            {
                ApplyWeaponFlatBonus(chainWeapon, upgrade.value1);
            }
            
            // value2: 연쇄 수 증가
            if (upgrade.value2 > 0)
            {
                var maxTargetsField = typeof(ChainWeapon).GetField("maxChainTargets", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (maxTargetsField != null)
                {
                    int currentTargets = (int)maxTargetsField.GetValue(chainWeapon);
                    int newTargets = currentTargets + Mathf.RoundToInt(upgrade.value2);
                    maxTargetsField.SetValue(chainWeapon, newTargets);
                    Debug.Log($"[UpgradeSystem] ✅ ChainLightning maxChainTargets: {currentTargets} → {newTargets}");
                }
                else
                {
                    Debug.LogWarning($"[UpgradeSystem] ❌ maxChainTargets 업그레이드 실패 - Field not found");
                }
            }
        }
    }
    
    /// <summary>
    /// 일렉트릭 스피어 커스텀 업그레이드 (value1: 데미지 증가량, value2: 초당 틱수 배율)
    /// </summary>
    private void ApplyElectricSphereUpgrade(WeaponBase weapon, UpgradeOption upgrade)
    {
        var electricSphere = weapon as ElectricSphere;
        if (electricSphere != null)
        {
            // value1: 데미지 증가량 (합연산)
            if (upgrade.value1 > 0)
            {
                ApplyWeaponFlatBonus(electricSphere, upgrade.value1);
            }
            
            // value2: 초당 틱수 배율 (곱셈연산)
            if (upgrade.value2 > 0)
            {
                var tickField = typeof(ElectricSphere).GetField("tickPerSec", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (tickField != null)
                {
                    float currentTick = (float)tickField.GetValue(electricSphere);
                    float newTick = currentTick * upgrade.value2; // 곱셈연산
                    tickField.SetValue(electricSphere, newTick);
                    Debug.Log($"[UpgradeSystem] ElectricSphere tickPerSec: {currentTick:F2} → {newTick:F2} (x{upgrade.value2})");
                }
                else
                {
                    Debug.LogWarning($"[UpgradeSystem] ❌ tickPerSec 업그레이드 실패 - Field not found");
                }
            }
        }
    }
    
    /// <summary>
    /// 프로스트 노바 커스텀 업그레이드 (value1: 데미지 증가량, value2: 냉각효과 강화 배율)
    /// </summary>
    private void ApplyFrostNovaUpgrade(WeaponBase weapon, UpgradeOption upgrade)
    {
        var frostNova = weapon as FrostNova;
        if (frostNova != null)
        {
            // value1: 데미지 증가량 (합연산)
            if (upgrade.value1 > 0)
            {
                ApplyWeaponFlatBonus(frostNova, upgrade.value1);
            }
            
            // value2: 냉각효과 강화 배율 (곱셈연산)
            if (upgrade.value2 > 0)
            {
                var statusEffectField = typeof(WeaponBase).GetField("statusEffect", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (statusEffectField != null)
                {
                    StatusEffect statusEffect = (StatusEffect)statusEffectField.GetValue(frostNova);
                    if (statusEffect.magnitude > 0) // struct는 null이 될 수 없으므로 magnitude만 체크
                    {
                        float currentMagnitude = statusEffect.magnitude;
                        float newMagnitude = currentMagnitude * upgrade.value2; // 곱셈연산
                        statusEffect.magnitude = newMagnitude;
                        statusEffectField.SetValue(frostNova, statusEffect); // struct이므로 다시 설정 필요
                        Debug.Log($"[UpgradeSystem] FrostNova 냉각효과 강화: {currentMagnitude:F2} → {newMagnitude:F2} (x{upgrade.value2})");
                    }
                    else
                    {
                        Debug.LogWarning($"[UpgradeSystem] ❌ FrostNova statusEffect magnitude가 0");
                    }
                }
                else
                {
                    Debug.LogWarning($"[UpgradeSystem] ❌ statusEffect 업그레이드 실패 - Field not found");
                }
            }
        }
    }
    
    /// <summary>
    /// 레이닝 파이어 커스텀 업그레이드 (value1: 데미지 증가량, value2: 지속시간 배율)
    /// </summary>
    private void ApplyRainingFireUpgrade(WeaponBase weapon, UpgradeOption upgrade)
    {
        // RainingFire 클래스가 없으므로 FieldWeapon으로 캐스팅
        var fieldWeapon = weapon as FieldWeapon;
        if (fieldWeapon != null)
        {
            // value1: 데미지 증가량 (합연산)
            if (upgrade.value1 > 0)
            {
                ApplyWeaponFlatBonus(fieldWeapon, upgrade.value1);
            }
            
            // value2: 지속시간 배율 (곱셈연산)
            if (upgrade.value2 > 0)
            {
                var durationField = typeof(FieldWeapon).GetField("duration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (durationField != null)
                {
                    float currentDuration = (float)durationField.GetValue(fieldWeapon);
                    float newDuration = currentDuration * upgrade.value2; // 곱셈연산
                    durationField.SetValue(fieldWeapon, newDuration);
                    Debug.Log($"[UpgradeSystem] RainingFire 지속시간: {currentDuration:F2}s → {newDuration:F2}s (x{upgrade.value2})");
                }
                else
                {
                    Debug.LogWarning($"[UpgradeSystem] ❌ RainingFire duration 업그레이드 실패 - Field not found");
                }
            }
        }
    }
    
    /// <summary>
    /// 썬더 커스텀 업그레이드 (value1: 데미지 증가량, value2: 전기지대 유지시간 배율)
    /// </summary>
    private void ApplyThunderUpgrade(WeaponBase weapon, UpgradeOption upgrade)
    {
        // Thunder 클래스가 없으므로 FieldWeapon으로 캐스팅
        var fieldWeapon = weapon as FieldWeapon;
        if (fieldWeapon != null)
        {
            // value1: 데미지 증가량 (합연산)
            if (upgrade.value1 > 0)
            {
                ApplyWeaponFlatBonus(fieldWeapon, upgrade.value1);
            }
            
            // value2: 전기지대 유지시간 배율 (곱셈연산)
            if (upgrade.value2 > 0)
            {
                var fieldDurationField = typeof(FieldWeapon).GetField("fieldDuration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (fieldDurationField != null)
                {
                    float currentFieldDuration = (float)fieldDurationField.GetValue(fieldWeapon);
                    float newFieldDuration = currentFieldDuration * upgrade.value2; // 곱셈연산
                    fieldDurationField.SetValue(fieldWeapon, newFieldDuration);
                    Debug.Log($"[UpgradeSystem] Thunder 전기지대 유지시간: {currentFieldDuration:F2}s → {newFieldDuration:F2}s (x{upgrade.value2})");
                }
                else
                {
                    Debug.LogWarning($"[UpgradeSystem] ❌ Thunder fieldDuration 업그레이드 실패 - Field not found");
                }
            }
        }
    }
    
    /// <summary>
    /// 헬퍼 메서드들 (확장 가능)
    /// </summary>
    private void ApplyDamageMultiplier(WeaponBase weapon, float multiplier)
    {
        // 새로운 시스템: 퍼센트 보너스로 변환 (1.2 → +20%)
        if (weapon != null)
        {
            float percentBonus = multiplier - 1f;
            weapon.AddPercentDamageBonus(percentBonus);
            Debug.Log($"[UpgradeSystem] {weapon.WeaponName} 전역 데미지 증가: +{percentBonus:P1} (배율 {multiplier:F2})");
        }
    }
    
    /// <summary>
    /// 무기별 고정 데미지 보너스 적용 (레벨업)
    /// </summary>
    private void ApplyWeaponFlatBonus(WeaponBase weapon, float flatAmount)
    {
        if (weapon != null)
        {
            weapon.AddFlatDamageBonus(flatAmount);
            Debug.Log($"[UpgradeSystem] {weapon.WeaponName} 고정 데미지 보너스: +{flatAmount}");
        }
    }
    
    private void ApplyCooldownMultiplier(WeaponBase weapon, float multiplier)
    {
        // WeaponBase의 ApplyCooldownMultiplier 메서드 사용
        if (weapon != null)
        {
            weapon.ApplyCooldownMultiplier(multiplier);
            Debug.Log($"[UpgradeSystem] {weapon.WeaponName} 쿨다운 배율 적용: x{multiplier}");
        }
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
            // 무기 선행조건인 경우 WeaponManager에서 실제 무기 보유 확인
            if (prerequisite.StartsWith("New") && weaponManager != null)
            {
                string weaponName = GetWeaponNameFromPrerequisite(prerequisite);
                if (!weaponManager.HasWeapon(weaponName))
                {
                    Debug.Log($"[UpgradeSystem] 무기 선행조건 미충족: {weaponName} 무기 없음");
                    return false;
                }
                Debug.Log($"[UpgradeSystem] 무기 선행조건 충족: {weaponName} 무기 보유 중");
            }
            // 업그레이드 선행조건인 경우 기존 방식
            else if (!HasUpgrade(prerequisite))
            {
                Debug.Log($"[UpgradeSystem] 업그레이드 선행조건 미충족: {prerequisite}");
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 선행조건 ID에서 실제 무기 이름 추출
    /// </summary>
    private string GetWeaponNameFromPrerequisite(string prerequisiteId)
    {
        // "new_chain_lightning" → "ChainLightning"
        // "new_fireball" → "Fireball"
        switch (prerequisiteId)
        {
            case "NewFireball":
                return "Fireball";
            case "NewChainLightning":
                return "ChainLightning";
            case "NewElectricSphere":
                return "ElectricSphere";
            case "NewFrostNova":
                return "FrostNova";
            case "new_raining_fire":
                return "RainingFire";
            case "new_thunder":
                return "Thunder";
            default:
                // 알 수 없는 경우 "New" 제거하고 그대로 사용
                string weaponName = prerequisiteId.Substring(3); // "New" 제거
                return ConvertToPascalCase(weaponName);
        }
    }
    
    /// <summary>
    /// snake_case를 PascalCase로 변환
    /// </summary>
    private string ConvertToPascalCase(string snakeCase)
    {
        string[] parts = snakeCase.Split('_');
        string result = "";
        foreach (string part in parts)
        {
            if (part.Length > 0)
            {
                result += char.ToUpper(part[0]) + part.Substring(1).ToLower();
            }
        }
        return result;
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