using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보스 전투 시 나타나는 UI (이름, 체력바, 상태효과)
/// </summary>
public class BossUI : MonoBehaviour
{
    [Header("보스 UI 요소")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText; // "1250/2000" 형태
    [SerializeField] private Image bossIcon; // 보스 아이콘 (선택사항)
    
    [Header("상태효과 영역")]
    [SerializeField] private Transform statusEffectContainer; // 상태효과 아이콘들의 부모
    [SerializeField] private GameObject fireStatusIconPrefab; // 화염 상태효과 아이콘 프리팹
    [SerializeField] private GameObject iceStatusIconPrefab; // 얼음 상태효과 아이콘 프리팹
    [SerializeField] private GameObject lightningStatusIconPrefab; // 번개 상태효과 아이콘 프리팹
    [SerializeField] private int maxStatusEffectIcons = 8; // 최대 표시할 상태효과 수
    
    [Header("애니메이션 설정")]
    [SerializeField] private bool enableHealthBarAnimation = true;
    [SerializeField] private float healthBarAnimationSpeed = 2f;
    [SerializeField] private bool enablePulseOnDamage = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    
    [Header("스타일 설정")]
    [SerializeField] private Color defaultHealthBarColor = Color.green;
    [SerializeField] private Color lowHealthBarColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30% 이하에서 빨간색
    
    // 현재 보스 참조
    private BossBase currentBoss;
    
    // 체력바 애니메이션
    private float targetHealthPercentage = 1f;
    private float currentHealthPercentage = 1f;
    
    // 상태효과 아이콘 관리
    private List<GameObject> activeStatusIcons = new List<GameObject>();
    private Dictionary<StatusType, GameObject> statusIconMap = new Dictionary<StatusType, GameObject>();
    
    // 캐시된 값들 (불필요한 업데이트 방지)
    private float lastHealth = -1f;
    private float lastMaxHealth = -1f;
    private string lastBossName = "";
    
    private void Update()
    {
        if (currentBoss != null)
        {
            UpdateBossUI();
            UpdateHealthBarAnimation();
        }
    }
    
    /// <summary>
    /// 보스 설정 및 UI 초기화
    /// </summary>
    public void SetBoss(BossBase boss)
    {
        if (boss == null)
        {
            Debug.LogError("[BossUI] 보스가 null입니다!");
            return;
        }
        
        currentBoss = boss;
        
        // 초기 UI 설정
        InitializeBossUI();
        
        // 보스 이벤트 구독
        SubscribeToBossEvents();
        
        Debug.Log($"[BossUI] 보스 UI 설정 완료: {boss.BossName}");
    }
    
    /// <summary>
    /// 보스 UI 클리어
    /// </summary>
    public void ClearBoss()
    {
        if (currentBoss != null)
        {
            UnsubscribeFromBossEvents();
        }
        
        currentBoss = null;
        ClearStatusEffects();
        
        // UI 초기화
        if (bossNameText != null) bossNameText.text = "";
        if (healthSlider != null) healthSlider.value = 0f;
        if (healthText != null) healthText.text = "";
        if (bossIcon != null) bossIcon.sprite = null;
        
        Debug.Log("[BossUI] 보스 UI 클리어 완료");
    }
    
    /// <summary>
    /// 보스 UI 초기화
    /// </summary>
    private void InitializeBossUI()
    {
        if (currentBoss == null) return;
        
        // 보스 이름 설정
        if (bossNameText != null)
        {
            bossNameText.text = currentBoss.BossName;
            lastBossName = currentBoss.BossName;
        }
        
        // 체력바 초기화
        if (healthSlider != null)
        {
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            targetHealthPercentage = 1f;
            currentHealthPercentage = 1f;
        }
        
        // 보스 아이콘 설정 (있다면)
        if (bossIcon != null && currentBoss.BossIcon != null)
        {
            bossIcon.sprite = currentBoss.BossIcon;
        }
        
        // 초기 체력 정보 업데이트
        UpdateHealthDisplay();
        
        // 상태효과 초기화
        ClearStatusEffects();
    }
    
    /// <summary>
    /// 보스 UI 업데이트 (매 프레임)
    /// </summary>
    private void UpdateBossUI()
    {
        if (currentBoss == null) return;
        
        // 체력 변화 체크
        float currentHealth = currentBoss.Health;
        float maxHealth = currentBoss.MaxHealth;
        
        if (!Mathf.Approximately(lastHealth, currentHealth) || 
            !Mathf.Approximately(lastMaxHealth, maxHealth))
        {
            UpdateHealthDisplay();
            
            // 데미지 받은 경우 펄스 효과
            if (enablePulseOnDamage && currentHealth < lastHealth)
            {
                StartCoroutine(DamagePulseEffect());
            }
            
            lastHealth = currentHealth;
            lastMaxHealth = maxHealth;
        }
        
        // 상태효과 업데이트
        UpdateStatusEffects();
    }
    
    /// <summary>
    /// 체력 표시 업데이트
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (currentBoss == null) return;
        
        float healthPercentage = currentBoss.HealthPercentage;
        targetHealthPercentage = healthPercentage;
        
        // 체력 텍스트 업데이트
        if (healthText != null)
        {
            int currentHealth = Mathf.CeilToInt(currentBoss.Health);
            int maxHealth = Mathf.CeilToInt(currentBoss.MaxHealth);
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
        
        // 체력바 색상 업데이트
        UpdateHealthBarColor(healthPercentage);
    }
    
    /// <summary>
    /// 체력바 애니메이션 업데이트
    /// </summary>
    private void UpdateHealthBarAnimation()
    {
        if (healthSlider == null || !enableHealthBarAnimation) return;
        
        if (!Mathf.Approximately(currentHealthPercentage, targetHealthPercentage))
        {
            currentHealthPercentage = Mathf.MoveTowards(
                currentHealthPercentage, 
                targetHealthPercentage, 
                healthBarAnimationSpeed * Time.deltaTime
            );
            
            healthSlider.value = currentHealthPercentage;
        }
    }
    
    /// <summary>
    /// 체력바 색상 업데이트
    /// </summary>
    private void UpdateHealthBarColor(float healthPercentage)
    {
        if (healthSlider == null) return;
        
        Image fillImage = healthSlider.fillRect?.GetComponent<Image>();
        if (fillImage == null) return;
        
        // 체력 비율에 따른 색상 변경
        if (healthPercentage <= lowHealthThreshold)
        {
            fillImage.color = lowHealthBarColor;
        }
        else
        {
            // 체력이 높을 때 점진적 색상 변화
            float t = (healthPercentage - lowHealthThreshold) / (1f - lowHealthThreshold);
            fillImage.color = Color.Lerp(lowHealthBarColor, defaultHealthBarColor, t);
        }
    }
    
    /// <summary>
    /// 상태효과 아이콘 업데이트
    /// </summary>
    private void UpdateStatusEffects()
    {
        if (currentBoss == null || statusEffectContainer == null) return;
        
        // StatusController에서 현재 상태효과 목록 가져오기
        StatusController statusController = currentBoss.GetComponent<StatusController>();
        if (statusController == null) return;
        
        // 현재 활성 상태효과 목록 가져오기
        var activeEffects = statusController.GetActiveStatusEffects();
        
        // 현재 UI에 표시된 상태효과 중 더 이상 활성화되지 않은 것들 제거
        var typesToRemove = new List<StatusType>();
        foreach (var pair in statusIconMap)
        {
            StatusType type = pair.Key;
            if (!activeEffects.ContainsKey(type))
            {
                typesToRemove.Add(type);
            }
        }
        
        foreach (var type in typesToRemove)
        {
            UpdateStatusIcon(type, false, 0f);
        }
        
        // 활성 상태효과들에 대해 아이콘 업데이트/생성
        foreach (var effect in activeEffects.Values)
        {
            UpdateStatusIcon(effect.type, true, effect.remainingDuration, effect.stacks);
        }
    }
    
    /// <summary>
    /// 특정 상태효과 아이콘 업데이트
    /// </summary>
    private void UpdateStatusIcon(StatusType statusType, bool isActive, float duration, int stacks = 1)
    {
        GameObject iconObj;
        
        if (statusIconMap.TryGetValue(statusType, out iconObj))
        {
            if (!isActive)
            {
                // 상태효과 제거
                statusIconMap.Remove(statusType);
                activeStatusIcons.Remove(iconObj);
                Destroy(iconObj);
            }
            else
            {
                // 지속시간 및 스택 업데이트
                StatusEffectIcon iconScript = iconObj.GetComponent<StatusEffectIcon>();
                if (iconScript != null)
                {
                    iconScript.UpdateDuration(duration, stacks);
                }
            }
        }
        else if (isActive)
        {
            // 새 상태효과 아이콘 생성
            CreateStatusIcon(statusType, duration, stacks);
        }
    }
    
    /// <summary>
    /// 상태효과 아이콘 생성
    /// </summary>
    private void CreateStatusIcon(StatusType statusType, float duration, int stacks = 1)
    {
        if (activeStatusIcons.Count >= maxStatusEffectIcons)
        {
            Debug.LogWarning("[BossUI] 최대 상태효과 아이콘 수에 도달했습니다!");
            return;
        }
        
        if (statusEffectContainer == null)
        {
            Debug.LogWarning("[BossUI] 상태효과 컨테이너가 설정되지 않았습니다!");
            return;
        }
        
        // 상태효과 타입에 따라 적절한 프리팹 선택
        GameObject prefabToUse = GetStatusIconPrefab(statusType);
        if (prefabToUse == null)
        {
            Debug.LogWarning($"[BossUI] {statusType}에 해당하는 상태효과 아이콘 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        GameObject iconObj = Instantiate(prefabToUse, statusEffectContainer);
        StatusEffectIcon iconScript = iconObj.GetComponent<StatusEffectIcon>();
        
        if (iconScript != null)
        {
            iconScript.Initialize(statusType, duration, stacks);
        }
        
        activeStatusIcons.Add(iconObj);
        statusIconMap[statusType] = iconObj;
    }
    
    /// <summary>
    /// 상태효과 타입에 따른 프리팹 반환
    /// </summary>
    private GameObject GetStatusIconPrefab(StatusType statusType)
    {
        switch (statusType)
        {
            case StatusType.Fire:
                return fireStatusIconPrefab;
            case StatusType.Ice:
                return iceStatusIconPrefab;
            case StatusType.Lightning:
                return lightningStatusIconPrefab;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 모든 상태효과 아이콘 제거
    /// </summary>
    private void ClearStatusEffects()
    {
        foreach (GameObject icon in activeStatusIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        
        activeStatusIcons.Clear();
        statusIconMap.Clear();
    }
    
    /// <summary>
    /// 데미지 펄스 효과
    /// </summary>
    private IEnumerator DamagePulseEffect()
    {
        if (healthSlider == null) yield break;
        
        Image fillImage = healthSlider.fillRect?.GetComponent<Image>();
        if (fillImage == null) yield break;
        
        Color originalColor = fillImage.color;
        
        // 빠르게 빨간색으로 변경
        fillImage.color = damageFlashColor;
        yield return new WaitForSeconds(0.1f);
        
        // 원래 색상으로 복원
        fillImage.color = originalColor;
    }
    
    /// <summary>
    /// 보스 이벤트 구독
    /// </summary>
    private void SubscribeToBossEvents()
    {
        if (currentBoss == null) return;
        
        // TODO: BossBase에 이벤트가 추가되면 구독
        // currentBoss.OnHealthChanged += OnBossHealthChanged;
        // currentBoss.OnStatusEffectChanged += OnBossStatusEffectChanged;
    }
    
    /// <summary>
    /// 보스 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromBossEvents()
    {
        if (currentBoss == null) return;
        
        // TODO: BossBase에 이벤트가 추가되면 구독 해제
        // currentBoss.OnHealthChanged -= OnBossHealthChanged;
        // currentBoss.OnStatusEffectChanged -= OnBossStatusEffectChanged;
    }
    
    /// <summary>
    /// UI 컴포넌트 유효성 검사
    /// </summary>
    private void ValidateComponents()
    {
        if (bossNameText == null)
            Debug.LogWarning("[BossUI] bossNameText가 설정되지 않았습니다!");
            
        if (healthSlider == null)
            Debug.LogWarning("[BossUI] healthSlider가 설정되지 않았습니다!");
            
        if (healthText == null)
            Debug.LogWarning("[BossUI] healthText가 설정되지 않았습니다!");
    }
    
    private void Start()
    {
        ValidateComponents();
    }
    
    private void OnDestroy()
    {
        ClearBoss();
    }
}


