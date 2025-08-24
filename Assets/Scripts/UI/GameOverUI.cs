using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임오버 UI 관리
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "PlayScene";
    
    private GameManager gameManager;
    
    private void Awake()
    {
        // GameManager 찾기
        gameManager = FindObjectOfType<GameManager>();
        
        // 초기에는 패널 비활성화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
    
    private void Start()
    {
        // GameManager 이벤트 구독
        if (gameManager != null && gameManager.Events != null)
        {
            gameManager.Events.OnGameLose.AddListener(ShowGameOverUI);
            Debug.Log("[GameOverUI] OnGameLose 이벤트 구독 완료");
        }
        else
        {
            Debug.LogError("[GameOverUI] GameManager 또는 Events가 null입니다!");
        }
        
        // 버튼 이벤트 연결
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gameManager != null && gameManager.Events != null)
        {
            gameManager.Events.OnGameLose.RemoveListener(ShowGameOverUI);
        }
    }
    
    /// <summary>
    /// 게임오버 UI 표시
    /// </summary>
    public void ShowGameOverUI()
    {
        Debug.Log("[GameOverUI] ShowGameOverUI 호출됨!");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("[GameOverUI] 게임오버 패널 활성화됨");
        }
        else
        {
            Debug.LogError("[GameOverUI] gameOverPanel이 null입니다!");
        }
        
        // 점수 및 시간 정보 업데이트
        UpdateGameOverInfo();
        
        Debug.Log("[GameOverUI] 게임오버 UI 표시 완료");
    }
    
    /// <summary>
    /// 게임오버 정보 업데이트
    /// </summary>
    private void UpdateGameOverInfo()
    {
        if (gameManager == null) return;
        
        // 점수 표시
        if (scoreText != null)
        {
            scoreText.text = $"점수: {gameManager.Score:N0}";
        }
        
        // 생존 시간 표시
        if (timeText != null)
        {
            float survivalTime = gameManager.GameTime;
            
            // 디버그 로그 추가
            Debug.Log($"[GameOverUI] 실제 게임 시간: {survivalTime}초");
            
            int minutes = Mathf.FloorToInt(survivalTime / 60f);
            int seconds = Mathf.FloorToInt(survivalTime % 60f);
            timeText.text = $"생존 시간: {minutes:00}:{seconds:00}";
            
            Debug.Log($"[GameOverUI] 표시된 시간: {minutes:00}:{seconds:00}");
        }
        
        // 게임오버 텍스트 (필요시 커스터마이징)
        if (gameOverText != null)
        {
            gameOverText.text = "게임 오버";
        }
    }
    
    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[GameOverUI] 게임 재시작");
        
        // 게임 시간 복원
        Time.timeScale = 1f;
        
        // 현재 씬 다시 로드
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// 메인 메뉴로 이동
    /// </summary>
    public void GoToMainMenu()
    {
        Debug.Log("[GameOverUI] 메인 메뉴로 이동");
        
        // 게임 시간 복원
        Time.timeScale = 1f;
        
        // 메인 메뉴 씬 로드
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// 수동으로 게임오버 UI 숨기기 (필요시)
    /// </summary>
    public void HideGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// ESC 키로 게임 종료 (개발/테스트용)
    /// </summary>
    private void Update()
    {
        // 게임오버 상태에서 ESC 키 처리
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
        }
    }
}