using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 일시정지 메뉴 관리자
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("UI 패널들")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject statusPanel;
    
    [Header("버튼 사운드")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    
    private AudioSource audioSource;
    private bool isPaused = false;
    
    // 싱글톤 패턴
    public static PauseMenuManager Instance { get; private set; }
    
    void Awake()
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
    }
    
    void Start()
    {
        // AudioSource 컴포넌트 가져오기 (없으면 추가)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 시작할 때는 일시정지 메뉴 숨김
        HidePauseMenu();
    }
    
    void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    /// <summary>
    /// 일시정지 토글
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        PlayButtonSound();
        
        isPaused = true;
        Time.timeScale = 0f; // 게임 시간 정지
        
        pauseMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        statusPanel.SetActive(false);
        
        // 커서 표시 (UI 조작을 위해)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        PlayButtonSound();
        
        isPaused = false;
        Time.timeScale = 1f; // 게임 시간 재개
        
        HidePauseMenu();
        
        // 커서 숨김 (게임 플레이 시)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    /// <summary>
    /// 설정 창 열기
    /// </summary>
    public void OpenSettings()
    {
        PlayButtonSound();
        
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        statusPanel.SetActive(false);
    }
    
    /// <summary>
    /// 일시정지 메뉴로 돌아가기
    /// </summary>
    public void BackToPauseMenu()
    {
        PlayButtonSound();
        
        pauseMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        statusPanel.SetActive(false);
    }
    
    /// <summary>
    /// 메인 메뉴로 이동
    /// </summary>
    public void GoToMainMenu()
    {
        PlayButtonSound();
        
        // 게임 시간 정상화
        Time.timeScale = 1f;
        isPaused = false;
        
        // 메인 메뉴 씬으로 전환
        SceneManager.LoadScene("MainMenuScene");
    }
    
    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        PlayButtonSound();
        
        // 게임 시간 정상화
        Time.timeScale = 1f;
        isPaused = false;
        
        // 현재 씬 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// 상태 확인 창 열기
    /// </summary>
    public void ShowStatus()
    {
        PlayButtonSound();
        
        pauseMenuPanel.SetActive(false);
        statusPanel.SetActive(true);
        
        // 상태 정보 업데이트
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// 상태 확인 창에서 일시정지 메뉴로 돌아가기
    /// </summary>
    public void BackFromStatus()
    {
        PlayButtonSound();
        
        pauseMenuPanel.SetActive(true);
        statusPanel.SetActive(false);
    }
    
    /// <summary>
    /// 상태 정보 업데이트 (StatusDisplayManager에서 호출될 예정)
    /// </summary>
    private void UpdateStatusDisplay()
    {
        // StatusDisplayManager를 통해 실제 게임 데이터를 UI에 반영
        StatusDisplayManager statusDisplay = GetComponent<StatusDisplayManager>();
        if (statusDisplay != null)
        {
            statusDisplay.UpdateDisplay();
        }
    }
    
    /// <summary>
    /// 일시정지 메뉴 숨김
    /// </summary>
    private void HidePauseMenu()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        statusPanel.SetActive(false);
    }
    
    /// <summary>
    /// 버튼 클릭 사운드 재생
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    /// <summary>
    /// 버튼 호버 사운드 재생 (UI 버튼 이벤트에서 호출)
    /// </summary>
    public void PlayHoverSound()
    {
        if (audioSource != null && buttonHoverSound != null)
        {
            audioSource.PlayOneShot(buttonHoverSound);
        }
    }
    
    /// <summary>
    /// 현재 일시정지 상태 확인
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
}