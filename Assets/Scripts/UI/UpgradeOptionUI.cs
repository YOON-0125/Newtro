using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 개별 업그레이드 옵션 UI 컴포넌트
/// </summary>
public class UpgradeOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Button optionButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    
    [Header("시각적 효과")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;
    
    [Header("업그레이드 타입별 색상")]
    [SerializeField] private Color weaponUpgradeColor = new Color(1f, 0.5f, 0f, 1f);    // 주황색
    [SerializeField] private Color newWeaponColor = new Color(0.8f, 0.2f, 0.2f, 1f);    // 빨간색
    [SerializeField] private Color playerUpgradeColor = new Color(0.2f, 0.8f, 0.2f, 1f); // 초록색
    [SerializeField] private Color specialUpgradeColor = new Color(0.8f, 0.2f, 0.8f, 1f); // 보라색
    
    // 내부 변수
    private UpgradeOption upgradeOption;
    private LevelUpManager levelUpManager;
    private Vector3 originalScale;
    private bool isInitialized = false;
    private bool isInteractable = true;
    private bool isSelected = false;
    private bool hasRerolled = false;
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (optionButton == null) optionButton = GetComponent<Button>();
        if (rerollButton == null) rerollButton = transform.Find("RerollButton")?.GetComponent<Button>();
        if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText == null) descriptionText = transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (valueText == null) valueText = transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
        if (levelText == null) levelText = transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        if (backgroundImage == null) backgroundImage = transform.Find("Background")?.GetComponent<Image>();
        if (borderImage == null) borderImage = transform.Find("Border")?.GetComponent<Image>();
        
        // 컴포넌트 할당 결과 디버그
        Debug.Log($"[UpgradeOptionUI] 컴포넌트 할당 결과:");
        Debug.Log($"  - nameText: {(nameText != null ? "✅" : "❌")}");
        Debug.Log($"  - descriptionText: {(descriptionText != null ? "✅" : "❌")}");
        Debug.Log($"  - valueText: {(valueText != null ? "✅" : "❌")}");
        Debug.Log($"  - levelText: {(levelText != null ? "✅" : "❌")}");
        
        // 원본 스케일 저장
        originalScale = transform.localScale;
        
        // 버튼 이벤트 설정
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnButtonClick);
        }
        
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollButtonClick);
        }
    }
    
    /// <summary>
    /// 업그레이드 옵션으로 UI 초기화
    /// </summary>
    public void Initialize(UpgradeOption option, LevelUpManager manager)
    {
        upgradeOption = option;
        levelUpManager = manager;
        
        UpdateUI();
        SetTypeBasedVisuals();
        SetupRerollButton();
        
        isInitialized = true;
        isInteractable = true;
        hasRerolled = false;
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (upgradeOption == null) return;
        
        // 아이콘 설정
        if (iconImage != null)
        {
            if (upgradeOption.icon != null)
            {
                iconImage.sprite = upgradeOption.icon;
                iconImage.color = Color.white;
            }
            else
            {
                // 기본 아이콘 또는 타입별 아이콘
                iconImage.sprite = GetDefaultIcon(upgradeOption.type);
                iconImage.color = GetTypeColor(upgradeOption.type);
            }
        }
        
        // 텍스트 설정
        if (nameText != null)
        {
            nameText.text = upgradeOption.displayName;
            Debug.Log($"[UpgradeOptionUI] ✅ NameText 설정: '{upgradeOption.displayName}', 활성화: {nameText.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[UpgradeOptionUI] ❌ nameText가 null입니다!");
        }
        
        if (descriptionText != null)
        {
            // Inspector에서 설정한 description 우선 사용
            string finalDescription = string.IsNullOrEmpty(upgradeOption.description) ? 
                "Description not set in Inspector" : upgradeOption.description;
            descriptionText.text = finalDescription;
            
            // 폰트 크기도 확인 가능하도록 설정
            if (descriptionText.fontSize < 10f)
                descriptionText.fontSize = 12f;
            
            Debug.Log($"[UpgradeOptionUI] Description 설정 (Inspector 우선): '{finalDescription}', 크기: {descriptionText.fontSize}");
        }
        else
        {
            Debug.LogWarning("[UpgradeOptionUI] descriptionText가 null입니다!");
        }
        
        // 값 텍스트 (업그레이드 효과 표시)
        if (valueText != null)
        {
            string valueTextContent = GetValueText();
            valueText.text = valueTextContent;
            Debug.Log($"[UpgradeOptionUI] ✅ ValueText 설정: '{valueTextContent}', 활성화: {valueText.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[UpgradeOptionUI] ❌ valueText가 null입니다!");
        }
        
        // 레벨 텍스트 (현재 레벨 표시)
        if (levelText != null)
        {
            string levelTextContent = GetLevelText();
            levelText.text = levelTextContent;
            Debug.Log($"[UpgradeOptionUI] ✅ LevelText 설정: '{levelTextContent}', 활성화: {levelText.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[UpgradeOptionUI] ❌ levelText가 null입니다!");
        }
    }
    
    /// <summary>
    /// 타입 기반 시각적 효과 설정
    /// </summary>
    private void SetTypeBasedVisuals()
    {
        Color typeColor = GetTypeColor(upgradeOption.type);
        
        // 테두리 색상 설정
        if (borderImage != null)
        {
            borderImage.color = typeColor;
        }
        
        // 배경 색상 (약간 투명하게)
        if (backgroundImage != null)
        {
            Color bgColor = typeColor;
            bgColor.a = 0.2f;
            backgroundImage.color = bgColor;
        }
    }
    
    /// <summary>
    /// 타입별 색상 반환
    /// </summary>
    private Color GetTypeColor(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.WeaponUpgrade: return weaponUpgradeColor;
            case UpgradeType.NewWeapon: return newWeaponColor;
            case UpgradeType.PlayerUpgrade: return playerUpgradeColor;
            case UpgradeType.SpecialUpgrade: return specialUpgradeColor;
            default: return normalColor;
        }
    }
    
    /// <summary>
    /// 기본 아이콘 반환 (타입별)
    /// </summary>
    private Sprite GetDefaultIcon(UpgradeType type)
    {
        // 기본 아이콘들이 있다면 타입별로 반환
        // 없다면 null 반환하여 색상으로 구분
        return null;
    }
    
    /// <summary>
    /// 한글 description을 영어로 변환 (하드코딩 제거됨 - Inspector 사용)
    /// </summary>
    private string GetEnglishDescription(string upgradeId, string originalDescription)
    {
        /*
        // 하드코딩된 description들 - 이제 Inspector에서 설정
        switch (upgradeId)
        {
            case "WeaponDamageBoost":
                return "Increases all weapon damage by 20%.";
            case "WeaponSpeedBoost":
                return "Increases all weapon attack speed by 15%.";
            case "HealthBoost":
                return "Increases max health by 1 heart (4HP).";
            case "MovementSpeedBoost":
                return "Increases movement speed by 20%.";
            case "NewFireball":
                return "Gain a fireball weapon that shoots fire projectiles.";
            case "NewChainLightning":
                return "Gain a lightning weapon that chains between enemies.";
            case "NewElectricSphere":
                return "Gain an electric sphere that deals electric damage.";
            case "NewFrostNova":
                return "Gain a frost nova that creates ice explosions.";
            case "FireballLevelUp":
                return "Enhances fireball damage and split effects.";
            case "ChainLightningLevelUp":
                return "Increases lightning chain count and range.";
            case "ElectricSphereLevelUp":
                return "Increases electric sphere damage and range.";
            case "FrostNovaLevelUp":
                return "Enhances frost nova range and freeze effects.";
            default:
                // 한글이 포함되어 있으면 기본 영어 텍스트 반환
                if (ContainsKorean(originalDescription))
                    return "Upgrade effect description.";
                return originalDescription;
        }
        */
        
        // 이제 Inspector의 description을 직접 사용
        return string.IsNullOrEmpty(originalDescription) ? 
            "Description not set in Inspector" : originalDescription;
    }
    
    /// <summary>
    /// 한글 포함 여부 체크
    /// </summary>
    private bool ContainsKorean(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        foreach (char c in text)
        {
            // 한글 유니코드 범위: AC00-D7A3
            if (c >= 0xAC00 && c <= 0xD7A3)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 값 텍스트 생성
    /// </summary>
    private string GetValueText()
    {
        if (upgradeOption == null) return "";
        
        switch (upgradeOption.type)
        {
            case UpgradeType.WeaponUpgrade:
                return GetWeaponUpgradeValueText();
            case UpgradeType.PlayerUpgrade:
                return GetPlayerUpgradeValueText();
            case UpgradeType.NewWeapon:
                return "New Weapon";
            case UpgradeType.SpecialUpgrade:
                return "Special Effect";
            default:
                return "";
        }
    }
    
    /// <summary>
    /// 무기 업그레이드 값 텍스트 (Inspector value1, value2 사용)
    /// </summary>
    private string GetWeaponUpgradeValueText()
    {
        // Inspector에서 설정한 value1, value2 값을 표시
        if (upgradeOption.value1 != 0 && upgradeOption.value2 != 0)
        {
            return $"DMG+{upgradeOption.value1:F0} / Effect×{upgradeOption.value2:F1}";
        }
        else if (upgradeOption.value1 != 0)
        {
            return $"DMG+{upgradeOption.value1:F0}";
        }
        else if (upgradeOption.value2 != 0)
        {
            return $"Effect×{upgradeOption.value2:F1}";
        }
        
        /*
        // 하드코딩된 값들 - 이제 Inspector value1, value2 사용
        switch (upgradeOption.id)
        {
            case "WeaponDamageBoost":
                float damageIncrease = (upgradeOption.value1 - 1f) * 100f;
                return $"+{damageIncrease:F0}% Damage";
            case "WeaponSpeedBoost":
                float speedIncrease = (1f - upgradeOption.value1) * 100f;
                return $"+{speedIncrease:F0}% Speed";
            default:
                return "";
        }
        */
        
        return ""; // 기본값
    }
    
    /// <summary>
    /// 플레이어 업그레이드 값 텍스트 (Inspector value1, value2 사용)
    /// </summary>
    private string GetPlayerUpgradeValueText()
    {
        // 특정 업그레이드들은 의미있는 표시로 변환
        switch (upgradeOption.id)
        {
            case "HealthBoost":
                int hearts = Mathf.FloorToInt(upgradeOption.value1 / 4f);
                return $"+{hearts} Hearts";
            case "MovementSpeedBoost":
                float speedIncrease = (upgradeOption.value1 - 1f) * 100f;
                return $"+{speedIncrease:F0}% Move Speed";
            case "WeaponDamageBoost":
                float damageIncrease = (upgradeOption.value1 - 1f) * 100f;
                return $"+{damageIncrease:F0}% Damage";
            case "WeaponSpeedBoost":
                float speedBoost = (1f - upgradeOption.value1) * 100f;
                return $"+{speedBoost:F0}% Attack Speed";
            default:
                // 기타 업그레이드들은 Inspector 값 직접 표시
                if (upgradeOption.value1 != 0 && upgradeOption.value2 != 0)
                {
                    return $"Value1: {upgradeOption.value1:F1} / Value2: {upgradeOption.value2:F1}";
                }
                else if (upgradeOption.value1 != 0)
                {
                    return $"Value: {upgradeOption.value1:F1}";
                }
                else if (upgradeOption.value2 != 0)
                {
                    return $"Value: {upgradeOption.value2:F1}";
                }
                return "";
        }
    }
    
    /// <summary>
    /// 레벨 텍스트 생성
    /// </summary>
    private string GetLevelText()
    {
        if (upgradeOption == null) return "";
        
        // 현재 레벨 가져오기
        int currentLevel = GetCurrentUpgradeLevel();
        
        // 타입별로 다른 표시 방식
        switch (upgradeOption.type)
        {
            case UpgradeType.NewWeapon:
                // 새 무기는 "습득" 또는 레벨 표시하지 않음
                return "습득";
                
            case UpgradeType.WeaponUpgrade:
                // 무기 업그레이드는 현재 무기 레벨 표시
                if (currentLevel == 0)
                    return "미습득";
                else
                    return $"레벨 {currentLevel}";
                    
            case UpgradeType.PlayerUpgrade:
            case UpgradeType.SpecialUpgrade:
                // 스탯 업그레이드는 현재 스탯 레벨 표시
                return $"레벨 {currentLevel}";
                
            default:
                return $"레벨 {currentLevel}";
        }
    }
    
    /// <summary>
    /// 현재 업그레이드 레벨 가져오기
    /// </summary>
    private int GetCurrentUpgradeLevel()
    {
        if (upgradeOption == null || levelUpManager == null) return 0;
        
        // 업그레이드 ID에 따라 현재 레벨 확인
        // 이 부분은 게임의 업그레이드 시스템에 따라 구현이 달라질 수 있습니다
        
        switch (upgradeOption.id)
        {
            // 플레이어 스탯 업그레이드들
            case "MovementSpeedBoost":
                return GetPlayerStatLevel("MovementSpeed");
            case "HealthBoost":
                return GetPlayerStatLevel("Health");
            case "WeaponDamageBoost":
                return GetPlayerStatLevel("WeaponDamage");
            case "WeaponSpeedBoost":
                return GetPlayerStatLevel("WeaponSpeed");
                
            // 무기 레벨업들
            case "FireballLevelUp":
                return GetWeaponLevel("Fireball");
            case "ChainLightningLevelUp":
                return GetWeaponLevel("ChainLightning");
            case "ElectricSphereLevelUp":
                return GetWeaponLevel("ElectricSphere");
            case "FrostNovaLevelUp":
                return GetWeaponLevel("FrostNova");
                
            // 새 무기들은 항상 0 (습득 전)
            case "NewFireball":
            case "NewChainLightning":
            case "NewElectricSphere":
            case "NewFrostNova":
                return 0;
                
            default:
                return 0;
        }
    }
    
    /// <summary>
    /// 플레이어 스탯 레벨 가져오기 (UpgradeSystem 연동)
    /// </summary>
    private int GetPlayerStatLevel(string statName)
    {
        if (levelUpManager == null) return 0;
        
        // GameManager를 통해 UpgradeSystem 접근
        GameManager gameManager = GameManager.Instance;
        if (gameManager?.UpgradeSystem == null) return 0;
        
        // 스탯별 업그레이드 ID 매핑
        string upgradeId = GetStatUpgradeId(statName);
        if (string.IsNullOrEmpty(upgradeId)) return 0;
        
        // UpgradeSystem에서 해당 업그레이드 획득 횟수 반환
        return gameManager.UpgradeSystem.GetUpgradeCount(upgradeId);
    }
    
    /// <summary>
    /// 스탯 이름을 업그레이드 ID로 변환
    /// </summary>
    private string GetStatUpgradeId(string statName)
    {
        switch (statName)
        {
            case "MovementSpeed": return "MovementSpeedBoost";
            case "Health": return "HealthBoost";
            case "WeaponDamage": return "WeaponDamageBoost";
            case "WeaponSpeed": return "WeaponSpeedBoost";
            default: return "";
        }
    }
    
    /// <summary>
    /// 무기 레벨 가져오기 (WeaponManager + UpgradeSystem 연동)
    /// </summary>
    private int GetWeaponLevel(string weaponName)
    {
        if (levelUpManager == null) return 0;
        
        // GameManager를 통해 WeaponManager와 UpgradeSystem 접근
        GameManager gameManager = GameManager.Instance;
        if (gameManager?.WeaponManager == null || gameManager?.UpgradeSystem == null) return 0;
        
        WeaponManager weaponManager = gameManager.WeaponManager;
        UpgradeSystem upgradeSystem = gameManager.UpgradeSystem;
        
        // 1. 무기를 보유하고 있는지 확인
        if (!weaponManager.HasWeapon(weaponName))
        {
            return 0; // 무기 미보유
        }
        
        // 2. 무기 기본 레벨(1) + 레벨업 업그레이드 횟수
        string levelUpId = GetWeaponLevelUpId(weaponName);
        if (string.IsNullOrEmpty(levelUpId))
        {
            // 레벨업 ID를 찾을 수 없으면 기본 무기 레벨만 반환
            WeaponBase weapon = weaponManager.GetWeapon(weaponName);
            return weapon?.Level ?? 1;
        }
        
        // 3. 기본 레벨(1) + 업그레이드 획득 횟수
        int upgradeCount = upgradeSystem.GetUpgradeCount(levelUpId);
        return 1 + upgradeCount;
    }
    
    /// <summary>
    /// 무기 이름을 레벨업 업그레이드 ID로 변환
    /// </summary>
    private string GetWeaponLevelUpId(string weaponName)
    {
        switch (weaponName)
        {
            case "Fireball": return "FireballLevelUp";
            case "ChainLightning": return "ChainLightningLevelUp";
            case "ElectricSphere": return "ElectricSphereLevelUp";
            case "FrostNova": return "FrostNovaLevelUp";
            case "RainingFire": return "raining_fire_level_up";
            case "Thunder": return "thunder_level_up";
            default: return "";
        }
    }
    
    /// <summary>
    /// 마우스 진입 이벤트
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        // 호버 효과
        StartCoroutine(AnimateScale(originalScale * hoverScale));
        
        // 색상 변경
        if (borderImage != null)
        {
            borderImage.color = hoverColor;
        }
        
        // 사운드 재생
        if (levelUpManager != null)
        {
            levelUpManager.PlayOptionHoverSound();
        }
    }
    
    /// <summary>
    /// 마우스 이탈 이벤트
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        // 원본 크기로 복원
        StartCoroutine(AnimateScale(originalScale));
        
        // 선택된 상태가 아닐 때만 원본 색상으로 복원
        if (!isSelected && borderImage != null)
        {
            borderImage.color = GetTypeColor(upgradeOption.type);
        }
        
        // 선택된 상태면 선택 색상 유지
        if (isSelected)
        {
            ApplySelectedVisuals();
        }
    }
    
    /// <summary>
    /// 클릭 이벤트
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        OnButtonClick();
    }
    
    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void OnButtonClick()
    {
        if (!isInitialized || !isInteractable || upgradeOption == null) return;
        
        // 선택 상태로 변경 (바로 적용하지 않음)
        SetSelected(true);
        
        // 레벨업 매니저에 선택 알림 (적용하지 않고 선택만)
        if (levelUpManager != null)
        {
            levelUpManager.OnOptionSelected(this);
        }
    }
    
    /// <summary>
    /// 스케일 애니메이션
    /// </summary>
    private System.Collections.IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    
    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selected)
        {
            // 선택된 상태의 시각적 효과 - 지속적으로 유지
            ApplySelectedVisuals();
            // 펄스 애니메이션은 별도로
            StartCoroutine(PlaySelectionPulse());
        }
        else
        {
            // 선택 해제 상태로 복원
            ResetVisualState();
        }
    }
    
    /// <summary>
    /// 선택된 상태의 지속적인 시각적 효과 적용
    /// </summary>
    private void ApplySelectedVisuals()
    {
        // 선택 색상을 지속적으로 유지
        if (borderImage != null)
        {
            borderImage.color = selectedColor;
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = selectedColor;
            bgColor.a = 0.3f;
            backgroundImage.color = bgColor;
        }
    }
    
    /// <summary>
    /// 선택 시 펄스 애니메이션만 재생
    /// </summary>
    private System.Collections.IEnumerator PlaySelectionPulse()
    {
        Vector3 pulseScale = originalScale * 1.2f;
        yield return StartCoroutine(AnimateScale(pulseScale));
        yield return StartCoroutine(AnimateScale(originalScale));
        
        // 펄스 후에도 선택 색상 유지
        if (isSelected)
        {
            ApplySelectedVisuals();
        }
    }
    
    /// <summary>
    /// 시각적 상태 초기화
    /// </summary>
    private void ResetVisualState()
    {
        isInteractable = true;
        
        // 원본 색상으로 복원
        if (borderImage != null)
        {
            borderImage.color = GetTypeColor(upgradeOption.type);
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = GetTypeColor(upgradeOption.type);
            bgColor.a = 0.2f;
            backgroundImage.color = bgColor;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 선택된 옵션 반환
    /// </summary>
    public UpgradeOption GetUpgradeOption()
    {
        return upgradeOption;
    }
    
    /// <summary>
    /// 선택 상태 확인
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// 리롤 버튼 설정
    /// </summary>
    private void SetupRerollButton()
    {
        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = !hasRerolled;
            
            // 리롤 버튼 텍스트 설정
            TextMeshProUGUI rerollText = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            if (rerollText != null)
            {
                rerollText.text = hasRerolled ? "Used" : "Reroll";
            }
        }
    }
    
    /// <summary>
    /// 리롤 버튼 클릭 처리
    /// </summary>
    private void OnRerollButtonClick()
    {
        if (!isInitialized || hasRerolled || levelUpManager == null) 
        {
            Debug.Log($"[UpgradeOptionUI] 리롤 불가: initialized={isInitialized}, hasRerolled={hasRerolled}");
            return;
        }
        
        Debug.Log($"[UpgradeOptionUI] 🎲 개별 리롤 실행: {upgradeOption?.displayName}");
        
        // 리롤 상태 설정
        hasRerolled = true;
        
        // 레벨업 매니저에 개별 리롤 요청
        if (levelUpManager != null)
        {
            levelUpManager.RerollSingleOption(this);
        }
        
        // 리롤 버튼 상태 업데이트
        SetupRerollButton();
    }
    
    /// <summary>
    /// 새로운 옵션으로 교체
    /// </summary>
    public void ReplaceWithNewOption(UpgradeOption newOption)
    {
        upgradeOption = newOption;
        UpdateUI();
        SetTypeBasedVisuals();
        
        Debug.Log($"[UpgradeOptionUI] ✅ 옵션 교체됨: {newOption.displayName}");
    }
    
    private void OnDestroy()
    {
        // 이벤트 정리
        if (optionButton != null)
        {
            optionButton.onClick.RemoveAllListeners();
        }
        
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
        }
    }
}