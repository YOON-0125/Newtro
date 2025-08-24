using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 유물 보관함 UI
/// </summary>
public class ArtifactInventoryUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button backButton;
    [SerializeField] private ScrollRect inventoryScrollView;
    [SerializeField] private Transform inventoryListParent;
    
    [Header("필터 UI")]
    [SerializeField] private Toggle showAllToggle;
    [SerializeField] private Toggle showCommonToggle;
    [SerializeField] private Toggle showRareToggle;
    [SerializeField] private Toggle showEpicToggle;
    [SerializeField] private Toggle showLegendaryToggle;
    
    [Header("정렬 UI")]
    [SerializeField] private Dropdown sortDropdown;
    [SerializeField] private Toggle sortAscendingToggle;
    
    [Header("통계 UI")]
    [SerializeField] private TextMeshProUGUI totalCountText;
    [SerializeField] private TextMeshProUGUI commonCountText;
    [SerializeField] private TextMeshProUGUI rareCountText;
    [SerializeField] private TextMeshProUGUI epicCountText;
    [SerializeField] private TextMeshProUGUI legendaryCountText;
    
    [Header("상세 정보 패널")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescText;
    [SerializeField] private TextMeshProUGUI detailRarityText;
    [SerializeField] private TextMeshProUGUI detailEffectText;
    [SerializeField] private Button detailCloseButton;
    
    [Header("레이아웃 설정")]
    [SerializeField] private float itemSpacing = 10f;
    [SerializeField] private Vector2 itemSize = new Vector2(150, 180);
    [SerializeField] private int itemsPerRow = 4;
    
    [Header("등급별 색상")]
    [SerializeField] private Color commonColor = Color.gray;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    [Header("UI 텍스트 포맷")]
    [SerializeField] private string countFormat = "{0}개";
    [SerializeField] private string totalFormat = "총 {0}개";
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 참조
    private ArtifactGachaSystem gachaSystem;
    private MainMenuManager menuManager;
    
    // 필터 및 정렬
    private ArtifactRarity currentFilter = ArtifactRarity.Common; // 전체는 따로 처리
    private bool showAllRarities = true;
    private SortType currentSortType = SortType.Rarity;
    private bool sortAscending = true;
    
    // UI 관리
    private List<ArtifactInventoryItem> inventoryItems = new List<ArtifactInventoryItem>();
    private Dictionary<int, int> artifactCounts = new Dictionary<int, int>(); // ID별 개수
    
    // 정렬 타입
    public enum SortType
    {
        Name,       // 이름순
        Rarity,     // 등급순
        Count,      // 개수순
        Recent      // 최신 획득순
    }
    
    private void Start()
    {
        InitializeReferences();
        SetupUI();
        SubscribeToEvents();
        RefreshInventory();
    }
    
    private void OnEnable()
    {
        RefreshInventory();
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        gachaSystem = ArtifactGachaSystem.Instance;
        menuManager = FindObjectOfType<MainMenuManager>();
        
        if (gachaSystem == null)
        {
            Debug.LogError("[ArtifactInventoryUI] ArtifactGachaSystem을 찾을 수 없습니다!");
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
        
        // 필터 토글 설정
        SetupFilterToggles();
        
        // 정렬 드롭다운 설정
        SetupSortDropdown();
        
        // 상세 패널 설정
        if (detailCloseButton != null)
        {
            detailCloseButton.onClick.AddListener(CloseDetailPanel);
        }
        
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        
        // GridLayoutGroup 설정
        SetupGridLayout();
    }
    
    /// <summary>
    /// 필터 토글 설정
    /// </summary>
    private void SetupFilterToggles()
    {
        if (showAllToggle != null)
        {
            showAllToggle.isOn = true;
            showAllToggle.onValueChanged.AddListener(OnShowAllToggleChanged);
        }
        
        if (showCommonToggle != null)
        {
            showCommonToggle.onValueChanged.AddListener((isOn) => OnRarityFilterChanged(ArtifactRarity.Common, isOn));
        }
        
        if (showRareToggle != null)
        {
            showRareToggle.onValueChanged.AddListener((isOn) => OnRarityFilterChanged(ArtifactRarity.Rare, isOn));
        }
        
        if (showEpicToggle != null)
        {
            showEpicToggle.onValueChanged.AddListener((isOn) => OnRarityFilterChanged(ArtifactRarity.Epic, isOn));
        }
        
        if (showLegendaryToggle != null)
        {
            showLegendaryToggle.onValueChanged.AddListener((isOn) => OnRarityFilterChanged(ArtifactRarity.Legendary, isOn));
        }
    }
    
    /// <summary>
    /// 정렬 드롭다운 설정
    /// </summary>
    private void SetupSortDropdown()
    {
        if (sortDropdown != null)
        {
            sortDropdown.options.Clear();
            sortDropdown.options.Add(new Dropdown.OptionData("이름순"));
            sortDropdown.options.Add(new Dropdown.OptionData("등급순"));
            sortDropdown.options.Add(new Dropdown.OptionData("개수순"));
            sortDropdown.options.Add(new Dropdown.OptionData("최신순"));
            
            sortDropdown.value = (int)currentSortType;
            sortDropdown.onValueChanged.AddListener(OnSortTypeChanged);
        }
        
        if (sortAscendingToggle != null)
        {
            sortAscendingToggle.isOn = sortAscending;
            sortAscendingToggle.onValueChanged.AddListener(OnSortOrderChanged);
        }
    }
    
    /// <summary>
    /// 그리드 레이아웃 설정
    /// </summary>
    private void SetupGridLayout()
    {
        if (inventoryListParent == null) return;
        
        GridLayoutGroup gridLayout = inventoryListParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = inventoryListParent.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        gridLayout.cellSize = itemSize;
        gridLayout.spacing = new Vector2(itemSpacing, itemSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = itemsPerRow;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        
        // ContentSizeFitter 추가
        ContentSizeFitter sizeFitter = inventoryListParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = inventoryListParent.gameObject.AddComponent<ContentSizeFitter>();
        }
        
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    
    /// <summary>
    /// 보관함 새로고침
    /// </summary>
    public void RefreshInventory()
    {
        if (gachaSystem == null) return;
        
        // 보유 유물 데이터 수집
        CollectArtifactData();
        
        // UI 업데이트
        UpdateInventoryItems();
        UpdateStatistics();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ArtifactInventoryUI] 보관함 새로고침 완료: {inventoryItems.Count}개 표시");
        }
    }
    
    /// <summary>
    /// 유물 데이터 수집
    /// </summary>
    private void CollectArtifactData()
    {
        artifactCounts.Clear();
        
        var ownedIds = gachaSystem.GetOwnedArtifactIds();
        
        // ID별 개수 계산
        foreach (int id in ownedIds)
        {
            if (artifactCounts.ContainsKey(id))
            {
                artifactCounts[id]++;
            }
            else
            {
                artifactCounts[id] = 1;
            }
        }
    }
    
    /// <summary>
    /// 보관함 아이템 UI 업데이트
    /// </summary>
    private void UpdateInventoryItems()
    {
        // 기존 아이템들 정리
        foreach (Transform child in inventoryListParent)
        {
            Destroy(child.gameObject);
        }
        inventoryItems.Clear();
        
        // 필터링된 유물들 가져오기
        var filteredArtifacts = GetFilteredAndSortedArtifacts();
        
        // UI 아이템 생성
        foreach (var kvp in filteredArtifacts)
        {
            int artifactId = kvp.Key;
            int count = kvp.Value;
            
            Artifact artifact = gachaSystem.GetArtifactById(artifactId);
            if (artifact != null)
            {
                CreateInventoryItem(artifact, count);
            }
        }
    }
    
    /// <summary>
    /// 필터링 및 정렬된 유물 목록 반환
    /// </summary>
    /// <returns>필터링된 유물 딕셔너리</returns>
    private Dictionary<int, int> GetFilteredAndSortedArtifacts()
    {
        var filtered = new Dictionary<int, int>();
        
        foreach (var kvp in artifactCounts)
        {
            int artifactId = kvp.Key;
            int count = kvp.Value;
            
            Artifact artifact = gachaSystem.GetArtifactById(artifactId);
            if (artifact == null) continue;
            
            // 필터 적용
            if (!showAllRarities && artifact.rarity != currentFilter) continue;
            
            filtered[artifactId] = count;
        }
        
        // 정렬 적용
        var sortedList = filtered.ToList();
        
        sortedList.Sort((a, b) => {
            Artifact artifactA = gachaSystem.GetArtifactById(a.Key);
            Artifact artifactB = gachaSystem.GetArtifactById(b.Key);
            
            if (artifactA == null || artifactB == null) return 0;
            
            int result = currentSortType switch
            {
                SortType.Name => string.Compare(artifactA.displayName, artifactB.displayName),
                SortType.Rarity => artifactA.rarity.CompareTo(artifactB.rarity),
                SortType.Count => a.Value.CompareTo(b.Value),
                SortType.Recent => artifactA.id.CompareTo(artifactB.id), // ID가 클수록 최신
                _ => 0
            };
            
            return sortAscending ? result : -result;
        });
        
        return sortedList.ToDictionary(x => x.Key, x => x.Value);
    }
    
    /// <summary>
    /// 보관함 아이템 UI 생성
    /// </summary>
    /// <param name="artifact">유물 데이터</param>
    /// <param name="count">보유 개수</param>
    private void CreateInventoryItem(Artifact artifact, int count)
    {
        // 아이템 오브젝트 생성
        GameObject itemObj = CreateInventoryItemUI(artifact, count);
        if (itemObj == null) return;
        
        // ArtifactInventoryItem 컴포넌트 설정
        ArtifactInventoryItem itemUI = itemObj.GetComponent<ArtifactInventoryItem>();
        if (itemUI == null)
        {
            itemUI = itemObj.AddComponent<ArtifactInventoryItem>();
        }
        
        itemUI.Initialize(artifact, count, this);
        inventoryItems.Add(itemUI);
    }
    
    /// <summary>
    /// 보관함 아이템 UI 생성 (코드)
    /// </summary>
    /// <param name="artifact">유물 데이터</param>
    /// <param name="count">보유 개수</param>
    /// <returns>생성된 UI GameObject</returns>
    private GameObject CreateInventoryItemUI(Artifact artifact, int count)
    {
        // 메인 아이템
        GameObject itemObj = new GameObject($"ArtifactItem_{artifact.id}");
        itemObj.transform.SetParent(inventoryListParent);
        itemObj.transform.localScale = Vector3.one;
        
        // RectTransform
        RectTransform rectTransform = itemObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = itemSize;
        
        // Button (클릭 이벤트용)
        Button button = itemObj.AddComponent<Button>();
        
        // 배경 Image
        Image backgroundImage = itemObj.AddComponent<Image>();
        Color bgColor = GetRarityColor(artifact.rarity);
        bgColor.a = 0.8f;
        backgroundImage.color = bgColor;
        button.targetGraphic = backgroundImage;
        
        // 수직 레이아웃
        VerticalLayoutGroup verticalLayout = itemObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.spacing = 5f;
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        
        // 아이콘 영역 (선택사항)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemObj.transform);
        iconObj.transform.localScale = Vector3.one;
        
        Image iconImage = iconObj.AddComponent<Image>();
        if (artifact.icon != null)
        {
            iconImage.sprite = artifact.icon;
        }
        else
        {
            iconImage.color = GetRarityColor(artifact.rarity);
        }
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60, 60);
        
        // 이름 텍스트
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(itemObj.transform);
        nameObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = artifact.displayName;
        nameText.fontSize = 12;
        nameText.color = Color.white;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = true;
        
        // 개수 텍스트
        GameObject countObj = new GameObject("Count");
        countObj.transform.SetParent(itemObj.transform);
        countObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI countText = countObj.AddComponent<TextMeshProUGUI>();
        countText.text = string.Format(countFormat, count);
        countText.fontSize = 10;
        countText.color = Color.yellow;
        countText.alignment = TextAlignmentOptions.Center;
        
        // 버튼 이벤트
        button.onClick.AddListener(() => ShowArtifactDetail(artifact, count));
        
        return itemObj;
    }
    
    /// <summary>
    /// 유물 상세 정보 표시
    /// </summary>
    /// <param name="artifact">유물 데이터</param>
    /// <param name="count">보유 개수</param>
    public void ShowArtifactDetail(Artifact artifact, int count)
    {
        if (detailPanel == null) return;
        
        detailPanel.SetActive(true);
        
        if (detailNameText != null)
        {
            detailNameText.text = artifact.displayName;
        }
        
        if (detailDescText != null)
        {
            detailDescText.text = artifact.description;
        }
        
        if (detailRarityText != null)
        {
            detailRarityText.text = GetRarityText(artifact.rarity);
            detailRarityText.color = GetRarityColor(artifact.rarity);
        }
        
        if (detailEffectText != null)
        {
            string effectText = artifact.isPercentage 
                ? $"{artifact.effectValue * 100:F0}% 증가" 
                : $"{artifact.effectValue:F0} 증가";
            detailEffectText.text = $"효과: {GetUpgradeTypeText(artifact.upgradeType)} {effectText}\n보유: {count}개";
        }
        
        if (detailIcon != null)
        {
            if (artifact.icon != null)
            {
                detailIcon.sprite = artifact.icon;
                detailIcon.color = Color.white;
            }
            else
            {
                detailIcon.sprite = null;
                detailIcon.color = GetRarityColor(artifact.rarity);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ArtifactInventoryUI] 상세 정보 표시: {artifact.displayName}");
        }
    }
    
    /// <summary>
    /// 상세 패널 닫기
    /// </summary>
    private void CloseDetailPanel()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 통계 UI 업데이트
    /// </summary>
    private void UpdateStatistics()
    {
        var totalCount = artifactCounts.Values.Sum();
        var rarityStats = new Dictionary<ArtifactRarity, int>();
        
        // 등급별 통계 계산
        foreach (var kvp in artifactCounts)
        {
            Artifact artifact = gachaSystem.GetArtifactById(kvp.Key);
            if (artifact != null)
            {
                if (rarityStats.ContainsKey(artifact.rarity))
                {
                    rarityStats[artifact.rarity] += kvp.Value;
                }
                else
                {
                    rarityStats[artifact.rarity] = kvp.Value;
                }
            }
        }
        
        // 통계 UI 업데이트
        if (totalCountText != null)
            totalCountText.text = string.Format(totalFormat, totalCount);
        
        if (commonCountText != null)
            commonCountText.text = string.Format(countFormat, rarityStats.GetValueOrDefault(ArtifactRarity.Common, 0));
        
        if (rareCountText != null)
            rareCountText.text = string.Format(countFormat, rarityStats.GetValueOrDefault(ArtifactRarity.Rare, 0));
        
        if (epicCountText != null)
            epicCountText.text = string.Format(countFormat, rarityStats.GetValueOrDefault(ArtifactRarity.Epic, 0));
        
        if (legendaryCountText != null)
            legendaryCountText.text = string.Format(countFormat, rarityStats.GetValueOrDefault(ArtifactRarity.Legendary, 0));
    }
    
    /// <summary>
    /// 전체 보기 토글 변경
    /// </summary>
    /// <param name="isOn">토글 상태</param>
    private void OnShowAllToggleChanged(bool isOn)
    {
        showAllRarities = isOn;
        RefreshInventory();
    }
    
    /// <summary>
    /// 등급 필터 변경
    /// </summary>
    /// <param name="rarity">등급</param>
    /// <param name="isOn">토글 상태</param>
    private void OnRarityFilterChanged(ArtifactRarity rarity, bool isOn)
    {
        if (isOn)
        {
            showAllRarities = false;
            currentFilter = rarity;
            
            if (showAllToggle != null)
                showAllToggle.isOn = false;
        }
        
        RefreshInventory();
    }
    
    /// <summary>
    /// 정렬 방식 변경
    /// </summary>
    /// <param name="sortIndex">정렬 인덱스</param>
    private void OnSortTypeChanged(int sortIndex)
    {
        currentSortType = (SortType)sortIndex;
        RefreshInventory();
    }
    
    /// <summary>
    /// 정렬 순서 변경
    /// </summary>
    /// <param name="ascending">오름차순 여부</param>
    private void OnSortOrderChanged(bool ascending)
    {
        sortAscending = ascending;
        RefreshInventory();
    }
    
    /// <summary>
    /// 등급별 색상 반환
    /// </summary>
    private Color GetRarityColor(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common => commonColor,
            ArtifactRarity.Rare => rareColor,
            ArtifactRarity.Epic => epicColor,
            ArtifactRarity.Legendary => legendaryColor,
            _ => Color.white
        };
    }
    
    /// <summary>
    /// 등급 텍스트 반환
    /// </summary>
    private string GetRarityText(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common => "일반",
            ArtifactRarity.Rare => "레어",
            ArtifactRarity.Epic => "에픽",
            ArtifactRarity.Legendary => "전설",
            _ => "알 수 없음"
        };
    }
    
    /// <summary>
    /// 업그레이드 타입 텍스트 반환
    /// </summary>
    private string GetUpgradeTypeText(PermanentUpgradeType upgradeType)
    {
        return upgradeType switch
        {
            PermanentUpgradeType.MaxHealth => "최대 체력",
            PermanentUpgradeType.Damage => "데미지",
            PermanentUpgradeType.MoveSpeed => "이동속도",
            PermanentUpgradeType.ExpMultiplier => "경험치",
            _ => "알 수 없음"
        };
    }
    
    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (gachaSystem != null)
        {
            gachaSystem.OnArtifactObtained += OnArtifactObtained;
        }
    }
    
    /// <summary>
    /// 유물 획득 이벤트 처리
    /// </summary>
    /// <param name="artifact">획득한 유물</param>
    private void OnArtifactObtained(Artifact artifact)
    {
        RefreshInventory();
    }
    
    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        if (gachaSystem != null)
        {
            gachaSystem.OnArtifactObtained -= OnArtifactObtained;
        }
    }
}

/// <summary>
/// 보관함 아이템 UI
/// </summary>
public class ArtifactInventoryItem : MonoBehaviour
{
    private Artifact artifactData;
    private int count;
    private ArtifactInventoryUI inventoryUI;
    
    public void Initialize(Artifact artifact, int itemCount, ArtifactInventoryUI ui)
    {
        artifactData = artifact;
        count = itemCount;
        inventoryUI = ui;
    }
}