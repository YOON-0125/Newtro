using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 메인메뉴 업그레이드 상점 UI
/// </summary>
public class MainMenuUpgradeShop : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button backButton;
    [SerializeField] private ScrollRect upgradeScrollView;
    [SerializeField] private Transform upgradeListParent;
    [SerializeField] private GameObject upgradeItemPrefab;
    
    [Header("골드 표시")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private string goldFormat = "보유 골드: {0}";
    
    [Header("상점 설정")]
    [SerializeField] private Color purchasableColor = new Color(0.2f, 0.6f, 0.2f, 0.9f);
    [SerializeField] private Color unpurchasableColor = new Color(0.6f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color maxLevelColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    
    [Header("레이아웃 설정")]
    [SerializeField] private float itemSpacing = 10f;
    [SerializeField] private Vector2 itemSize = new Vector2(400, 120);
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 참조
    private PermanentUpgradeSystem upgradeSystem;
    private GoldSystem goldSystem;
    private MainMenuManager menuManager;
    
    // UI 관리
    private List<MainMenuUpgradeItem> upgradeItems = new List<MainMenuUpgradeItem>();
    
    private void Start()
    {
        InitializeReferences();
        SetupUI();
        CreateUpgradeItems();
        SubscribeToEvents();
        UpdateUI();
    }
    
    private void OnEnable()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        upgradeSystem = PermanentUpgradeSystem.Instance;
        goldSystem = GoldSystem.Instance;
        menuManager = FindObjectOfType<MainMenuManager>();
        
        if (upgradeSystem == null)
        {
            Debug.LogError("[MainMenuUpgradeShop] PermanentUpgradeSystem을 찾을 수 없습니다!");
        }
        
        if (goldSystem == null)
        {
            Debug.LogError("[MainMenuUpgradeShop] GoldSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// UI 설정
    /// </summary>
    private void SetupUI()
    {
        // 뒤로가기 버튼
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                if (menuManager != null)
                {
                    menuManager.ShowMainMenu();
                }
            });
        }
        
        // ScrollRect 설정
        if (upgradeScrollView != null)
        {
            upgradeScrollView.horizontal = false;
            upgradeScrollView.vertical = true;
        }
        
        // VerticalLayoutGroup 설정
        if (upgradeListParent != null)
        {
            VerticalLayoutGroup layoutGroup = upgradeListParent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = upgradeListParent.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            
            layoutGroup.spacing = itemSpacing;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            
            // ContentSizeFitter 추가
            ContentSizeFitter sizeFitter = upgradeListParent.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = upgradeListParent.gameObject.AddComponent<ContentSizeFitter>();
            }
            
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    
    /// <summary>
    /// 업그레이드 아이템 UI 생성
    /// </summary>
    private void CreateUpgradeItems()
    {
        if (upgradeSystem == null || upgradeListParent == null) return;
        
        // 기존 아이템들 정리
        foreach (Transform child in upgradeListParent)
        {
            Destroy(child.gameObject);
        }
        upgradeItems.Clear();
        
        // 업그레이드 아이템 생성
        var allUpgrades = upgradeSystem.GetAllUpgrades();
        foreach (var upgrade in allUpgrades)
        {
            CreateUpgradeItem(upgrade);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[MainMenuUpgradeShop] 업그레이드 아이템 {upgradeItems.Count}개 생성됨");
        }
    }
    
    /// <summary>
    /// 개별 업그레이드 아이템 생성
    /// </summary>
    /// <param name="upgrade">업그레이드 데이터</param>
    private void CreateUpgradeItem(PermanentUpgrade upgrade)
    {
        // 아이템 UI 생성
        GameObject itemObj = CreateUpgradeItemUI(upgrade);
        if (itemObj == null) return;
        
        // MainMenuUpgradeItem 컴포넌트 설정
        MainMenuUpgradeItem itemUI = itemObj.GetComponent<MainMenuUpgradeItem>();
        if (itemUI == null)
        {
            itemUI = itemObj.AddComponent<MainMenuUpgradeItem>();
        }
        
        itemUI.Initialize(upgrade, upgradeSystem, goldSystem, this);
        upgradeItems.Add(itemUI);
    }
    
    /// <summary>
    /// 업그레이드 아이템 UI 생성 (코드로 생성)
    /// </summary>
    /// <param name="upgrade">업그레이드 데이터</param>
    /// <returns>생성된 UI GameObject</returns>
    private GameObject CreateUpgradeItemUI(PermanentUpgrade upgrade)
    {
        // 메인 패널
        GameObject itemObj = new GameObject($"UpgradeItem_{upgrade.type}");
        itemObj.transform.SetParent(upgradeListParent);
        itemObj.transform.localScale = Vector3.one;
        
        // RectTransform 설정
        RectTransform rectTransform = itemObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = itemSize;
        
        // Image (배경)
        Image backgroundImage = itemObj.AddComponent<Image>();
        backgroundImage.color = purchasableColor;
        
        // Button
        Button button = itemObj.AddComponent<Button>();
        button.targetGraphic = backgroundImage;
        
        // 수직 레이아웃
        VerticalLayoutGroup verticalLayout = itemObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 5f;
        verticalLayout.padding = new RectOffset(15, 15, 10, 10);
        verticalLayout.childAlignment = TextAnchor.MiddleLeft;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        
        // 제목 텍스트
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(itemObj.transform);
        titleObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = upgrade.displayName;
        titleText.fontSize = 18;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        
        // 설명 텍스트
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(itemObj.transform);
        descObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = upgrade.description;
        descText.fontSize = 12;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        
        // 정보 텍스트 (레벨, 비용)
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(itemObj.transform);
        infoObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
        infoText.fontSize = 14;
        infoText.color = Color.yellow;
        infoText.fontStyle = FontStyles.Bold;
        
        // 버튼 이벤트
        button.onClick.AddListener(() => OnUpgradeButtonClicked(upgrade.type));
        
        // MainMenuUpgradeItem에 참조 저장
        MainMenuUpgradeItem itemComponent = itemObj.AddComponent<MainMenuUpgradeItem>();
        itemComponent.SetUIReferences(button, backgroundImage, titleText, descText, infoText);
        
        return itemObj;
    }
    
    /// <summary>
    /// 업그레이드 버튼 클릭 처리
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    public void OnUpgradeButtonClicked(PermanentUpgradeType upgradeType)
    {
        if (upgradeSystem == null) return;
        
        bool success = upgradeSystem.PurchaseUpgrade(upgradeType);
        
        if (success)
        {
            UpdateUI();
            
            if (enableDebugLogs)
            {
                PermanentUpgrade upgrade = upgradeSystem.GetUpgradeByType(upgradeType);
                int newLevel = upgradeSystem.GetUpgradeLevel(upgradeType);
                Debug.Log($"[MainMenuUpgradeShop] ✅ 업그레이드 구매: {upgrade.displayName} 레벨 {newLevel}");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MainMenuUpgradeShop] ❌ 업그레이드 구매 실패: {upgradeType}");
            }
        }
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    public void UpdateUI()
    {
        UpdateGoldDisplay();
        UpdateUpgradeItems();
    }
    
    /// <summary>
    /// 골드 표시 업데이트
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldText != null && goldSystem != null)
        {
            goldText.text = string.Format(goldFormat, goldSystem.CurrentGold.ToString("N0"));
        }
    }
    
    /// <summary>
    /// 업그레이드 아이템들 업데이트
    /// </summary>
    private void UpdateUpgradeItems()
    {
        foreach (var item in upgradeItems)
        {
            if (item != null)
            {
                item.UpdateDisplay();
            }
        }
    }
    
    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (goldSystem != null)
        {
            goldSystem.OnGoldChanged += OnGoldChanged;
        }
        
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradePurchased += OnUpgradePurchased;
        }
    }
    
    /// <summary>
    /// 골드 변경 이벤트 처리
    /// </summary>
    /// <param name="newGoldAmount">새로운 골드량</param>
    private void OnGoldChanged(int newGoldAmount)
    {
        UpdateGoldDisplay();
        UpdateUpgradeItems();
    }
    
    /// <summary>
    /// 업그레이드 구매 이벤트 처리
    /// </summary>
    /// <param name="upgradeType">구매된 업그레이드</param>
    /// <param name="newLevel">새 레벨</param>
    private void OnUpgradePurchased(PermanentUpgradeType upgradeType, int newLevel)
    {
        UpdateUI();
    }
    
    /// <summary>
    /// 색상 설정 가져오기
    /// </summary>
    public Color GetPurchasableColor() => purchasableColor;
    public Color GetUnpurchasableColor() => unpurchasableColor;
    public Color GetMaxLevelColor() => maxLevelColor;
    
    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        if (goldSystem != null)
        {
            goldSystem.OnGoldChanged -= OnGoldChanged;
        }
        
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradePurchased -= OnUpgradePurchased;
        }
    }
}

