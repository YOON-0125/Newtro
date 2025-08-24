using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 보물상자 보상 애니메이션 UI - 타이핑 효과 및 페이드 애니메이션
/// </summary>
public class RewardAnimationUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private TextMeshProUGUI rewardText;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float displayDuration = 1.0f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("타이핑 메시지")]
    [SerializeField] private string determiningMessage = "보상 결정 중";
    [SerializeField] private string dotAnimation = "...";
    
    [Header("보상별 색상")]
    [SerializeField] private Color goldColor = Color.yellow;
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color clearMapColor = Color.cyan;
    [SerializeField] private Color awakeningColor = Color.magenta;
    [SerializeField] private Color defaultColor = Color.white;
    
    [Header("배경 설정")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 상태 관리
    private bool isAnimationPlaying = false;
    private Coroutine currentAnimationCoroutine;
    
    // 싱글톤
    public static RewardAnimationUI Instance { get; private set; }
    
    private void Awake()
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
        
        InitializeComponents();
        SetupUI();
    }
    
    private void Start()
    {
        SubscribeToEvents();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // CanvasGroup 자동 찾기
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 자식 컴포넌트 자동 찾기
        if (backgroundPanel == null)
        {
            backgroundPanel = GetComponentInChildren<Image>();
        }
        
        if (rewardText == null)
        {
            rewardText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // 필수 컴포넌트 확인
        if (rewardText == null)
        {
            Debug.LogError("[RewardAnimationUI] TextMeshProUGUI 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// UI 초기 설정
    /// </summary>
    private void SetupUI()
    {
        // 초기 상태: 보이지 않음
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // 배경 색상 설정
        if (backgroundPanel != null)
        {
            backgroundPanel.color = backgroundColor;
        }
        
        // 텍스트 초기화
        if (rewardText != null)
        {
            rewardText.text = "";
            rewardText.color = defaultColor;
        }
    }
    
    /// <summary>
    /// 보물상자 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        // TreasureChest 인스턴스들의 이벤트 구독
        // 동적으로 생성되는 보물상자들을 위해 정적 이벤트 사용을 고려할 수도 있음
        
        if (enableDebugLogs)
        {
            Debug.Log("[RewardAnimationUI] 이벤트 구독 완료");
        }
    }
    
    /// <summary>
    /// 보상 애니메이션 표시
    /// </summary>
    /// <param name="reward">표시할 보상 정보</param>
    public void ShowRewardAnimation(TreasureReward reward)
    {
        if (isAnimationPlaying)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[RewardAnimationUI] 이미 애니메이션이 재생 중입니다.");
            }
            return;
        }
        
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
        
        currentAnimationCoroutine = StartCoroutine(PlayRewardAnimationCoroutine(reward));
    }
    
    /// <summary>
    /// 보상 애니메이션 코루틴
    /// </summary>
    /// <param name="reward">표시할 보상</param>
    private IEnumerator PlayRewardAnimationCoroutine(TreasureReward reward)
    {
        isAnimationPlaying = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[RewardAnimationUI] 🎬 애니메이션 시작: {reward.nameText}");
        }
        
        // 1단계: 페이드 인
        yield return StartCoroutine(FadeInCoroutine());
        
        // 2단계: 타이핑 효과 - "보상 결정 중..."
        yield return StartCoroutine(TypingEffectCoroutine(determiningMessage + dotAnimation));
        
        // 3단계: 실제 보상 표시
        yield return StartCoroutine(ShowRewardResultCoroutine(reward));
        
        // 4단계: 표시 대기
        yield return new WaitForSeconds(displayDuration);
        
        // 5단계: 페이드 아웃
        yield return StartCoroutine(FadeOutCoroutine());
        
        isAnimationPlaying = false;
        currentAnimationCoroutine = null;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[RewardAnimationUI] ✅ 애니메이션 완료");
        }
    }
    
    /// <summary>
    /// 페이드 인 애니메이션
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeInDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// 페이드 아웃 애니메이션
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 타이핑 효과 애니메이션
    /// </summary>
    /// <param name="message">타이핑할 메시지</param>
    private IEnumerator TypingEffectCoroutine(string message)
    {
        rewardText.text = "";
        rewardText.color = defaultColor;
        
        for (int i = 0; i <= message.Length; i++)
        {
            rewardText.text = message.Substring(0, i);
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }
    
    /// <summary>
    /// 보상 결과 표시
    /// </summary>
    /// <param name="reward">표시할 보상</param>
    private IEnumerator ShowRewardResultCoroutine(TreasureReward reward)
    {
        // 보상에 따른 색상 및 아이콘 설정
        Color rewardColor = GetRewardColor(reward.type);
        string rewardIcon = GetRewardIcon(reward.type);
        string displayText = $"{rewardIcon} {reward.nameText}";
        
        // 색상 변경
        rewardText.color = rewardColor;
        
        // 텍스트 지우고 새 텍스트 타이핑
        rewardText.text = "";
        
        for (int i = 0; i <= displayText.Length; i++)
        {
            rewardText.text = displayText.Substring(0, i);
            yield return new WaitForSecondsRealtime(typingSpeed * 0.5f); // 결과 표시는 조금 더 빠르게
        }
    }
    
    /// <summary>
    /// 보상 타입에 따른 색상 반환
    /// </summary>
    /// <param name="rewardType">보상 타입</param>
    /// <returns>보상 색상</returns>
    private Color GetRewardColor(TreasureRewardType rewardType)
    {
        switch (rewardType)
        {
            case TreasureRewardType.Gold:
                return goldColor;
            case TreasureRewardType.Health:
                return healthColor;
            case TreasureRewardType.ClearMap:
                return clearMapColor;
            case TreasureRewardType.Awakening:
                return awakeningColor;
            default:
                return defaultColor;
        }
    }
    
    /// <summary>
    /// 보상 타입에 따른 아이콘 반환
    /// </summary>
    /// <param name="rewardType">보상 타입</param>
    /// <returns>보상 아이콘</returns>
    private string GetRewardIcon(TreasureRewardType rewardType)
    {
        switch (rewardType)
        {
            case TreasureRewardType.Gold:
                return "💰";
            case TreasureRewardType.Health:
                return "❤️";
            case TreasureRewardType.ClearMap:
                return "⚡";
            case TreasureRewardType.Awakening:
                return "✨";
            default:
                return "🎁";
        }
    }
    
    /// <summary>
    /// 애니메이션 강제 중지
    /// </summary>
    public void StopAnimation()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        
        isAnimationPlaying = false;
        canvasGroup.alpha = 0f;
        rewardText.text = "";
    }
    
    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
    }
    
    /// <summary>
    /// 테스트용 애니메이션 실행 (디버그)
    /// </summary>
    [ContextMenu("테스트 애니메이션 - 골드")]
    private void TestGoldAnimation()
    {
        TreasureReward testReward = new TreasureReward
        {
            type = TreasureRewardType.Gold,
            nameText = "골드 25",
            description = "25 골드를 획득했습니다!"
        };
        ShowRewardAnimation(testReward);
    }
}