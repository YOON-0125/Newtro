using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 메인메뉴 관리자
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("메뉴 패널들")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject upgradeShopPanel;
    [SerializeField] private GameObject artifactGachaPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("메인 메뉴 버튼들")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button upgradeShopButton;
    [SerializeField] private Button artifactGachaButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    
    [Header("골드 표시")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private string goldFormat = "골드: {0}";
    
    [Header("게임 시작 설정")]
    [SerializeField] private string gameSceneName = "PlayScene";
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 참조
    private GoldSystem goldSystem;
    private PermanentUpgradeSystem upgradeSystem;
    private ArtifactGachaSystem gachaSystem;
    private UpgradeEffectApplier effectApplier;
    
    // UI 상태
    private GameObject currentActivePanel;
    
    private void Start()
    {
        InitializeReferences();
        SetupButtons();
        ShowMainMenu();
        UpdateGoldDisplay();
        
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] 🏠 메인메뉴 초기화 완료");
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        goldSystem = GoldSystem.Instance;
        upgradeSystem = PermanentUpgradeSystem.Instance;
        gachaSystem = ArtifactGachaSystem.Instance;
        effectApplier = UpgradeEffectApplier.Instance;
        
        if (goldSystem == null)
        {
            Debug.LogError("[MainMenuManager] GoldSystem을 찾을 수 없습니다!");
        }
        
        // 골드 변경 이벤트 구독
        if (goldSystem != null)
        {
            goldSystem.OnGoldChanged += OnGoldChanged;
        }
    }
    
    /// <summary>
    /// 버튼 설정
    /// </summary>
    private void SetupButtons()
    {
        // 메인 메뉴 버튼들
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        
        if (upgradeShopButton != null)
            upgradeShopButton.onClick.AddListener(ShowUpgradeShop);
        
        if (artifactGachaButton != null)
            artifactGachaButton.onClick.AddListener(ShowArtifactGacha);
        
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(ShowInventory);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ShowSettings);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }
    
    /// <summary>
    /// 메인메뉴 표시
    /// </summary>
    public void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
        
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] 📋 메인메뉴 표시");
        }
    }
    
    /// <summary>
    /// 업그레이드 상점 표시
    /// </summary>
    public void ShowUpgradeShop()
    {
        SetActivePanel(upgradeShopPanel);
        
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] 🔧 업그레이드 상점 표시");
        }
    }
    
    /// <summary>
    /// 유물 가챠 표시
    /// </summary>
    public void ShowArtifactGacha()
    {
        SetActivePanel(artifactGachaPanel);
        
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] 🎰 유물 가챠 표시");
        }
    }
    
    /// <summary>
    /// 보관함 표시
    /// </summary>
    public void ShowInventory()
    {
        SetActivePanel(inventoryPanel);
        
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] 📦 보관함 표시");
        }
    }
    
    /// <summary>
    /// 설정 표시
    /// </summary>
    public void ShowSettings()
    {
        SetActivePanel(settingsPanel);
        
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] ⚙️ 설정 표시");
        }
    }
    
    /// <summary>
    /// 활성 패널 설정
    /// </summary>
    /// <param name="targetPanel">표시할 패널</param>
    private void SetActivePanel(GameObject targetPanel)
    {
        // 모든 패널 비활성화
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);
        if (artifactGachaPanel != null) artifactGachaPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // 대상 패널 활성화
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            currentActivePanel = targetPanel;
        }
    }
    
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MainMenuManager] 🎮 게임 시작: {gameSceneName}");
        }
        
        // 업그레이드 효과 적용 (혹시 모를 상황을 위해)
        if (effectApplier != null)
        {
            effectApplier.ApplyAllUpgrades();
        }
        
        // 씬 로드
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// 게임 종료
    /// </summary>
    public void ExitGame()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[MainMenuManager] 👋 게임 종료");
        }
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
    /// 골드 변경 이벤트 처리
    /// </summary>
    /// <param name="newGoldAmount">새로운 골드량</param>
    private void OnGoldChanged(int newGoldAmount)
    {
        UpdateGoldDisplay();
    }
    
    /// <summary>
    /// 뒤로가기 (Android Back 버튼 대응)
    /// </summary>
    private void Update()
    {
        // Android 뒤로가기 버튼 처리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }
    
    /// <summary>
    /// 뒤로가기 버튼 처리
    /// </summary>
    private void HandleBackButton()
    {
        if (currentActivePanel == mainMenuPanel || currentActivePanel == null)
        {
            // 메인메뉴에서 뒤로가기면 게임 종료 확인
            ExitGame();
        }
        else
        {
            // 다른 패널에서 뒤로가기면 메인메뉴로
            ShowMainMenu();
        }
    }
    
    /// <summary>
    /// 골드 추가 (테스트용)
    /// </summary>
    [ContextMenu("테스트 골드 추가 (1000)")]
    public void AddTestGold()
    {
        if (goldSystem != null)
        {
            goldSystem.AddGold(1000);
            Debug.Log("[MainMenuManager] 💰 테스트 골드 1000 추가됨");
        }
    }
    
    /// <summary>
    /// 모든 데이터 리셋 (테스트용)
    /// </summary>
    [ContextMenu("모든 데이터 리셋")]
    public void ResetAllData()
    {
        if (goldSystem != null)
        {
            PlayerPrefs.DeleteKey("PlayerGold");
        }
        
        if (upgradeSystem != null)
        {
            upgradeSystem.ResetAllUpgrades();
        }
        
        if (gachaSystem != null)
        {
            gachaSystem.ResetOwnedArtifacts();
        }
        
        PlayerPrefs.Save();
        
        // UI 업데이트
        UpdateGoldDisplay();
        
        Debug.Log("[MainMenuManager] 🔄 모든 데이터가 리셋되었습니다.");
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
    }
}