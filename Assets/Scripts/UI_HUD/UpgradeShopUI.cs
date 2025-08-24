using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 영구 업그레이드 상점 UI
/// </summary>
public class UpgradeShopUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button openShopButton;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private Transform upgradeListParent;
    [SerializeField] private GameObject upgradeItemPrefab;
    
    [Header("골드 표시")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private string goldFormat = "보유 골드: {0}";
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 참조
    private PermanentUpgradeSystem upgradeSystem;
    private GoldSystem goldSystem;
    
    // UI 관리
    private List<UpgradeItemUI> upgradeItems = new List<UpgradeItemUI>();
    
    private void Start()
    {
        InitializeReferences();
        SetupUI();
        CreateUpgradeItems();
        SubscribeToEvents();
        
        // 초기 상태: 상점 닫힌 상태
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        upgradeSystem = PermanentUpgradeSystem.Instance;
        goldSystem = GoldSystem.Instance;
        
        if (upgradeSystem == null)
        {
            Debug.LogError("[UpgradeShopUI] PermanentUpgradeSystem을 찾을 수 없습니다!");
        }
        
        if (goldSystem == null)
        {
            Debug.LogError("[UpgradeShopUI] GoldSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// UI 설정
    /// </summary>
    private void SetupUI()
    {
        // 상점 열기/닫기 버튼 설정
        if (openShopButton != null)
        {
            openShopButton.onClick.AddListener(OpenShop);
        }
        
        if (closeShopButton != null)
        {
            closeShopButton.onClick.AddListener(CloseShop);
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
            Debug.Log($"[UpgradeShopUI] 업그레이드 아이템 {upgradeItems.Count}개 생성됨");
        }
    }
    
    /// <summary>
    /// 개별 업그레이드 아이템 UI 생성
    /// </summary>
    /// <param name="upgrade">업그레이드 데이터</param>
    private void CreateUpgradeItem(PermanentUpgrade upgrade)
    {
        if (upgradeItemPrefab == null)
        {
            // 프리팹이 없으면 간단한 UI를 코드로 생성
            CreateSimpleUpgradeItem(upgrade);
            return;
        }
        
        // 프리팹을 이용한 UI 생성 (추후 구현)
        GameObject itemObj = Instantiate(upgradeItemPrefab, upgradeListParent);
        UpgradeItemUI itemUI = itemObj.GetComponent<UpgradeItemUI>();
        
        if (itemUI == null)
        {
            itemUI = itemObj.AddComponent<UpgradeItemUI>();
        }
        
        itemUI.Initialize(upgrade, upgradeSystem, this);
        upgradeItems.Add(itemUI);
    }
    
    /// <summary>
    /// 간단한 업그레이드 아이템 UI 생성 (프리팹 없을 때)
    /// </summary>
    /// <param name="upgrade">업그레이드 데이터</param>
    private void CreateSimpleUpgradeItem(PermanentUpgrade upgrade)
    {
        // 간단한 버튼 UI 생성
        GameObject buttonObj = new GameObject($"Upgrade_{upgrade.type}");
        buttonObj.transform.SetParent(upgradeListParent);
        buttonObj.transform.localScale = Vector3.one;
        
        // Button 컴포넌트
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);
        
        // RectTransform 설정
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 80);
        
        // 텍스트 추가
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        textObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = GetUpgradeButtonText(upgrade);
        text.color = Color.white;
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // 버튼 클릭 이벤트
        button.onClick.AddListener(() => OnUpgradeButtonClicked(upgrade.type));
        
        // UpgradeItemUI 컴포넌트 생성
        UpgradeItemUI itemUI = buttonObj.AddComponent<UpgradeItemUI>();
        itemUI.SetSimpleUI(button, text, upgrade);
        upgradeItems.Add(itemUI);
    }
    
    /// <summary>
    /// 업그레이드 버튼 텍스트 생성
    /// </summary>
    /// <param name="upgrade">업그레이드 데이터</param>
    /// <returns>버튼 텍스트</returns>
    private string GetUpgradeButtonText(PermanentUpgrade upgrade)
    {
        if (upgradeSystem == null) return upgrade.displayName;
        
        int currentLevel = upgradeSystem.GetUpgradeLevel(upgrade.type);
        int cost = upgradeSystem.GetUpgradeCost(upgrade.type);
        
        if (currentLevel >= upgrade.maxLevel)
        {
            return $"{upgrade.displayName} (최대 레벨)";
        }
        
        return $"{upgrade.displayName} LV.{currentLevel}\n비용: {cost}골드";
    }
    
    /// <summary>
    /// 업그레이드 버튼 클릭 처리
    /// </summary>
    /// <param name="upgradeType">업그레이드 타입</param>
    private void OnUpgradeButtonClicked(PermanentUpgradeType upgradeType)
    {
        if (upgradeSystem == null) return;
        
        bool success = upgradeSystem.PurchaseUpgrade(upgradeType);
        
        if (success)
        {
            UpdateUI();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[UpgradeShopUI] ✅ 업그레이드 구매 성공: {upgradeType}");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[UpgradeShopUI] ❌ 업그레이드 구매 실패: {upgradeType}");
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
        UpdateUpgradeItems(); // 구매 가능 상태 업데이트
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
    /// 상점 열기
    /// </summary>
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            UpdateUI();
            
            if (enableDebugLogs)
            {
                Debug.Log("[UpgradeShopUI] 🏪 업그레이드 상점 열림");
            }
        }
    }
    
    /// <summary>
    /// 상점 닫기
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            
            if (enableDebugLogs)
            {
                Debug.Log("[UpgradeShopUI] 🏪 업그레이드 상점 닫힘");
            }
        }
    }
    
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
/// 개별 업그레이드 아이템 UI
/// </summary>
public class UpgradeItemUI : MonoBehaviour
{
    private PermanentUpgrade upgradeData;
    private PermanentUpgradeSystem upgradeSystem;
    private UpgradeShopUI shopUI;
    
    // Simple UI 모드용
    private Button button;
    private TextMeshProUGUI text;
    
    public void Initialize(PermanentUpgrade upgrade, PermanentUpgradeSystem system, UpgradeShopUI shop)
    {
        upgradeData = upgrade;
        upgradeSystem = system;
        shopUI = shop;
    }
    
    public void SetSimpleUI(Button btn, TextMeshProUGUI txt, PermanentUpgrade upgrade)
    {
        button = btn;
        text = txt;
        upgradeData = upgrade;
    }
    
    public void UpdateDisplay()
    {
        if (text != null && upgradeData != null && upgradeSystem != null)
        {
            int currentLevel = upgradeSystem.GetUpgradeLevel(upgradeData.type);
            int cost = upgradeSystem.GetUpgradeCost(upgradeData.type);
            bool canAfford = upgradeSystem.CanPurchaseUpgrade(upgradeData.type);
            
            if (currentLevel >= upgradeData.maxLevel)
            {
                text.text = $"{upgradeData.displayName} (최대 레벨)";
                if (button != null) button.interactable = false;
            }
            else
            {
                text.text = $"{upgradeData.displayName} LV.{currentLevel}\n비용: {cost}골드";
                if (button != null) button.interactable = canAfford;
            }
            
            // 색상 업데이트
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = canAfford ? new Color(0.2f, 0.5f, 0.2f, 0.8f) : new Color(0.5f, 0.2f, 0.2f, 0.8f);
                button.colors = colors;
            }
        }
    }
}