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
    private HealthSystem healthSystem;
    
    private void Awake()
    {
        healthSystem = FindObjectOfType<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem을 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 등록
        healthSystem.OnHealthChanged.AddListener(UpdateHealthUI);
        healthSystem.OnMaxHealthChanged.AddListener(UpdateMaxHealthUI);
    }
    
    private void Start()
    {
        InitializeHearts();
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged.RemoveListener(UpdateHealthUI);
            healthSystem.OnMaxHealthChanged.RemoveListener(UpdateMaxHealthUI);
        }
    }
    
    /// <summary>
    /// 초기 하트 UI를 설정합니다
    /// </summary>
    private void InitializeHearts()
    {
        // 기존 하트들 제거
        ClearHearts();
        
        // 새 하트들 생성
        for (int i = 0; i < healthSystem.MaxHearts; i++)
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
        GameObject heartObj;
        
        if (heartPrefab != null)
        {
            heartObj = Instantiate(heartPrefab, heartContainer);
        }
        else
        {
            // Prefab이 없을 경우 동적 생성
            heartObj = new GameObject("Heart");
            heartObj.transform.SetParent(heartContainer);
            heartObj.AddComponent<Image>();
        }
        
        Image heartImage = heartObj.GetComponent<Image>();
        if (heartImage != null)
        {
            heartImage.sprite = heartSprites[4]; // 초기에는 가득 찬 하트
            heartImages.Add(heartImage);
        }
    }
    
    /// <summary>
    /// 기존 하트 UI들을 제거합니다
    /// </summary>
    private void ClearHearts()
    {
        foreach (Image heart in heartImages)
        {
            if (heart != null)
                DestroyImmediate(heart.gameObject);
        }
        heartImages.Clear();
    }
    
    /// <summary>
    /// 체력 변화에 따른 UI 업데이트
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    private void UpdateHealthUI(int currentHealth)
    {
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// 최대 체력 변화에 따른 UI 업데이트
    /// </summary>
    /// <param name="maxHealth">최대 체력</param>
    private void UpdateMaxHealthUI(int maxHealth)
    {
        InitializeHearts();
    }
    
    /// <summary>
    /// 하트 표시를 업데이트합니다
    /// </summary>
    private void UpdateHealthDisplay()
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < healthSystem.MaxHearts)
            {
                // 활성화된 하트
                heartImages[i].gameObject.SetActive(true);
                
                // 해당 하트의 채워진 정도 계산
                int fillAmount = healthSystem.GetHeartFillAmount(i);
                
                // 스프라이트 설정
                if (fillAmount >= 0 && fillAmount < heartSprites.Length)
                {
                    heartImages[i].sprite = heartSprites[fillAmount];
                }
            }
            else
            {
                // 비활성화된 하트
                heartImages[i].gameObject.SetActive(false);
            }
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
        if (healthSystem != null)
            healthSystem.TakeDamage(1);
    }
    
    [ContextMenu("Test Heal")]
    private void TestHeal()
    {
        if (healthSystem != null)
            healthSystem.Heal(1);
    }
    
    [ContextMenu("Test Add Heart")]
    private void TestAddHeart()
    {
        if (healthSystem != null)
            healthSystem.IncreaseMaxHealth(1);
    }
#endif
}