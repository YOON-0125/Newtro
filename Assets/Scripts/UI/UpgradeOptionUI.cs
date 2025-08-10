using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 개별 업그레이드 옵션 UI 컴포넌트
/// </summary>
public class UpgradeOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Button optionButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text valueText;
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
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (optionButton == null) optionButton = GetComponent<Button>();
        if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<Text>();
        if (descriptionText == null) descriptionText = transform.Find("DescriptionText")?.GetComponent<Text>();
        if (valueText == null) valueText = transform.Find("ValueText")?.GetComponent<Text>();
        if (backgroundImage == null) backgroundImage = transform.Find("Background")?.GetComponent<Image>();
        if (borderImage == null) borderImage = transform.Find("Border")?.GetComponent<Image>();
        
        // 원본 스케일 저장
        originalScale = transform.localScale;
        
        // 버튼 이벤트 설정
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnButtonClick);
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
        
        isInitialized = true;
        isInteractable = true;
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
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = upgradeOption.description;
        }
        
        // 값 텍스트 (업그레이드 효과 표시)
        if (valueText != null)
        {
            valueText.text = GetValueText();
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
                return "새 무기";
            case UpgradeType.SpecialUpgrade:
                return "특수 효과";
            default:
                return "";
        }
    }
    
    /// <summary>
    /// 무기 업그레이드 값 텍스트
    /// </summary>
    private string GetWeaponUpgradeValueText()
    {
        switch (upgradeOption.id)
        {
            case "weapon_damage_boost":
                float damageIncrease = (upgradeOption.value1 - 1f) * 100f;
                return $"+{damageIncrease:F0}% 데미지";
            case "weapon_speed_boost":
                float speedIncrease = (1f - upgradeOption.value1) * 100f;
                return $"+{speedIncrease:F0}% 속도";
            default:
                return "";
        }
    }
    
    /// <summary>
    /// 플레이어 업그레이드 값 텍스트
    /// </summary>
    private string GetPlayerUpgradeValueText()
    {
        switch (upgradeOption.id)
        {
            case "health_boost":
                int hearts = Mathf.FloorToInt(upgradeOption.value1 / 4f);
                return $"+{hearts} 하트";
            case "movement_speed_boost":
                float speedIncrease = (upgradeOption.value1 - 1f) * 100f;
                return $"+{speedIncrease:F0}% 이동속도";
            default:
                return "";
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
        
        // 원본 색상으로 복원
        if (borderImage != null)
        {
            borderImage.color = GetTypeColor(upgradeOption.type);
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
        
        // 상호작용 비활성화 (중복 클릭 방지)
        isInteractable = false;
        
        // 선택 효과
        StartCoroutine(AnimateSelection());
        
        // 레벨업 매니저에 선택 알림
        if (levelUpManager != null)
        {
            levelUpManager.SelectUpgrade(upgradeOption.id);
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
    /// 선택 애니메이션
    /// </summary>
    private System.Collections.IEnumerator AnimateSelection()
    {
        // 색상을 선택 색상으로 변경
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
        
        // 펄스 효과
        Vector3 pulseScale = originalScale * 1.2f;
        yield return StartCoroutine(AnimateScale(pulseScale));
        yield return StartCoroutine(AnimateScale(originalScale));
    }
    
    private void OnDestroy()
    {
        // 이벤트 정리
        if (optionButton != null)
        {
            optionButton.onClick.RemoveAllListeners();
        }
    }
}