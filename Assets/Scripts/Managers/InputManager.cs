using UnityEngine;

/// <summary>
/// 게임 전체 입력 관리자 (ESC 키 등)
/// </summary>
public class InputManager : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject pauseMenuCanvas;
    
    // 싱글톤 패턴
    public static InputManager Instance { get; private set; }
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // PauseMenuCanvas 찾기 (Inspector에서 설정되지 않은 경우)
        if (pauseMenuCanvas == null)
        {
            pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
            if (pauseMenuCanvas == null)
            {
                Debug.LogWarning("InputManager: PauseMenuCanvas를 찾을 수 없습니다!");
            }
        }
    }
    
    void Update()
    {
        HandleInputs();
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    private void HandleInputs()
    {
        // ESC 키로 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }
    
    /// <summary>
    /// 일시정지 메뉴 토글
    /// </summary>
    public void TogglePauseMenu()
    {
        if (pauseMenuCanvas == null) return;
        
        // PauseMenuManager가 있는지 확인
        PauseMenuManager pauseManager = pauseMenuCanvas.GetComponent<PauseMenuManager>();
        if (pauseManager != null)
        {
            // PauseMenuCanvas가 비활성화되어 있다면 활성화하고 일시정지
            if (!pauseMenuCanvas.activeInHierarchy)
            {
                pauseMenuCanvas.SetActive(true);
                pauseManager.PauseGame();
            }
            else
            {
                // 이미 활성화되어 있다면 토글
                pauseManager.TogglePause();
            }
        }
        else
        {
            Debug.LogWarning("InputManager: PauseMenuManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 일시정지 메뉴 강제 열기 (모바일 버튼에서 사용)
    /// </summary>
    public void OpenPauseMenu()
    {
        if (pauseMenuCanvas == null) return;
        
        PauseMenuManager pauseManager = pauseMenuCanvas.GetComponent<PauseMenuManager>();
        if (pauseManager != null)
        {
            if (!pauseMenuCanvas.activeInHierarchy)
            {
                pauseMenuCanvas.SetActive(true);
            }
            
            pauseManager.PauseGame();
        }
    }
}