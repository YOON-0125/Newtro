using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// 레벨업 UI 관리자 - 업그레이드 선택 화면 처리
/// </summary>
public class LevelUpManager : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpTitle;
    [SerializeField] private Transform upgradeOptionsContainer;
    [SerializeField] private GameObject upgradeOptionPrefab;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollButtonText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    
    [Header("배경")]
    [SerializeField] private GameObject backgroundOverlay;
    [SerializeField] private Image backgroundImage;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float panelAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve panelAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("사운드")]
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip optionSelectSound;
    [SerializeField] private AudioClip optionHoverSound;
    [SerializeField] private AudioClip rerollSound;
    
    // 이벤트
    [System.Serializable]
    public class LevelUpEvents
    {
        public UnityEvent OnLevelUpStart;
        public UnityEvent OnLevelUpEnd;
        public UnityEvent<string> OnUpgradeSelected;
    }
    
    [Header("이벤트")]
    [SerializeField] private LevelUpEvents events;
    
    // 내부 변수
    private List<UpgradeOptionUI> currentOptionUIs = new List<UpgradeOptionUI>();
    private UpgradeSystem upgradeSystem;
    private GameManager gameManager;
    private AudioSource audioSource;
    private bool isLevelUpActive = false;
    private int currentLevel = 1;
    private UpgradeOptionUI selectedOption = null;
    
    // 싱글톤
    public static LevelUpManager Instance { get; private set; }
    
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
    }
    
    private void Start()
    {
        InitializeReferences();
        SetupInitialState();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // AudioSource 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        
        // 이벤트 초기화
        if (events == null)
        {
            events = new LevelUpEvents();
        }
        
        // 배경 설정
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0, 0, 0, 0.8f); // 반투명 검정
        }
        
        // Reroll 버튼 설정
        if (rerollButton != null)
        {
            Debug.Log("[LevelUpManager] ✅ Reroll 버튼 리스너 등록됨");
            rerollButton.onClick.AddListener(OnRerollButtonClick);
        }
        else
        {
            Debug.LogWarning("[LevelUpManager] ❌ Reroll 버튼이 null입니다!");
        }
        
        // Confirm 버튼 설정
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        upgradeSystem = UpgradeSystem.Instance;
        gameManager = GameManager.Instance;
        
        if (upgradeSystem == null)
        {
            Debug.LogError("LevelUpManager: UpgradeSystem을 찾을 수 없습니다!");
        }
        
        if (gameManager == null)
        {
            Debug.LogError("LevelUpManager: GameManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    private void SetupInitialState()
    {
        // 레벨업 패널 숨김
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
        
        if (backgroundOverlay != null)
        {
            backgroundOverlay.SetActive(false);
        }
    }
    
    /// <summary>
    /// 레벨업 시작 (GameManager에서 호출)
    /// </summary>
    public void StartLevelUp(int newLevel)
    {
        if (isLevelUpActive) return;
        
        isLevelUpActive = true;
        currentLevel = newLevel;
        
        // 게임 일시정지
        Time.timeScale = 0f;
        
        // 커서 표시
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // 사운드 재생
        PlayLevelUpSound();
        
        // 업그레이드 옵션 생성
        GenerateUpgradeOptions(newLevel);
        
        // UI 표시
        ShowLevelUpPanel(newLevel);
        
        // 버튼들 활성화
        SetupButtons();
        
        // 이벤트 발생
        events?.OnLevelUpStart?.Invoke();
    }
    
    /// <summary>
    /// 업그레이드 옵션 생성
    /// </summary>
    private void GenerateUpgradeOptions(int currentLevel)
    {
        if (upgradeSystem == null) return;
        
        // 기존 옵션 UI 제거
        ClearCurrentOptions();
        
        // 새 옵션 생성
        List<UpgradeOption> options = upgradeSystem.GenerateUpgradeOptions(currentLevel);
        
        foreach (var option in options)
        {
            CreateUpgradeOptionUI(option);
        }
    }
    
    /// <summary>
    /// 업그레이드 옵션 UI 생성
    /// </summary>
    private void CreateUpgradeOptionUI(UpgradeOption option)
    {
        if (upgradeOptionPrefab == null || upgradeOptionsContainer == null)
        {
            Debug.LogError("LevelUpManager: 업그레이드 옵션 프리팹이나 컨테이너가 설정되지 않았습니다!");
            return;
        }
        
        // 프리팹 인스턴스 생성
        GameObject optionObj = Instantiate(upgradeOptionPrefab, upgradeOptionsContainer);
        UpgradeOptionUI optionUI = optionObj.GetComponent<UpgradeOptionUI>();
        
        if (optionUI == null)
        {
            optionUI = optionObj.AddComponent<UpgradeOptionUI>();
        }
        
        // 옵션 UI 설정
        optionUI.Initialize(option, this);
        currentOptionUIs.Add(optionUI);
    }
    
    /// <summary>
    /// 레벨업 패널 표시
    /// </summary>
    private void ShowLevelUpPanel(int newLevel)
    {
        if (backgroundOverlay != null)
        {
            backgroundOverlay.SetActive(true);
        }
        
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
            
            // 제목 설정
            if (levelUpTitle != null)
            {
                levelUpTitle.text = $"LEVEL UP! (Lv.{newLevel})";
            }
            
            // 패널 애니메이션
            StartCoroutine(AnimatePanelShow());
        }
    }
    
    /// <summary>
    /// 패널 표시 애니메이션
    /// </summary>
    private System.Collections.IEnumerator AnimatePanelShow()
    {
        if (levelUpPanel == null) yield break;
        
        RectTransform panelRect = levelUpPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;
        
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        
        panelRect.localScale = startScale;
        
        float elapsed = 0f;
        while (elapsed < panelAnimationDuration)
        {
            float t = elapsed / panelAnimationDuration;
            float curveValue = panelAnimationCurve.Evaluate(t);
            panelRect.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        panelRect.localScale = endScale;
    }
    
    /// <summary>
    /// 옵션 선택 처리 (UpgradeOptionUI에서 호출)
    /// </summary>
    public void OnOptionSelected(UpgradeOptionUI optionUI)
    {
        if (!isLevelUpActive || optionUI == null) return;
        
        // 이전 선택 해제
        if (selectedOption != null)
        {
            selectedOption.SetSelected(false);
        }
        
        // 새로운 선택
        selectedOption = optionUI;
        selectedOption.SetSelected(true);
        
        // 확정 버튼 활성화
        UpdateConfirmButton();
        
        // 사운드 재생
        PlayOptionHoverSound();
    }
    
    /// <summary>
    /// 확정 버튼 클릭 처리
    /// </summary>
    private void OnConfirmButtonClick()
    {
        if (!isLevelUpActive || selectedOption == null) return;
        
        // 사운드 재생
        PlayOptionSelectSound();
        
        // 업그레이드 적용
        UpgradeOption upgrade = selectedOption.GetUpgradeOption();
        if (upgradeSystem != null && upgrade != null)
        {
            upgradeSystem.ApplyUpgrade(upgrade.id);
        }
        
        // 이벤트 발생
        events?.OnUpgradeSelected?.Invoke(upgrade?.id ?? "");
        
        // 레벨업 종료
        EndLevelUp();
    }
    
    /// <summary>
    /// 레벨업 종료
    /// </summary>
    private void EndLevelUp()
    {
        if (!isLevelUpActive) return;
        
        isLevelUpActive = false;
        
        // 패널 숨김
        StartCoroutine(AnimatePanelHide());
    }
    
    /// <summary>
    /// 패널 숨김 애니메이션
    /// </summary>
    private System.Collections.IEnumerator AnimatePanelHide()
    {
        if (levelUpPanel == null) yield break;
        
        RectTransform panelRect = levelUpPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < panelAnimationDuration)
            {
                float t = elapsed / panelAnimationDuration;
                float curveValue = panelAnimationCurve.Evaluate(t);
                panelRect.localScale = Vector3.Lerp(startScale, endScale, curveValue);
                
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            panelRect.localScale = endScale;
        }
        
        // UI 숨김
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
        
        if (backgroundOverlay != null)
        {
            backgroundOverlay.SetActive(false);
        }
        
        // 옵션 UI 정리
        ClearCurrentOptions();
        
        // 게임 재개
        Time.timeScale = 1f;
        
        // 커서 숨김
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // 이벤트 발생
        events?.OnLevelUpEnd?.Invoke();
    }
    
    /// <summary>
    /// 현재 옵션 UI 정리
    /// </summary>
    private void ClearCurrentOptions()
    {
        foreach (var optionUI in currentOptionUIs)
        {
            if (optionUI != null && optionUI.gameObject != null)
            {
                Destroy(optionUI.gameObject);
            }
        }
        currentOptionUIs.Clear();
    }
    
    /// <summary>
    /// 사운드 재생 메서드들
    /// </summary>
    private void PlayLevelUpSound()
    {
        if (audioSource != null && levelUpSound != null)
        {
            audioSource.PlayOneShot(levelUpSound);
        }
    }
    
    public void PlayOptionSelectSound()
    {
        if (audioSource != null && optionSelectSound != null)
        {
            audioSource.PlayOneShot(optionSelectSound);
        }
    }
    
    public void PlayOptionHoverSound()
    {
        if (audioSource != null && optionHoverSound != null)
        {
            audioSource.PlayOneShot(optionHoverSound);
        }
    }
    
    private void PlayRerollSound()
    {
        if (audioSource != null && rerollSound != null)
        {
            audioSource.PlayOneShot(rerollSound);
        }
    }
    
    /// <summary>
    /// 강제 레벨업 종료 (디버그용)
    /// </summary>
    public void ForceEndLevelUp()
    {
        if (isLevelUpActive)
        {
            EndLevelUp();
        }
    }
    
    /// <summary>
    /// 레벨업 활성 상태 확인
    /// </summary>
    public bool IsLevelUpActive()
    {
        return isLevelUpActive;
    }
    
    /// <summary>
    /// 버튼들 설정
    /// </summary>
    private void SetupButtons()
    {
        // Reroll 버튼 설정
        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = true;
        }
        
        if (rerollButtonText != null)
        {
            rerollButtonText.text = "Reroll";
        }
        
        // Confirm 버튼 설정
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false; // 처음에는 비활성화
        }
        
        if (confirmButtonText != null)
        {
            confirmButtonText.text = "Confirm";
        }
        
        // 선택 초기화
        selectedOption = null;
    }
    
    /// <summary>
    /// 확정 버튼 상태 업데이트
    /// </summary>
    private void UpdateConfirmButton()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = (selectedOption != null);
        }
    }
    
    /// <summary>
    /// Reroll 버튼 클릭 이벤트
    /// </summary>
    private void OnRerollButtonClick()
    {
        Debug.Log("[LevelUpManager] 🎲 Reroll 버튼 클릭됨!");
        
        if (!isLevelUpActive) 
        {
            Debug.LogWarning("[LevelUpManager] ❌ 레벨업이 활성 상태가 아님!");
            return;
        }
        
        Debug.Log("[LevelUpManager] ✅ Reroll 실행 중...");
        
        // 사운드 재생
        PlayRerollSound();
        
        // 새로운 옵션 생성
        RegenerateUpgradeOptions();
    }
    
    /// <summary>
    /// 업그레이드 옵션 재생성
    /// </summary>
    private void RegenerateUpgradeOptions()
    {
        if (upgradeSystem == null) return;
        
        // 기존 옵션 UI 제거
        ClearCurrentOptions();
        
        // 선택 상태 초기화
        selectedOption = null;
        UpdateConfirmButton();
        
        // 새 옵션 생성 (이전 옵션들 제외)
        List<UpgradeOption> newOptions = upgradeSystem.GenerateNewUpgradeOptions(currentLevel);
        
        foreach (var option in newOptions)
        {
            CreateUpgradeOptionUI(option);
        }
        
        Debug.Log($"[LevelUpManager] Reroll 완료 - 새로운 {newOptions.Count}개 옵션 생성");
    }
    
    /// <summary>
    /// 개별 옵션 리롤
    /// </summary>
    public void RerollSingleOption(UpgradeOptionUI optionToReroll)
    {
        if (upgradeSystem == null || optionToReroll == null) return;
        
        Debug.Log($"[LevelUpManager] 🎲 개별 옵션 리롤 시작");
        
        // 현재 옵션 가져오기
        UpgradeOption currentOption = optionToReroll.GetUpgradeOption();
        if (currentOption == null) return;
        
        // 현재 화면에 표시된 모든 옵션들 수집
        List<UpgradeOption> currentDisplayedOptions = new List<UpgradeOption>();
        foreach (var optionUI in currentOptionUIs)
        {
            if (optionUI != null && optionUI.GetUpgradeOption() != null)
            {
                currentDisplayedOptions.Add(optionUI.GetUpgradeOption());
            }
        }
        
        Debug.Log($"[LevelUpManager] 📋 현재 표시된 옵션 수: {currentDisplayedOptions.Count}");
        Debug.Log($"[LevelUpManager] 🚫 제외할 옵션들: {string.Join(", ", currentDisplayedOptions.ConvertAll(o => o.displayName))}");
        
        // 새로운 옵션 생성 (현재 옵션 제외)
        UpgradeOption newOption = upgradeSystem.GenerateSingleNewOption(currentLevel, currentOption.id);
        
        if (newOption != null)
        {
            // 옵션 교체
            optionToReroll.ReplaceWithNewOption(newOption);
            
            // 선택 상태 해제 (리롤된 옵션은 선택 해제)
            if (selectedOption == optionToReroll)
            {
                selectedOption = null;
                optionToReroll.SetSelected(false);
                UpdateConfirmButton();
            }
            
            Debug.Log($"[LevelUpManager] ✅ 개별 리롤 완료: {currentOption.displayName} → {newOption.displayName}");
        }
        else
        {
            Debug.LogWarning($"[LevelUpManager] ❌ 새로운 옵션 생성 실패");
        }
    }
}