/// <summary>
/// 메인메뉴 업그레이드 아이템 UI
/// </summary>
public class MainMenuUpgradeItem : MonoBehaviour
{
    // UI 컴포넌트들
    private Button button;
    private Image backgroundImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI infoText;
    
    // 데이터 참조
    private PermanentUpgrade upgradeData;
    private PermanentUpgradeSystem upgradeSystem;
    private GoldSystem goldSystem;
    private MainMenuUpgradeShop shopUI;
    
    public void Initialize(PermanentUpgrade upgrade, PermanentUpgradeSystem system, GoldSystem gold, MainMenuUpgradeShop shop)
    {
        upgradeData = upgrade;
        upgradeSystem = system;
        goldSystem = gold;
        shopUI = shop;
    }
    
    public void SetUIReferences(Button btn, Image bg, TextMeshProUGUI title, TextMeshProUGUI desc, TextMeshProUGUI info)
    {
        button = btn;
        backgroundImage = bg;
        titleText = title;
        descriptionText = desc;
        infoText = info;
    }
    
    public void UpdateDisplay()
    {
        if (upgradeData == null || upgradeSystem == null || goldSystem == null) return;
        
        int currentLevel = upgradeSystem.GetUpgradeLevel(upgradeData.type);
        int cost = upgradeSystem.GetUpgradeCost(upgradeData.type);
        bool canAfford = upgradeSystem.CanPurchaseUpgrade(upgradeData.type);
        
        // 정보 텍스트 업데이트
        if (infoText != null)
        {
            if (currentLevel >= upgradeData.maxLevel)
            {
                infoText.text = $"레벨 {currentLevel}/{upgradeData.maxLevel} (최대)";
            }
            else
            {
                string effectText = upgradeData.isPercentage 
                    ? $"{upgradeData.effectValue * 100:F0}%" 
                    : upgradeData.effectValue.ToString("F0");
                
                infoText.text = $"레벨 {currentLevel}/{upgradeData.maxLevel} | 효과: +{effectText} | 비용: {cost:N0}골드";
            }
        }
        
        // 버튼 상태 업데이트
        if (button != null)
        {
            button.interactable = canAfford && currentLevel < upgradeData.maxLevel;
        }
        
        // 배경 색상 업데이트
        if (backgroundImage != null && shopUI != null)
        {
            if (currentLevel >= upgradeData.maxLevel)
            {
                backgroundImage.color = shopUI.GetMaxLevelColor();
            }
            else if (canAfford)
            {
                backgroundImage.color = shopUI.GetPurchasableColor();
            }
            else
            {
                backgroundImage.color = shopUI.GetUnpurchasableColor();
            }
        }
    }
}