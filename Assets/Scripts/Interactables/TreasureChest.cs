using UnityEngine;

/// <summary>
/// 보물상자 - 플레이어와 상호작용하여 보상 지급
/// </summary>
public class TreasureChest : MonoBehaviour
{
    [Header("상태")]
    [SerializeField] private bool isOpened = false;
    
    [Header("프리팹 참조")]
    [SerializeField] private GameObject closedChestPrefab;  // PF Props - Chest 01
    [SerializeField] private GameObject openedChestPrefab;  // PF Props - Chest 01 Open
    
    [Header("사운드")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip rewardSound;
    
    [Header("이펙트")]
    [SerializeField] private GameObject openEffect;        // 열림 이펙트 (선택사항)
    [SerializeField] private float effectDuration = 1f;
    
    [Header("자동 제거")]
    [SerializeField] private float autoDestroyDelay = 3f;  // 열린 후 자동 제거까지 시간 (초)
    [SerializeField] private bool enableAutoDestroy = true; // 자동 제거 활성화
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 컴포넌트 참조
    private Collider2D chestCollider;
    private AudioSource audioSource;
    private RewardSystem rewardSystem;
    
    // 이벤트
    public System.Action<TreasureReward> OnChestOpened;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        InitializeReferences();
        SetupChest();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // Collider2D 설정
        chestCollider = GetComponent<Collider2D>();
        if (chestCollider == null)
        {
            chestCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        chestCollider.isTrigger = true;
        
        // AudioSource 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        rewardSystem = RewardSystem.Instance;
        
        if (rewardSystem == null)
        {
            Debug.LogError("[TreasureChest] RewardSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 보물상자 설정
    /// </summary>
    private void SetupChest()
    {
        // 초기 상태는 닫힌 상태
        isOpened = false;
        UpdateChestVisual();
    }
    
    /// <summary>
    /// 보물상자 시각적 상태 업데이트
    /// </summary>
    private void UpdateChestVisual()
    {
        // 현재 자식 오브젝트들 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Chest"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        // 적절한 프리팹 인스턴스화
        GameObject prefabToUse = isOpened ? openedChestPrefab : closedChestPrefab;
        
        if (prefabToUse != null)
        {
            GameObject chestVisual = Instantiate(prefabToUse, transform);
            chestVisual.transform.localPosition = Vector3.zero;
            chestVisual.name = isOpened ? "OpenedChest" : "ClosedChest";
            
            // 프리팹의 Collider 비활성화 (부모의 Collider 사용)
            Collider2D prefabCollider = chestVisual.GetComponent<Collider2D>();
            if (prefabCollider != null)
            {
                prefabCollider.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"[TreasureChest] {(isOpened ? "열린" : "닫힌")} 상자 프리팹이 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 플레이어와 충돌 감지
    /// </summary>
    /// <param name="other">충돌한 오브젝트</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 열린 상자는 무시
        if (isOpened) return;
        
        // 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            OpenChest();
        }
    }
    
    /// <summary>
    /// 보물상자 열기
    /// </summary>
    private void OpenChest()
    {
        if (isOpened) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureChest] 📦 보물상자 열림!");
        }
        
        // 상태 변경
        isOpened = true;
        
        // 시각적 업데이트
        UpdateChestVisual();
        
        // 사운드 재생
        PlayOpenSound();
        
        // 이펙트 재생
        PlayOpenEffect();
        
        // 보상 지급
        GiveReward();
        
        // 콜라이더 비활성화 (재상호작용 방지)
        if (chestCollider != null)
        {
            chestCollider.enabled = false;
        }
        
        // 자동 제거 시작
        if (enableAutoDestroy)
        {
            StartCoroutine(AutoDestroyCoroutine());
        }
    }
    
    /// <summary>
    /// 보상 지급
    /// </summary>
    private void GiveReward()
    {
        if (rewardSystem == null)
        {
            Debug.LogError("[TreasureChest] RewardSystem이 없어서 보상을 지급할 수 없습니다!");
            return;
        }
        
        // 보상 결정 및 지급
        TreasureReward reward = rewardSystem.DetermineAndGiveReward();
        
        // 보상 사운드 재생
        PlayRewardSound();
        
        // 보상 애니메이션 표시 (UI 시스템에서 처리)
        ShowRewardAnimation(reward);
        
        // 이벤트 발생
        OnChestOpened?.Invoke(reward);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureChest] 🎁 보상 지급 완료: {reward.nameText}");
        }
    }
    
    /// <summary>
    /// 보상 애니메이션 표시
    /// </summary>
    /// <param name="reward">표시할 보상</param>
    private void ShowRewardAnimation(TreasureReward reward)
    {
        // RewardAnimationUI 시스템과 연동
        if (RewardAnimationUI.Instance != null)
        {
            RewardAnimationUI.Instance.ShowRewardAnimation(reward);
        }
        else
        {
            Debug.LogWarning("[TreasureChest] RewardAnimationUI가 씬에 없습니다!");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureChest] 🎬 보상 애니메이션: {reward.nameText}");
        }
    }
    
    /// <summary>
    /// 열림 사운드 재생
    /// </summary>
    private void PlayOpenSound()
    {
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }
    
    /// <summary>
    /// 보상 사운드 재생
    /// </summary>
    private void PlayRewardSound()
    {
        if (audioSource != null && rewardSound != null)
        {
            audioSource.PlayOneShot(rewardSound);
        }
    }
    
    /// <summary>
    /// 열림 이펙트 재생
    /// </summary>
    private void PlayOpenEffect()
    {
        if (openEffect != null)
        {
            GameObject effect = Instantiate(openEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }
    
    /// <summary>
    /// 보물상자 리셋 (재사용을 위해)
    /// </summary>
    public void ResetChest()
    {
        isOpened = false;
        
        if (chestCollider != null)
        {
            chestCollider.enabled = true;
        }
        
        UpdateChestVisual();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureChest] 🔄 보물상자 리셋됨");
        }
    }
    
    /// <summary>
    /// 현재 상태 확인
    /// </summary>
    /// <returns>열린 상태 여부</returns>
    public bool IsOpened()
    {
        return isOpened;
    }
    
    /// <summary>
    /// 자동 제거 코루틴
    /// </summary>
    private System.Collections.IEnumerator AutoDestroyCoroutine()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureChest] ⏰ 보물상자 {autoDestroyDelay}초 후 자동 제거 시작");
        }
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(autoDestroyDelay);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureChest] 🗑️ 보물상자 자동 제거됨");
        }
        
        // 게임오브젝트 파괴
        Destroy(gameObject);
    }
}