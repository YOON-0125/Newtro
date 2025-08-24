using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// Boss reward option UI component (Trophy, Power, Awakening)
/// </summary>
public class BossRewardOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Button optionButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI slotTypeText; // Trophy/Power/Awakening display
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    
    [Header("시각적 효과")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;
    
    [Header("보상 타입별 색상")]
    [SerializeField] private Color trophyColor = new Color(1f, 0.8f, 0f, 1f);    // Gold (Trophy)
    [SerializeField] private Color powerColor = new Color(0.6f, 0.2f, 0.8f, 1f);    // Purple (Power)
    [SerializeField] private Color awakeningColor = new Color(1f, 0.2f, 0.2f, 1f); // Red (Awakening)
    
    // 내부 변수
    private BossRewardOption rewardOption;
    private BossRewardManager rewardManager;
    private Vector3 originalScale;
    private bool isInitialized = false;
    private bool isInteractable = true;
    private bool isSelected = false;
    private bool hasRerolled = false;
    
    public event Action<BossRewardOptionUI> OnOptionSelectedCallback;

    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (optionButton == null) optionButton = GetComponent<Button>();
        if (rerollButton == null) rerollButton = transform.Find("RerollButton")?.GetComponent<Button>();
        if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText == null) descriptionText = transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (valueText == null) valueText = transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
        if (slotTypeText == null) slotTypeText = transform.Find("SlotTypeText")?.GetComponent<TextMeshProUGUI>();
        if (backgroundImage == null) backgroundImage = transform.Find("Background")?.GetComponent<Image>();
        if (borderImage == null) borderImage = transform.Find("Border")?.GetComponent<Image>();
        
        // 컴포넌트 할당 결과 디버그
        Debug.Log($"[BossRewardOptionUI] 컴포넌트 할당 결과:");
        Debug.Log($"  - nameText: {(nameText != null ? "✅" : "❌")}");
        Debug.Log($"  - descriptionText: {(descriptionText != null ? "✅" : "❌")}");
        Debug.Log($"  - slotTypeText: {(slotTypeText != null ? "✅" : "❌")}");
        
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
    /// 보스 보상 옵션으로 UI 초기화
    /// </summary>
    public void Initialize(BossRewardOption option, BossRewardManager manager)
    {
        rewardOption = option;
        rewardManager = manager;
        
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
        if (rewardOption == null) return;
        
        // Slot type text setup (Trophy/Power/Awakening)
        if (slotTypeText != null)
        {
            slotTypeText.text = GetSlotTypeName(rewardOption.slotType);
            slotTypeText.color = GetSlotTypeColor(rewardOption.slotType);
        }
        
        // 아이콘 설정
        if (iconImage != null)
        {
            if (rewardOption.icon != null)
            {
                iconImage.sprite = rewardOption.icon;
                iconImage.color = Color.white;
            }
            else
            {
                // 기본 아이콘 또는 타입별 아이콘
                iconImage.sprite = GetDefaultIcon(rewardOption.slotType);
                iconImage.color = GetSlotTypeColor(rewardOption.slotType);
            }
        }
        
        // 텍스트 설정
        if (nameText != null)
        {
            nameText.text = rewardOption.displayName;
            Debug.Log($"[BossRewardOptionUI] ✅ NameText 설정: '{rewardOption.displayName}'");
        }
        
        if (descriptionText != null)
        {
            string finalDescription = string.IsNullOrEmpty(rewardOption.description) ? 
                "Description not set" : rewardOption.description;
            descriptionText.text = finalDescription;
            
            if (descriptionText.fontSize < 10f)
                descriptionText.fontSize = 12f;
            
            Debug.Log($"[BossRewardOptionUI] Description 설정: '{finalDescription}'");
        }
        
        // 값 텍스트 (보상 효과 표시)
        if (valueText != null)
        {
            string valueTextContent = GetValueText();
            valueText.text = valueTextContent;
            Debug.Log($"[BossRewardOptionUI] ✅ ValueText 설정: '{valueTextContent}'");
        }
    }
    
    /// <summary>
    /// 슬롯 타입 이름 반환
    /// </summary>
    private string GetSlotTypeName(BossRewardSlotType slotType)
    {
        switch (slotType)
        {
            case BossRewardSlotType.Trophy: return "Trophy";
            case BossRewardSlotType.Power: return "Power";
            case BossRewardSlotType.Awakening: return "Awakening";
            default: return "Reward";
        }
    }
    
    /// <summary>
    /// 타입 기반 시각적 효과 설정
    /// </summary>
    private void SetTypeBasedVisuals()
    {
        Color typeColor = GetSlotTypeColor(rewardOption.slotType);
        
        // 테두리 색상 설정 (두껍고 빛나는 효과)
        if (borderImage != null)
        {
            borderImage.color = typeColor;
            // 보스 보상은 더 두꺼운 테두리 효과
            Outline outline = borderImage.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = true;
            }
        }
        
        // 배경 색상 (약간 투명하게)
        if (backgroundImage != null)
        {
            Color bgColor = typeColor;
            bgColor.a = 0.25f; // 보스 보상은 좀 더 진하게
            backgroundImage.color = bgColor;
        }
    }
    
    /// <summary>
    /// 슬롯 타입별 색상 반환
    /// </summary>
    private Color GetSlotTypeColor(BossRewardSlotType slotType)
    {
        switch (slotType)
        {
            case BossRewardSlotType.Trophy: return trophyColor;
            case BossRewardSlotType.Power: return powerColor;
            case BossRewardSlotType.Awakening: return awakeningColor;
            default: return normalColor;
        }
    }
    
    /// <summary>
    /// 기본 아이콘 반환 (슬롯 타입별)
    /// </summary>
    private Sprite GetDefaultIcon(BossRewardSlotType slotType)
    {
        // 기본 아이콘들이 있다면 슬롯 타입별로 반환
        return null;
    }
    
    /// <summary>
    /// 값 텍스트 생성
    /// </summary>
    private string GetValueText()
    {
        if (rewardOption == null) return "";
        
        switch (rewardOption.slotType)
        {
            case BossRewardSlotType.Trophy:
                return GetTrophyValueText();
            case BossRewardSlotType.Power:
                return GetPowerValueText();
            case BossRewardSlotType.Awakening:
                return GetAwakeningValueText();
            default:
                return "";
        }
    }
    
    /// <summary>
    /// Trophy value text
    /// </summary>
    private string GetTrophyValueText()
    {
        if (rewardOption.value1 != 0 && rewardOption.value2 != 0)
        {
            return $"Power+{rewardOption.value1:F0} / Effect×{rewardOption.value2:F1}";
        }
        else if (rewardOption.value1 != 0)
        {
            return $"Power+{rewardOption.value1:F0}";
        }
        else if (rewardOption.value2 != 0)
        {
            return $"Effect×{rewardOption.value2:F1}";
        }
        return "Special Power";
    }
    
    /// <summary>
    /// Power value text
    /// </summary>
    private string GetPowerValueText()
    {
        return "New Ability";
    }
    
    /// <summary>
    /// Awakening value text
    /// </summary>
    private string GetAwakeningValueText()
    {
        if (rewardOption.value1 != 0)
        {
            float percentage = (rewardOption.value1 - 1f) * 100f;
            return $"+{percentage:F0}% Enhanced";
        }
        return "Enhanced Effect";
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
        if (rewardManager != null)
        {
            rewardManager.PlayOptionHoverSound();
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
            borderImage.color = GetSlotTypeColor(rewardOption.slotType);
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
    /// 버튼 클릭 처리 - 선택만 (확인 버튼 방식)
    /// </summary>
    private void OnButtonClick()
    {
        if (!isInitialized || !isInteractable || rewardOption == null) return;
        
        // 선택 상태로 변경 (바로 적용하지 않음)
        SetSelected(true);
        
        Debug.Log($"[BossRewardOptionUI] 보상 옵션 선택됨: {rewardOption.displayName}");
        
        // 보상 매니저에 선택 알림 (적용하지 않고 선택만)
        if (rewardManager != null)
        {
            rewardManager.OnOptionSelected(this);
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
            ApplySelectedVisuals();
            StartCoroutine(PlaySelectionPulse());
        }
        else
        {
            ResetVisualState();
        }
    }
    
    /// <summary>
    /// 선택된 상태의 시각적 효과 적용
    /// </summary>
    private void ApplySelectedVisuals()
    {
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
    /// 선택 시 펄스 애니메이션
    /// </summary>
    private System.Collections.IEnumerator PlaySelectionPulse()
    {
        Vector3 pulseScale = originalScale * 1.2f;
        yield return StartCoroutine(AnimateScale(pulseScale));
        yield return StartCoroutine(AnimateScale(originalScale));
        
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
        
        if (borderImage != null)
        {
            borderImage.color = GetSlotTypeColor(rewardOption.slotType);
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = GetSlotTypeColor(rewardOption.slotType);
            bgColor.a = 0.25f;
            backgroundImage.color = bgColor;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 선택된 보상 옵션 반환
    /// </summary>
    public BossRewardOption GetRewardOption()
    {
        return rewardOption;
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
        if (!isInitialized || hasRerolled || rewardManager == null) 
        {
            Debug.Log($"[BossRewardOptionUI] 리롤 불가: initialized={isInitialized}, hasRerolled={hasRerolled}");
            return;
        }
        
        Debug.Log($"[BossRewardOptionUI] 🎲 보상 리롤 실행: {rewardOption?.displayName}");
        
        hasRerolled = true;
        
        if (rewardManager != null)
        {
            rewardManager.RerollSingleOption(this);
        }
        
        SetupRerollButton();
    }
    
    /// <summary>
    /// 새로운 보상으로 교체
    /// </summary>
    public void ReplaceWithNewOption(BossRewardOption newOption)
    {
        rewardOption = newOption;
        UpdateUI();
        SetTypeBasedVisuals();
        
        Debug.Log($"[BossRewardOptionUI] ✅ 보상 교체됨: {newOption.displayName}");
    }
    
    private void OnDestroy()
    {
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

// 보스 보상 슬롯 타입
public enum BossRewardSlotType
{
    Trophy,     // Trophy
    Power,      // Power  
    Awakening   // Awakening
}

// 보스 보상 옵션 데이터 구조체
[System.Serializable]
public class BossRewardOption
{
    public string id;
    public string displayName;
    public string description;
    public BossRewardSlotType slotType;
    public Sprite icon;
    public float value1;
    public float value2;
    public RelicBase relic;
}