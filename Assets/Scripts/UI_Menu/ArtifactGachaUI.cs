using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 유물 가챠 UI
/// </summary>
public class ArtifactGachaUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button gachaButton;
    [SerializeField] private TextMeshProUGUI gachaCostText;
    [SerializeField] private TextMeshProUGUI goldText;
    
    [Header("가챠 결과 UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Image artifactIcon;
    [SerializeField] private TextMeshProUGUI artifactNameText;
    [SerializeField] private TextMeshProUGUI artifactDescText;
    [SerializeField] private TextMeshProUGUI artifactRarityText;
    [SerializeField] private Button resultCloseButton;
    
    [Header("확률 표시 UI")]
    [SerializeField] private Transform probabilityListParent;
    [SerializeField] private TextMeshProUGUI commonProbText;
    [SerializeField] private TextMeshProUGUI rareProbText;
    [SerializeField] private TextMeshProUGUI epicProbText;
    [SerializeField] private TextMeshProUGUI legendaryProbText;
    
    [Header("통계 UI")]
    [SerializeField] private TextMeshProUGUI totalArtifactCountText;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float gachaAnimationDuration = 2f;
    [SerializeField] private string gachaAnimationText = "가챠 중...";
    [SerializeField] private AnimationCurve gachaAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("등급별 색상")]
    [SerializeField] private Color commonColor = Color.gray;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    [Header("UI 텍스트 포맷")]
    [SerializeField] private string goldFormat = "보유 골드: {0}";
    [SerializeField] private string costFormat = "{0}골드";
    [SerializeField] private string countFormat = "보유 유물: {0}개";
    [SerializeField] private string probFormat = "{0}: {1}%";
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 참조
    private ArtifactGachaSystem gachaSystem;
    private GoldSystem goldSystem;
    private MainMenuManager menuManager;
    
    // 애니메이션 상태
    private bool isGachaAnimationPlaying = false;
    
    private void Start()
    {
        InitializeReferences();
        SetupUI();
        SubscribeToEvents();
        UpdateUI();
    }
    
    private void OnEnable()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        gachaSystem = ArtifactGachaSystem.Instance;
        goldSystem = GoldSystem.Instance;
        menuManager = FindObjectOfType<MainMenuManager>();
        
        if (gachaSystem == null)
        {
            Debug.LogError("[ArtifactGachaUI] ArtifactGachaSystem을 찾을 수 없습니다!");
        }
        
        if (goldSystem == null)
        {
            Debug.LogError("[ArtifactGachaUI] GoldSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// UI 설정
    /// </summary>
    private void SetupUI()
    {
        // 뒤로가기 버튼
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                if (menuManager != null)
                {
                    menuManager.ShowMainMenu();
                }
            });
        }
        
        // 가챠 버튼
        if (gachaButton != null)
        {
            gachaButton.onClick.AddListener(OnGachaButtonClicked);
        }
        
        // 결과 패널 닫기 버튼
        if (resultCloseButton != null)
        {
            resultCloseButton.onClick.AddListener(CloseResultPanel);
        }
        
        // 결과 패널 초기 상태: 비활성화
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 가챠 버튼 클릭 처리
    /// </summary>
    private void OnGachaButtonClicked()
    {
        if (isGachaAnimationPlaying)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[ArtifactGachaUI] 가챠 애니메이션 재생 중입니다.");
            }
            return;
        }
        
        if (gachaSystem == null || !gachaSystem.CanPullGacha())
        {
            if (enableDebugLogs)
            {
                Debug.Log("[ArtifactGachaUI] 가챠 실행 불가 (골드 부족 등)");
            }
            return;
        }
        
        StartCoroutine(PlayGachaAnimationCoroutine());
    }
    
    /// <summary>
    /// 가챠 애니메이션 코루틴
    /// </summary>
    private IEnumerator PlayGachaAnimationCoroutine()
    {
        isGachaAnimationPlaying = true;
        
        // 가챠 버튼 비활성화
        if (gachaButton != null)
        {
            gachaButton.interactable = false;
        }
        
        // 애니메이션 텍스트 표시
        if (gachaCostText != null)
        {
            string originalText = gachaCostText.text;
            
            float elapsed = 0f;
            while (elapsed < gachaAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / gachaAnimationDuration;
                
                // 텍스트 애니메이션 (점 개수 변화)
                int dotCount = Mathf.FloorToInt(t * 3) + 1;
                string dots = new string('.', dotCount);
                gachaCostText.text = gachaAnimationText + dots;
                
                yield return null;
            }
            
            gachaCostText.text = originalText;
        }
        else
        {
            yield return new WaitForSecondsRealtime(gachaAnimationDuration);
        }
        
        // 실제 가챠 실행
        Artifact result = gachaSystem.PullGacha();
        
        if (result != null)
        {
            ShowGachaResult(result);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[ArtifactGachaUI] 🎁 가챠 결과: {result.displayName} ({result.rarity})");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log("[ArtifactGachaUI] ❌ 가챠 실패");
            }
        }
        
        // UI 업데이트
        UpdateUI();
        
        // 가챠 버튼 재활성화
        if (gachaButton != null)
        {
            gachaButton.interactable = gachaSystem != null && gachaSystem.CanPullGacha();
        }
        
        isGachaAnimationPlaying = false;
    }
    
    /// <summary>
    /// 가챠 결과 표시
    /// </summary>
    /// <param name="artifact">획득한 유물</param>
    private void ShowGachaResult(Artifact artifact)
    {
        if (resultPanel == null) return;
        
        // 결과 패널 활성화
        resultPanel.SetActive(true);
        
        // 유물 정보 표시
        if (artifactNameText != null)
        {
            artifactNameText.text = artifact.displayName;
        }
        
        if (artifactDescText != null)
        {
            artifactDescText.text = artifact.description;
        }
        
        if (artifactRarityText != null)
        {
            artifactRarityText.text = GetRarityText(artifact.rarity);
            artifactRarityText.color = GetRarityColor(artifact.rarity);
        }
        
        // 유물 아이콘 (있다면)
        if (artifactIcon != null)
        {
            if (artifact.icon != null)
            {
                artifactIcon.sprite = artifact.icon;
                artifactIcon.color = Color.white;
            }
            else
            {
                // 기본 아이콘 (등급별 색상)
                artifactIcon.sprite = null;
                artifactIcon.color = GetRarityColor(artifact.rarity);
            }
        }
        
        // 결과 패널 배경 색상도 등급에 맞게 설정
        Image resultBg = resultPanel.GetComponent<Image>();
        if (resultBg != null)
        {
            Color bgColor = GetRarityColor(artifact.rarity);
            bgColor.a = 0.3f; // 반투명
            resultBg.color = bgColor;
        }
    }
    
    /// <summary>
    /// 결과 패널 닫기
    /// </summary>
    private void CloseResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        UpdateGoldDisplay();
        UpdateGachaCostDisplay();
        UpdateProbabilityDisplay();
        UpdateStatisticsDisplay();
        UpdateGachaButtonState();
    }
    
    /// <summary>
    /// 골드 표시 업데이트
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldText != null && goldSystem != null)
        {
            goldText.text = string.Format(goldFormat, goldSystem.CurrentGold.ToString("N0"));
        }
    }
    
    /// <summary>
    /// 가챠 비용 표시 업데이트
    /// </summary>
    private void UpdateGachaCostDisplay()
    {
        if (gachaCostText != null && gachaSystem != null)
        {
            gachaCostText.text = string.Format(costFormat, gachaSystem.GetGachaCost().ToString("N0"));
        }
    }
    
    /// <summary>
    /// 확률 표시 업데이트
    /// </summary>
    private void UpdateProbabilityDisplay()
    {
        if (gachaSystem == null) return;
        
        // Inspector에서 설정한 확률값들을 가져와서 표시
        if (commonProbText != null)
        {
            commonProbText.text = string.Format(probFormat, "일반", "60"); // 하드코딩 대신 gachaSystem에서 가져올 수 있도록 개선 필요
            commonProbText.color = commonColor;
        }
        
        if (rareProbText != null)
        {
            rareProbText.text = string.Format(probFormat, "레어", "25");
            rareProbText.color = rareColor;
        }
        
        if (epicProbText != null)
        {
            epicProbText.text = string.Format(probFormat, "에픽", "12");
            epicProbText.color = epicColor;
        }
        
        if (legendaryProbText != null)
        {
            legendaryProbText.text = string.Format(probFormat, "전설", "3");
            legendaryProbText.color = legendaryColor;
        }
    }
    
    /// <summary>
    /// 통계 표시 업데이트
    /// </summary>
    private void UpdateStatisticsDisplay()
    {
        if (totalArtifactCountText != null && gachaSystem != null)
        {
            int count = gachaSystem.GetOwnedArtifactCount();
            totalArtifactCountText.text = string.Format(countFormat, count);
        }
    }
    
    /// <summary>
    /// 가챠 버튼 상태 업데이트
    /// </summary>
    private void UpdateGachaButtonState()
    {
        if (gachaButton != null && gachaSystem != null)
        {
            bool canPull = gachaSystem.CanPullGacha() && !isGachaAnimationPlaying;
            gachaButton.interactable = canPull;
            
            // 버튼 색상도 상태에 따라 변경
            ColorBlock colors = gachaButton.colors;
            colors.normalColor = canPull ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.6f, 0.2f, 0.2f);
            gachaButton.colors = colors;
        }
    }
    
    /// <summary>
    /// 등급별 색상 반환
    /// </summary>
    /// <param name="rarity">유물 등급</param>
    /// <returns>등급 색상</returns>
    private Color GetRarityColor(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common => commonColor,
            ArtifactRarity.Rare => rareColor,
            ArtifactRarity.Epic => epicColor,
            ArtifactRarity.Legendary => legendaryColor,
            _ => Color.white
        };
    }
    
    /// <summary>
    /// 등급 텍스트 반환
    /// </summary>
    /// <param name="rarity">유물 등급</param>
    /// <returns>등급 텍스트</returns>
    private string GetRarityText(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common => "일반",
            ArtifactRarity.Rare => "레어",
            ArtifactRarity.Epic => "에픽",
            ArtifactRarity.Legendary => "전설",
            _ => "알 수 없음"
        };
    }
    
    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (goldSystem != null)
        {
            goldSystem.OnGoldChanged += OnGoldChanged;
        }
        
        if (gachaSystem != null)
        {
            gachaSystem.OnArtifactObtained += OnArtifactObtained;
        }
    }
    
    /// <summary>
    /// 골드 변경 이벤트 처리
    /// </summary>
    /// <param name="newGoldAmount">새로운 골드량</param>
    private void OnGoldChanged(int newGoldAmount)
    {
        UpdateGoldDisplay();
        UpdateGachaButtonState();
    }
    
    /// <summary>
    /// 유물 획득 이벤트 처리
    /// </summary>
    /// <param name="artifact">획득한 유물</param>
    private void OnArtifactObtained(Artifact artifact)
    {
        UpdateStatisticsDisplay();
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
        
        if (gachaSystem != null)
        {
            gachaSystem.OnArtifactObtained -= OnArtifactObtained;
        }
    }
}