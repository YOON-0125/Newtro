using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 메뉴 관리자
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI 패널들")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    
    [Header("버튼 사운드")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        // AudioSource 컴포넌트 가져오기 (없으면 추가)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 메인 메뉴 패널만 활성화
        ShowMainMenu();
    }
    
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        PlayButtonSound();
        
        // 게임 씬으로 전환
        SceneManager.LoadScene("PlayScene");
    }
    
    /// <summary>
    /// 설정 창 열기
    /// </summary>
    public void OpenSettings()
    {
        PlayButtonSound();
        
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    /// <summary>
    /// 크레딧 창 열기
    /// </summary>
    public void OpenCredits()
    {
        PlayButtonSound();
        
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }
    
    /// <summary>
    /// 메인 메뉴로 돌아가기
    /// </summary>
    public void ShowMainMenu()
    {
        PlayButtonSound();
        
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }
    
    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
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
}