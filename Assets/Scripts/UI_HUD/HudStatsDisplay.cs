using UnityEngine;
using TMPro;

/// <summary>
/// HUD에서 실시간 게임 통계를 표시하는 스크립트
/// </summary>
public class HudStatsDisplay : MonoBehaviour
{
    [Header("UI 텍스트 컴포넌트")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private TextMeshProUGUI scoreText; // 추가로 점수도 표시
    
    [Header("표시 설정")]
    [SerializeField] private bool showHours = false; // 시간 표시에 시간 포함할지
    [SerializeField] private string timePrefix = "시간: ";
    [SerializeField] private string killPrefix = "처치: ";
    [SerializeField] private string scorePrefix = "점수: ";
    
    // 게임 매니저 참조
    private GameManager gameManager;
    
    // 캐시된 값들 (불필요한 UI 업데이트 방지)
    private float lastTime = -1f;
    private int lastKills = -1;
    private int lastScore = -1;
    
    // 시간 펄스 효과용
    private float lastSecond = -1f;
    
    // 애니메이션 중복 방지
    private bool isKillAnimating = false;
    
    private void Awake()
    {
        // GameManager 찾기
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (gameManager == null)
        {
            Debug.LogError("[HudStatsDisplay] GameManager를 찾을 수 없습니다!");
        }
    }
    
    private void Start()
    {
        // GameManager 이벤트 구독
        SubscribeToEvents();
        
        // 초기 값 설정
        UpdateAllDisplays();
        
        // UI 컴포넌트 유효성 검사
        ValidateUIComponents();
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// GameManager 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (gameManager != null && gameManager.Events != null)
        {
            gameManager.Events.OnTimeUpdate.AddListener(UpdateTimeDisplay);
            gameManager.Events.OnScoreChanged.AddListener(UpdateScoreDisplay);
            
            // 적 처치는 점수 변경과 함께 발생하므로 점수 이벤트에서 처리
            gameManager.Events.OnScoreChanged.AddListener((_) => UpdateKillDisplay());
        }
    }
    
    /// <summary>
    /// GameManager 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (gameManager != null && gameManager.Events != null)
        {
            gameManager.Events.OnTimeUpdate.RemoveListener(UpdateTimeDisplay);
            gameManager.Events.OnScoreChanged.RemoveListener(UpdateScoreDisplay);
            gameManager.Events.OnScoreChanged.RemoveListener((_) => UpdateKillDisplay());
        }
    }
    
    /// <summary>
    /// 모든 표시 업데이트
    /// </summary>
    public void UpdateAllDisplays()
    {
        if (gameManager != null)
        {
            UpdateTimeDisplay(gameManager.GameTime);
            UpdateKillDisplay();
            UpdateScoreDisplay(gameManager.Score);
        }
    }
    
    /// <summary>
    /// 시간 표시 업데이트
    /// </summary>
    private void UpdateTimeDisplay(float gameTime)
    {
        if (timeText == null) return;
        
        // 캐시된 값과 비교 (소수점 반올림으로 초 단위만 비교)
        float roundedTime = Mathf.Floor(gameTime);
        if (Mathf.Approximately(lastTime, roundedTime)) return;
        
        // 초가 바뀔 때마다 펄스 애니메이션
        if (roundedTime != lastSecond)
        {
            lastSecond = roundedTime;
            StartCoroutine(TimePulseAnimation());
        }
        
        lastTime = roundedTime;
        
        string formattedTime = FormatTime(gameTime);
        timeText.text = timePrefix + formattedTime;
    }
    
    /// <summary>
    /// 적 처치 수 표시 업데이트
    /// </summary>
    private void UpdateKillDisplay()
    {
        if (killText == null || gameManager == null) return;
        
        int currentKills = gameManager.EnemiesKilled;
        if (lastKills == currentKills) return;
        
        // Kill이 증가했을 때만 애니메이션 (중복 방지)
        if (currentKills > lastKills && !isKillAnimating)
        {
            StartCoroutine(KillScaleAnimation());
        }
        
        lastKills = currentKills;
        killText.text = killPrefix + currentKills.ToString();
    }
    
    /// <summary>
    /// 점수 표시 업데이트
    /// </summary>
    private void UpdateScoreDisplay(int score)
    {
        if (scoreText == null) return;
        
        if (lastScore == score) return;
        
        lastScore = score;
        scoreText.text = scorePrefix + score.ToString("N0"); // 천 단위 콤마 표시
    }
    
    /// <summary>
    /// 시간 포맷팅
    /// </summary>
    private string FormatTime(float timeInSeconds)
    {
        int totalSeconds = Mathf.FloorToInt(timeInSeconds);
        
        if (showHours)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        else
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
    
    /// <summary>
    /// UI 컴포넌트 유효성 검사
    /// </summary>
    private void ValidateUIComponents()
    {
        if (timeText == null)
            Debug.LogWarning("[HudStatsDisplay] timeText가 설정되지 않았습니다!");
            
        if (killText == null)
            Debug.LogWarning("[HudStatsDisplay] killText가 설정되지 않았습니다!");
            
        // scoreText는 선택사항이므로 경고만
        if (scoreText == null)
            Debug.Log("[HudStatsDisplay] scoreText가 설정되지 않았습니다. (선택사항)");
    }
    
    /// <summary>
    /// 수동으로 강제 업데이트 (디버그용)
    /// </summary>
    [ContextMenu("Force Update All")]
    public void ForceUpdateAll()
    {
        lastTime = -1f;
        lastKills = -1;
        lastScore = -1;
        UpdateAllDisplays();
    }
    
    /// <summary>
    /// Kill 증가 시 스케일 애니메이션
    /// </summary>
    private System.Collections.IEnumerator KillScaleAnimation()
    {
        if (killText == null) yield break;
        
        isKillAnimating = true;
        
        Vector3 originalScale = Vector3.one; // 항상 기본 크기에서 시작
        Vector3 targetScale = originalScale * 1.2f; // 120% 스케일
        
        // 현재 스케일을 기본 크기로 즉시 리셋
        killText.transform.localScale = originalScale;
        
        float duration = 0.08f; // 80ms
        float elapsed = 0f;
        
        // 확대
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 이징 효과 (EaseOut)
            t = 1f - (1f - t) * (1f - t);
            killText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        
        // 원복
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 이징 효과 (EaseOut)
            t = 1f - (1f - t) * (1f - t);
            killText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        killText.transform.localScale = originalScale;
        isKillAnimating = false;
    }
    
    /// <summary>
    /// Time 초마다 펄스 애니메이션 (투명도 0.9→1.0)
    /// </summary>
    private System.Collections.IEnumerator TimePulseAnimation()
    {
        if (timeText == null) yield break;
        
        Color originalColor = timeText.color;
        Color dimColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.9f);
        Color brightColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1.0f);
        
        float duration = 0.15f; // 짧은 펄스
        float elapsed = 0f;
        
        // 투명도 감소 (1.0 → 0.9)
        while (elapsed < duration / 2)
        {
            float t = elapsed / (duration / 2);
            timeText.color = Color.Lerp(brightColor, dimColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        
        // 투명도 증가 (0.9 → 1.0)
        while (elapsed < duration / 2)
        {
            float t = elapsed / (duration / 2);
            timeText.color = Color.Lerp(dimColor, brightColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        timeText.color = originalColor;
    }
    
    /// <summary>
    /// Inspector에서 설정 변경 시 미리보기
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateAllDisplays();
        }
    }
}