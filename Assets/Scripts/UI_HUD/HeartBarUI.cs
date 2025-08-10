using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform heartContainer;
    [SerializeField] private GameObject heartPrefab;
    
    [Header("Heart Sprites")]
    [SerializeField] private Sprite[] heartSprites = new Sprite[5];
    // heartSprites[0] = 빈 하트
    // heartSprites[1] = 1/4 채워진 하트
    // heartSprites[2] = 2/4 채워진 하트
    // heartSprites[3] = 3/4 채워진 하트
    // heartSprites[4] = 완전히 채워진 하트
    
    private List<Image> heartImages = new List<Image>();
    private PlayerHealth playerHealth;
    
    private void Awake()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth를 찾을 수 없습니다!");
            return;
        }
        // 이벤트 등록은 Start()로 이동
    }
    
    private void Start()
    {
        // 이벤트 등록을 여기서
        if (playerHealth != null && playerHealth.events != null)
        {
            playerHealth.events.OnHealthChanged.AddListener(UpdateHealthUI);
        }
        
        InitializeHearts();
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제
        if (playerHealth != null)
        {
            playerHealth.events.OnHealthChanged.RemoveListener(UpdateHealthUI);
        }
    }
    
    /// <summary>
    /// 초기 하트 UI를 설정합니다
    /// </summary>
    private void InitializeHearts()
    {
        // 기존 하트들 제거
        ClearHearts();
        
        // 새 하트들 생성 (최대 하트 개수 = 최대체력 / 4)
        int maxHearts = Mathf.CeilToInt(playerHealth.MaxHealth / 4f);
        for (int i = 0; i < maxHearts; i++)
        {
            CreateHeart();
        }
        
        // 체력 UI 업데이트
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// 새로운 하트 UI를 생성합니다
    /// </summary>
    private void CreateHeart()
    {
        GameObject heartObj = heartPrefab != null
            ? Instantiate(heartPrefab, heartContainer)                  // worldPositionStays=false
            : new GameObject("Heart", typeof(RectTransform), typeof(Image));

        var rt = heartObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;

        var img = heartObj.GetComponent<Image>();
        img.sprite = heartSprites[4]; // 기본 꽉 찬 하트
        img.color  = Color.white;     // 혹시 알파 0 예방

        // 레이아웃이 자식 크기를 “안”관리하면 직접 크기 설정
        if (!HasControlChildSize(heartContainer))
        {
            var s = img.sprite != null ? img.sprite.rect.size : new Vector2(64, 64);
            rt.sizeDelta = s; // 64x64 같은 고정 크기
        }

        // 레이아웃이 자식 크기를 “관리”한다면, 프리퍼드 크기 제공
        if (HasControlChildSize(heartContainer))
        {
            var le = heartObj.GetComponent<LayoutElement>() ?? heartObj.AddComponent<LayoutElement>();
            le.preferredWidth  = 64;
            le.preferredHeight = 64;
        }

        heartImages.Add(img);
    }

    private bool HasControlChildSize(Transform t)
    {
        var h = t.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        var v = t.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        return (h != null && (h.childControlWidth || h.childControlHeight))
            || (v != null && (v.childControlWidth || v.childControlHeight));
    }
    
    /// <summary>
    /// 기존 하트 UI들을 제거합니다
    /// </summary>
    private void ClearHearts()
    {
        foreach (var heart in heartImages)
            if (heart) Destroy(heart.gameObject);
        heartImages.Clear();
    }
    
    /// <summary>
    /// 체력 변화에 따른 UI 업데이트
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    private void UpdateHealthUI(float currentHealth)
    {
        Debug.Log($"HeartBarUI: UpdateHealthUI called with health: {currentHealth}");
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// 하트 표시를 업데이트합니다
    /// </summary>
    private void UpdateHealthDisplay()
    {
        int maxHearts = Mathf.CeilToInt(playerHealth.MaxHealth / 4f);

        // 부족하면 추가 생성
        while (heartImages.Count < maxHearts) CreateHeart();

        float current = playerHealth.Health;
        for (int i = 0; i < heartImages.Count; i++)
        {
            bool active = i < maxHearts;
            heartImages[i].gameObject.SetActive(active);
            if (!active) continue;

            float h = current - (i * 4f);
            int fill = Mathf.Clamp(Mathf.RoundToInt(h), 0, 4);
            if (fill >= 0 && fill < heartSprites.Length)
                heartImages[i].sprite = heartSprites[fill];
        }
    }
    
    /// <summary>
    /// 하트 애니메이션 효과 (선택사항)
    /// </summary>
    /// <param name="heartIndex">애니메이션할 하트 인덱스</param>
    public void AnimateHeart(int heartIndex)
    {
        if (heartIndex >= 0 && heartIndex < heartImages.Count)
        {
            // 간단한 스케일 애니메이션
            StartCoroutine(HeartPulseAnimation(heartImages[heartIndex].transform));
        }
    }
    
    private System.Collections.IEnumerator HeartPulseAnimation(Transform heartTransform)
    {
        Vector3 originalScale = heartTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        // 크기 증가
        while (elapsed < duration)
        {
            heartTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        
        // 크기 감소
        while (elapsed < duration)
        {
            heartTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        heartTransform.localScale = originalScale;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 테스트용 메서드들
    /// </summary>
    [ContextMenu("Test Take Damage")]
    private void TestTakeDamage()
    {
        if (playerHealth != null)
            playerHealth.TakeDamage(1f);
    }
    
    [ContextMenu("Test Heal")]
    private void TestHeal()
    {
        if (playerHealth != null)
            playerHealth.RestoreHealth(1f);
    }
    
    [ContextMenu("Test Add Heart")]
    private void TestAddHeart()
    {
        if (playerHealth != null)
            playerHealth.IncreaseMaxHealth(4f); // 1하트 = 4체력
    }
#endif
}