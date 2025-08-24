using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 웨이브 패턴 카운트다운 UI (3→2→1 표시)
/// </summary>
public class WavePatternCountdownUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image backgroundImage;
    
    [Header("애니메이션 설정")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1.2f, 1f, 1f);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private float countdownDuration = 1f;
    
    [Header("스타일 설정")]
    [SerializeField] private Color[] countdownColors = {
        Color.red,    // 3
        Color.yellow, // 2  
        Color.green   // 1
    };
    [SerializeField] private string[] countdownTexts = { "3", "2", "1" };
    [SerializeField] private float textSize = 80f;
    
    // 싱글톤 패턴
    public static WavePatternCountdownUI Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        
        // 초기화
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }
    
    /// <summary>
    /// 카운트다운 시작 (3→2→1)
    /// </summary>
    public void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// 카운트다운 코루틴
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        if (countdownPanel == null || countdownText == null)
        {
            Debug.LogError("[WavePatternCountdownUI] UI 컴포넌트가 설정되지 않았습니다!");
            yield break;
        }
        
        // 패널 활성화
        countdownPanel.SetActive(true);
        
        // 3, 2, 1 카운트다운
        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(ShowCountdownNumber(i));
        }
        
        // 패널 비활성화
        countdownPanel.SetActive(false);
    }
    
    /// <summary>
    /// 개별 숫자 표시 애니메이션
    /// </summary>
    private IEnumerator ShowCountdownNumber(int index)
    {
        if (countdownText == null) yield break;
        
        // 텍스트 설정
        countdownText.text = countdownTexts[index];
        countdownText.color = countdownColors[index];
        countdownText.fontSize = textSize;
        
        // 배경 색상 설정
        if (backgroundImage != null)
        {
            Color bgColor = countdownColors[index];
            bgColor.a = 0.3f; // 반투명
            backgroundImage.color = bgColor;
        }
        
        float elapsedTime = 0f;
        Vector3 originalScale = Vector3.one;
        Color originalColor = countdownText.color;
        
        // 애니메이션 루프
        while (elapsedTime < countdownDuration)
        {
            float progress = elapsedTime / countdownDuration;
            
            // 스케일 애니메이션
            float scaleValue = scaleCurve.Evaluate(progress);
            countdownText.transform.localScale = originalScale * scaleValue;
            
            // 알파 애니메이션
            float alphaValue = alphaCurve.Evaluate(progress);
            Color newColor = originalColor;
            newColor.a = alphaValue;
            countdownText.color = newColor;
            
            // 배경 알파
            if (backgroundImage != null)
            {
                Color bgColor = backgroundImage.color;
                bgColor.a = alphaValue * 0.3f;
                backgroundImage.color = bgColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 최종 상태로 리셋
        countdownText.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 강제로 카운트다운 중지
    /// </summary>
    public void StopCountdown()
    {
        StopAllCoroutines();
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }
    
    /// <summary>
    /// 테스트용 카운트다운 실행
    /// </summary>
    [ContextMenu("Test Countdown")]
    public void TestCountdown()
    {
        if (Application.isPlaying)
        {
            StartCountdown();
            Debug.Log("[WavePatternCountdownUI] 테스트 카운트다운 실행!");
        }
    }
}