using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manager that handles boss reward system
/// </summary>
public class BossRewardManager : MonoBehaviour
{
    [Header("Boss Reward UI")]
    [SerializeField] private Transform rewardOptionsParent;
    [SerializeField] private GameObject rewardOptionPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject rewardPanel;

    [Header("Trophy Rewards")]
    [SerializeField] private List<RelicBase> possibleTrophyRelics;
    
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip confirmSound;
    
    // 현재 보상 옵션들
    private List<BossRewardOptionUI> currentRewardOptions = new List<BossRewardOptionUI>();
    private BossRewardOptionUI selectedOption = null;
    private bool isRewardActive = false;
    
    // 외부 매니저 참조
    private GameManager gameManager;
    private BossManager bossManager;
    
    private void Awake()
    {
        // 외부 매니저 참조 설정
        gameManager = GameManager.Instance;
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        
        bossManager = BossManager.Instance;
        if (bossManager == null) bossManager = FindObjectOfType<BossManager>();
        
        // 확인 버튼 이벤트 설정 (기존 리스너 제거 후 새로 추가)
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();  // 기존 LevelUpManager 리스너 제거
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
            confirmButton.interactable = false;
        }
        
        Debug.Log($"[BossRewardManager] Awake() - 현재 GameObject: {gameObject.name}");
        Debug.Log($"[BossRewardManager] Awake() - 현재 활성화 상태: {gameObject.activeInHierarchy}");
    }
    
    private void Start()
    {
        Debug.Log($"[BossRewardManager] Start() - Current GameObject: {gameObject.name}");
        Debug.Log($"[BossRewardManager] Start() - Current activation state: {gameObject.activeInHierarchy}");
        
        // Initialize as inactive at start - keep GameObject active but make UI invisible
        // The BossManager will handle showing/hiding the UI panels
        isRewardActive = false;
        
        // Hide UI panels by deactivating RewardPanel directly
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
            Debug.Log($"[BossRewardManager] RewardPanel deactivated at start");
        }
        else
        {
            Debug.LogWarning($"[BossRewardManager] RewardPanel not assigned in inspector!");
        }
        
        Debug.Log($"[BossRewardManager] BossRewardManager initialized - GameObject remains active");
    }
    
    /// <summary>
    /// 보스 보상 UI 표시
    /// </summary>
    public void ShowBossReward(BossType bossType)
    {
        if (isRewardActive)
        {
            Debug.LogWarning("[BossRewardManager] 이미 보상이 활성화되어 있습니다!");
            return;
        }
        
        Debug.Log($"[BossRewardManager] ShowBossReward 시작 - BossType: {bossType}");
        Debug.Log($"[BossRewardManager] 현재 GameObject: {gameObject.name}");
        Debug.Log($"[BossRewardManager] 활성화 전 상태: {gameObject.activeInHierarchy}");
        
        isRewardActive = true;
        
        // Activate RewardPanel (BossManager handles Canvas activation, here we handle panel activation)
        if (rewardPanel != null && !rewardPanel.activeInHierarchy)
        {
            rewardPanel.SetActive(true);
            Debug.Log($"[BossRewardManager] RewardPanel activated");
        }
        else if (rewardPanel == null)
        {
            Debug.LogError($"[BossRewardManager] RewardPanel not assigned in inspector!");
        }
        
        // 게임 일시정지
        Time.timeScale = 0f;
        
        // 부모 Canvas 확인 및 Sort Order 설정
        Canvas canvas = GetComponentInParent<Canvas>();
        Debug.Log($"[BossRewardManager] 부모 Canvas: {(canvas != null ? canvas.name : "null")}");
        if (canvas != null)
        {
            Debug.Log($"[BossRewardManager] Canvas 활성화 상태: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"[BossRewardManager] Canvas Sort Order (변경 전): {canvas.sortingOrder}");
            
            // Sort Order를 높게 설정하여 다른 UI 위에 표시
            canvas.sortingOrder = 1000;
            Debug.Log($"[BossRewardManager] Canvas Sort Order (변경 후): {canvas.sortingOrder}");
            Debug.Log($"[BossRewardManager] Canvas Render Mode: {canvas.renderMode}");
            
            // Canvas가 비활성화되어 있다면 활성화
            if (!canvas.gameObject.activeInHierarchy)
            {
                canvas.gameObject.SetActive(true);
                Debug.Log($"[BossRewardManager] 부모 Canvas '{canvas.name}' 활성화됨");
            }
        }
        
        // 현재 상태 재확인
        Debug.Log($"[BossRewardManager] 최종 활성화 상태: {gameObject.activeInHierarchy}");
        Debug.Log($"[BossRewardManager] Transform 위치: {transform.position}");
        Debug.Log($"[BossRewardManager] Transform 스케일: {transform.localScale}");
        
        // 제목 설정
        if (titleText != null)
        {
            titleText.text = $"{GetBossName(bossType)} Defeat Reward";
            Debug.Log($"[BossRewardManager] 제목 설정: {titleText.text}");
        }
        else
        {
            Debug.LogWarning("[BossRewardManager] titleText가 null입니다!");
        }
        
        // 보상 옵션 생성
        GenerateRewardOptions(bossType);
        
        Debug.Log($"[BossRewardManager] 보스 보상 UI 표시 완료: {bossType}");
    }
    
    /// <summary>
    /// 보스 보상 UI 숨기기
    /// </summary>
    public void HideBossReward()
    {
        if (!isRewardActive) 
        {
            Debug.Log("[BossRewardManager] HideBossReward 호출되었지만 이미 비활성 상태");
            return;
        }
        
        Debug.Log("[BossRewardManager] HideBossReward 시작");
        
        isRewardActive = false;
        
        // Panel scale reset (for next use)
        if (rewardOptionsParent != null)
        {
            Transform parentPanel = rewardOptionsParent.parent;
            if (parentPanel != null)
            {
                RectTransform panelRect = parentPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.localScale = Vector3.one;
                    Debug.Log("[BossRewardManager] Panel scale reset");
                }
            }
        }
        
        // 보상 옵션들 정리
        ClearRewardOptions();
        
        // Resume game
        Time.timeScale = 1f;
        Debug.Log("[BossRewardManager] Game time resumed");
        
        // Deactivate RewardPanel
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
            Debug.Log("[BossRewardManager] RewardPanel deactivated");
        }
    }
    
    /// <summary>
    /// Generate reward options (3 slots: Trophy, Power, Awakening)
    /// </summary>
    private void GenerateRewardOptions(BossType bossType)
    {
        ClearRewardOptions();
        
        if (rewardOptionPrefab == null || rewardOptionsParent == null)
        {
            Debug.LogError("[BossRewardManager] Reward option prefab or parent is not set!");
            return;
        }
        
        // RewardContainer layout is handled by Unity UI - no hardcoded settings
        
        // Generate 3 slots: Trophy, Power, Awakening
        BossRewardSlotType[] slotTypes = { 
            BossRewardSlotType.Trophy, 
            BossRewardSlotType.Power, 
            BossRewardSlotType.Awakening 
        };
        
        for (int i = 0; i < slotTypes.Length; i++)
        {
            // Create reward option UI
            GameObject optionObj = Instantiate(rewardOptionPrefab, rewardOptionsParent);
            BossRewardOptionUI optionUI = optionObj.GetComponent<BossRewardOptionUI>();
            
            Debug.Log($"[BossRewardManager] Option {i+1} created - optionUI: {(optionUI != null ? "✅" : "❌")}");
            
            // Individual option layout handled by prefab and Unity UI
            
            if (optionUI != null)
            {
                // Generate reward data
                BossRewardOption rewardData = GenerateRewardData(bossType, slotTypes[i]);
                
                // Initialize UI
                optionUI.Initialize(rewardData, this);
                currentRewardOptions.Add(optionUI);
                
                Debug.Log($"[BossRewardManager] Reward initialization completed: {rewardData.displayName}");
            }
            else
            {
                Debug.LogError($"[BossRewardManager] Prefab does not have BossRewardOptionUI component! Prefab: {rewardOptionPrefab.name}");
            }
        }
        
        Debug.Log($"[BossRewardManager] {currentRewardOptions.Count} reward options created");
    }
    
    /// <summary>
    /// 보상 데이터 생성
    /// </summary>
    private BossRewardOption GenerateRewardData(BossType bossType, BossRewardSlotType slotType)
    {
        BossRewardOption reward = new BossRewardOption();
        
        switch (slotType)
        {
            case BossRewardSlotType.Trophy:
                reward.id = $"Trophy_{bossType}";
                reward.displayName = "Powerful Artifact";
                reward.description = "Greatly increases weapon damage and effects.";
                reward.slotType = slotType;
                reward.value1 = 50f; // 데미지 증가
                reward.value2 = 1.3f; // 효과 배율
                break;
                
            case BossRewardSlotType.Power:
                reward.id = $"Power_{bossType}";
                reward.displayName = "New Power";
                reward.description = "Acquire special abilities.";
                reward.slotType = slotType;
                reward.value1 = 0f;
                reward.value2 = 0f;
                break;
                
            case BossRewardSlotType.Awakening:
                reward.id = $"Awakening_{bossType}";
                reward.displayName = "Power of Awakening";
                reward.description = "All abilities are greatly enhanced.";
                reward.slotType = slotType;
                reward.value1 = 1.5f; // 전체 효과 150%
                reward.value2 = 0f;
                break;
        }
        
        return reward;
    }
    
    /// <summary>
    /// 보상 옵션들 정리
    /// </summary>
    private void ClearRewardOptions()
    {
        foreach (var option in currentRewardOptions)
        {
            if (option != null)
            {
                Destroy(option.gameObject);
            }
        }
        currentRewardOptions.Clear();
        selectedOption = null;
        
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
    }
    
    /// <summary>
    /// 옵션 선택 처리 (확인 버튼 방식)
    /// </summary>
    public void OnOptionSelected(BossRewardOptionUI selectedOptionUI)
    {
        if (!isRewardActive || selectedOptionUI == null) return;
        
        // 이전 선택 해제
        if (selectedOption != null)
        {
            selectedOption.SetSelected(false);
        }
        
        // 새로운 선택 설정
        selectedOption = selectedOptionUI;
        selectedOption.SetSelected(true);
        
        // 확인 버튼 활성화
        UpdateConfirmButton();
        
        PlayOptionSelectSound();
        
        Debug.Log($"[BossRewardManager] 보상 선택됨: {selectedOption.GetRewardOption().displayName}");
    }
    
    /// <summary>
    /// 확인 버튼 상태 업데이트
    /// </summary>
    private void UpdateConfirmButton()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = (selectedOption != null);
        }
    }
    
    
    /// <summary>
    /// 확인 버튼 클릭 처리 (LevelUpManager 방식)
    /// </summary>
    private void OnConfirmButtonClick()
    {
        if (!isRewardActive || selectedOption == null)
        {
            Debug.LogWarning("[BossRewardManager] 선택된 보상이 없습니다!");
            return;
        }
        
        BossRewardOption selectedReward = selectedOption.GetRewardOption();
        
        Debug.Log($"[BossRewardManager] 보상 적용 시작: {selectedReward.displayName}");
        
        // 사운드 재생
        PlayConfirmSound();
        
        // 보상 적용
        ApplyBossReward(selectedReward);
        
        // 축소 애니메이션과 함께 UI 닫기 (부모에서 실행)
        if (transform.parent != null)
        {
            MonoBehaviour parentMono = transform.parent.GetComponent<MonoBehaviour>();
            if (parentMono != null)
            {
                parentMono.StartCoroutine(AnimateRewardPanelClose());
            }
            else
            {
                StartCoroutine(AnimateRewardPanelClose());
            }
        }
        else
        {
            StartCoroutine(AnimateRewardPanelClose());
        }
    }
    
    /// <summary>
    /// 보상 패널 닫기 애니메이션 (LevelUpManager 방식)
    /// </summary>
    private System.Collections.IEnumerator AnimateRewardPanelClose()
    {
        Debug.Log("[BossRewardManager] Animation coroutine started");
        
        // Find panel through rewardOptionsParent (RewardContainer) and go up to RewardPanel
        RectTransform panelRect = null;
        if (rewardOptionsParent != null)
        {
            // RewardContainer -> RewardPanel
            Transform targetPanel = rewardOptionsParent.parent;
            if (targetPanel != null)
            {
                panelRect = targetPanel.GetComponent<RectTransform>();
                Debug.Log($"[BossRewardManager] Animation target panel found: {(panelRect != null ? panelRect.name : "null")}");
            }
            else
            {
                Debug.LogWarning("[BossRewardManager] RewardPanel parent not found!");
            }
        }
        else
        {
            Debug.LogWarning("[BossRewardManager] rewardOptionsParent is null!");
        }
        
        if (panelRect != null)
        {
            Vector3 startScale = panelRect.localScale;
            Vector3 endScale = Vector3.zero;
            float duration = 0.3f;
            float elapsed = 0f;
            
            Debug.Log($"[BossRewardManager] Animation started - startScale: {startScale}");
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                panelRect.localScale = Vector3.Lerp(startScale, endScale, t);
                
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            panelRect.localScale = endScale;
            Debug.Log("[BossRewardManager] Animation completed");
        }
        else
        {
            Debug.LogError("[BossRewardManager] Cannot find animation target panel!");
        }
        
        // 애니메이션 완료 후 직접 비활성화 처리
        isRewardActive = false;
        
        // 패널 스케일 원복 (다음 사용을 위해)
        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one;
        }
        
        // 보상 옵션들 정리
        ClearRewardOptions();
        
        // Resume game
        Time.timeScale = 1f;
        
        // Deactivate RewardPanel instead of entire GameObject
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
            Debug.Log("[BossRewardManager] RewardPanel deactivated after animation");
        }
        
        Debug.Log("[BossRewardManager] Boss reward UI animation completed and hidden");
    }
    
    /// <summary>
    /// 딜레이 후 UI 숨기기 (GameObject가 비활성일 때 대안)
    /// </summary>
    private System.Collections.IEnumerator DelayedHideRewardUI()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        
        // UI가 이미 활성화되어 있다면 애니메이션 실행
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateRewardPanelClose());
        }
        else
        {
            Debug.Log("[BossRewardManager] UI가 이미 비활성화되어 있음");
        }
    }
    
    /// <summary>
    /// 보상 적용
    /// </summary>
    private void ApplyBossReward(BossRewardOption reward)
    {
        // TODO: 실제 보상 효과 적용 로직 구현
        switch (reward.slotType)
        {
            case BossRewardSlotType.Trophy:
                Debug.Log($"[BossRewardManager] Trophy applied: {reward.displayName}");
                break;
                
            case BossRewardSlotType.Power:
                Debug.Log($"[BossRewardManager] Power applied: {reward.displayName}");
                break;
                
            case BossRewardSlotType.Awakening:
                Debug.Log($"[BossRewardManager] Awakening applied: {reward.displayName}");
                break;
        }
    }
    
    /// <summary>
    /// 개별 옵션 리롤
    /// </summary>
    public void RerollSingleOption(BossRewardOptionUI optionUI)
    {
        if (optionUI == null) return;
        
        BossRewardOption currentOption = optionUI.GetRewardOption();
        
        // 새로운 보상 생성 (같은 슬롯 타입으로)
        BossRewardOption newReward = GenerateRewardData(BossType.Fire, currentOption.slotType); // 임시로 Fire 사용
        
        // 옵션 교체
        optionUI.ReplaceWithNewOption(newReward);
        
        Debug.Log($"[BossRewardManager] 보상 리롤됨: {newReward.displayName}");
    }
    
    /// <summary>
    /// 보스 이름 반환
    /// </summary>
    private string GetBossName(BossType bossType)
    {
        switch (bossType)
        {
            case BossType.Fire: return "Fire Boss";
            case BossType.Ice: return "Ice Boss";
            case BossType.Lightning: return "Lightning Boss";
            case BossType.Shadow: return "Shadow Boss";
            case BossType.Earth: return "Earth Boss";
            default: return "Boss";
        }
    }
    
    /// <summary>
    /// 호버 사운드 재생
    /// </summary>
    public void PlayOptionHoverSound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }
    
    /// <summary>
    /// 선택 사운드 재생
    /// </summary>
    private void PlayOptionSelectSound()
    {
        if (audioSource != null && selectSound != null)
        {
            audioSource.PlayOneShot(selectSound);
        }
    }
    
    /// <summary>
    /// 확인 사운드 재생
    /// </summary>
    private void PlayConfirmSound()
    {
        if (audioSource != null && confirmSound != null)
        {
            audioSource.PlayOneShot(confirmSound);
        }
    }
    
    

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
        }
    }
}