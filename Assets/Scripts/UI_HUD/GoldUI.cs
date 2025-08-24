using UnityEngine;
using TMPro;

/// <summary>
/// 골드 UI 표시 - HUD에 현재 골드량 표시
/// </summary>
public class GoldUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI goldText;
    
    [Header("표시 설정")]
    [SerializeField] private string goldPrefix = "골드: ";
    [SerializeField] private bool showGoldIcon = true;
    [SerializeField] private string goldIcon = "💰";
    
    [Header("애니메이션")]
    [SerializeField] private bool enableGoldChangeAnimation = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Color gainColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;
    
    // 참조
    private GoldSystem goldSystem;
    
    private void Awake()
    {
        // TextMeshProUGUI 자동 찾기
        if (goldText == null)
        {
            goldText = GetComponent<TextMeshProUGUI>();
        }
        
        if (goldText == null)
        {
            Debug.LogError("[GoldUI] TextMeshProUGUI 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    private void Start()
    {
        InitializeGoldSystem();
        UpdateGoldDisplay(goldSystem?.CurrentGold ?? 0);
    }
    
    /// <summary>
    /// 골드 시스템 초기화 및 이벤트 구독
    /// </summary>
    private void InitializeGoldSystem()
    {
        // GoldSystem 찾기
        goldSystem = GoldSystem.Instance;
        
        if (goldSystem != null)
        {
            // 골드 변경 이벤트 구독
            goldSystem.OnGoldChanged += OnGoldChanged;
            Debug.Log("[GoldUI] 골드 시스템 연결됨");
        }
        else
        {
            Debug.LogWarning("[GoldUI] GoldSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 골드 변경 이벤트 처리
    /// </summary>
    /// <param name="newGoldAmount">새로운 골드량</param>
    private void OnGoldChanged(int newGoldAmount)
    {
        UpdateGoldDisplay(newGoldAmount);
        
        // 골드 증가 애니메이션
        if (enableGoldChangeAnimation)
        {
            PlayGoldChangeAnimation();
        }
    }
    
    /// <summary>
    /// 골드 표시 업데이트
    /// </summary>
    /// <param name="goldAmount">표시할 골드량</param>
    private void UpdateGoldDisplay(int goldAmount)
    {
        if (goldText == null) return;
        
        string displayText = "";
        
        // 아이콘 추가
        if (showGoldIcon)
        {
            displayText += goldIcon + " ";
        }
        
        // 골드량 표시
        displayText += goldPrefix + goldAmount.ToString("N0"); // 천 단위 콤마
        
        goldText.text = displayText;
    }
    
    /// <summary>
    /// 골드 변경 애니메이션
    /// </summary>
    private void PlayGoldChangeAnimation()
    {
        if (goldText == null) return;
        
        // 간단한 색상 변경 애니메이션
        StartCoroutine(GoldChangeAnimationCoroutine());
    }
    
    /// <summary>
    /// 골드 변경 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator GoldChangeAnimationCoroutine()
    {
        // 골드 획득 색상으로 변경
        goldText.color = gainColor;
        
        // 약간 커지는 효과
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        float elapsed = 0f;
        
        // 커지기
        while (elapsed < animationDuration * 0.3f)
        {
            float t = elapsed / (animationDuration * 0.3f);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
        
        // 잠시 대기
        yield return new WaitForSecondsRealtime(animationDuration * 0.4f);
        
        elapsed = 0f;
        
        // 원래 크기로 돌아가기 + 색상 복원
        while (elapsed < animationDuration * 0.3f)
        {
            float t = elapsed / (animationDuration * 0.3f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            goldText.color = Color.Lerp(gainColor, normalColor, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
        goldText.color = normalColor;
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
    
    /// <summary>
    /// 골드 표시 강제 업데이트 (디버그용)
    /// </summary>
    [ContextMenu("골드 표시 업데이트")]
    public void ForceUpdateDisplay()
    {
        if (goldSystem != null)
        {
            UpdateGoldDisplay(goldSystem.CurrentGold);
        }
    }
}