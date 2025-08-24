using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유물 등급
/// </summary>
public enum ArtifactRarity
{
    Common,    // 일반 (회색)
    Rare,      // 레어 (파란색)
    Epic,      // 에픽 (보라색)
    Legendary  // 전설 (황금색)
}

/// <summary>
/// 유물 데이터
/// </summary>
[System.Serializable]
public class Artifact
{
    [Header("기본 정보")]
    public int id;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public ArtifactRarity rarity;
    
    [Header("효과")]
    public PermanentUpgradeType upgradeType;
    public float effectValue;           // 효과값 (예: 0.15f = 15%)
    public bool isPercentage = true;   // 퍼센트 효과인지
    
    [Header("시각 효과")]
    public Color rarityColor = Color.white;
    public Sprite icon;
}

/// <summary>
/// 유물 가챠 시스템 - 골드로 랜덤 유물 획득
/// </summary>
public class ArtifactGachaSystem : MonoBehaviour
{
    [Header("가챠 설정")]
    [SerializeField] private int gachaCost = 5000;
    [SerializeField] [Range(0f, 100f)] private float commonChance = 60f;
    [SerializeField] [Range(0f, 100f)] private float rareChance = 25f;
    [SerializeField] [Range(0f, 100f)] private float epicChance = 12f;
    [SerializeField] [Range(0f, 100f)] private float legendaryChance = 3f;
    
    [Header("유물 데이터베이스")]
    [SerializeField] private List<Artifact> commonArtifacts = new List<Artifact>();
    [SerializeField] private List<Artifact> rareArtifacts = new List<Artifact>();
    [SerializeField] private List<Artifact> epicArtifacts = new List<Artifact>();
    [SerializeField] private List<Artifact> legendaryArtifacts = new List<Artifact>();
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showProbabilityCheck = false;
    
    // 싱글톤
    public static ArtifactGachaSystem Instance { get; private set; }
    
    // 참조
    private GoldSystem goldSystem;
    
    // 보유 유물 (PlayerPrefs에 저장)
    private List<int> ownedArtifactIds = new List<int>();
    private const string ARTIFACTS_SAVE_KEY = "OwnedArtifacts";
    
    // 이벤트
    public System.Action<Artifact> OnArtifactObtained;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeArtifacts();
        LoadOwnedArtifacts();
    }
    
    private void Start()
    {
        InitializeReferences();
        ValidateProbabilities();
    }
    
    /// <summary>
    /// 기본 유물 데이터 초기화
    /// </summary>
    private void InitializeArtifacts()
    {
        // 일반 유물들
        if (commonArtifacts.Count == 0)
        {
            commonArtifacts.AddRange(new List<Artifact>
            {
                new Artifact 
                { 
                    id = 1001, displayName = "낡은 검", 
                    description = "오래된 검이지만 여전히 예리하다.\n데미지가 5% 증가한다.",
                    rarity = ArtifactRarity.Common,
                    upgradeType = PermanentUpgradeType.Damage,
                    effectValue = 0.05f, isPercentage = true,
                    rarityColor = Color.gray
                },
                new Artifact 
                { 
                    id = 1002, displayName = "가벼운 신발", 
                    description = "가벼운 소재로 만들어진 신발.\n이동속도가 8% 증가한다.",
                    rarity = ArtifactRarity.Common,
                    upgradeType = PermanentUpgradeType.MoveSpeed,
                    effectValue = 0.08f, isPercentage = true,
                    rarityColor = Color.gray
                },
                new Artifact 
                { 
                    id = 1003, displayName = "작은 체력 포션", 
                    description = "작지만 효과적인 체력 포션.\n최대 체력이 증가한다.",
                    rarity = ArtifactRarity.Common,
                    upgradeType = PermanentUpgradeType.MaxHealth,
                    effectValue = 2f, isPercentage = false,
                    rarityColor = Color.gray
                }
            });
        }
        
        // 레어 유물들
        if (rareArtifacts.Count == 0)
        {
            rareArtifacts.AddRange(new List<Artifact>
            {
                new Artifact 
                { 
                    id = 2001, displayName = "예리한 검", 
                    description = "잘 벼려진 검날이 빛난다.\n데미지가 12% 증가한다.",
                    rarity = ArtifactRarity.Rare,
                    upgradeType = PermanentUpgradeType.Damage,
                    effectValue = 0.12f, isPercentage = true,
                    rarityColor = Color.blue
                },
                new Artifact 
                { 
                    id = 2002, displayName = "바람의 신발", 
                    description = "바람의 축복이 깃든 신발.\n이동속도가 15% 증가한다.",
                    rarity = ArtifactRarity.Rare,
                    upgradeType = PermanentUpgradeType.MoveSpeed,
                    effectValue = 0.15f, isPercentage = true,
                    rarityColor = Color.blue
                },
                new Artifact 
                { 
                    id = 2003, displayName = "지혜의 서", 
                    description = "고대의 지혜가 담긴 책.\n경험치 획득량이 20% 증가한다.",
                    rarity = ArtifactRarity.Rare,
                    upgradeType = PermanentUpgradeType.ExpMultiplier,
                    effectValue = 0.20f, isPercentage = true,
                    rarityColor = Color.blue
                }
            });
        }
        
        // 에픽 유물들
        if (epicArtifacts.Count == 0)
        {
            epicArtifacts.AddRange(new List<Artifact>
            {
                new Artifact 
                { 
                    id = 3001, displayName = "마검 듀랜달", 
                    description = "전설적인 기사의 검.\n데미지가 25% 증가한다.",
                    rarity = ArtifactRarity.Epic,
                    upgradeType = PermanentUpgradeType.Damage,
                    effectValue = 0.25f, isPercentage = true,
                    rarityColor = Color.magenta
                },
                new Artifact 
                { 
                    id = 3002, displayName = "생명의 목걸이", 
                    description = "생명력이 넘치는 보석 목걸이.\n최대 체력이 2하트 증가한다.",
                    rarity = ArtifactRarity.Epic,
                    upgradeType = PermanentUpgradeType.MaxHealth,
                    effectValue = 8f, isPercentage = false,
                    rarityColor = Color.magenta
                }
            });
        }
        
        // 전설 유물들
        if (legendaryArtifacts.Count == 0)
        {
            legendaryArtifacts.AddRange(new List<Artifact>
            {
                new Artifact 
                { 
                    id = 4001, displayName = "엑스칼리버", 
                    description = "왕의 검, 모든 것을 베어낸다.\n데미지가 50% 증가한다.",
                    rarity = ArtifactRarity.Legendary,
                    upgradeType = PermanentUpgradeType.Damage,
                    effectValue = 0.50f, isPercentage = true,
                    rarityColor = Color.yellow
                },
                new Artifact 
                { 
                    id = 4002, displayName = "시공의 부츠", 
                    description = "시공을 초월하는 신속함.\n이동속도가 40% 증가한다.",
                    rarity = ArtifactRarity.Legendary,
                    upgradeType = PermanentUpgradeType.MoveSpeed,
                    effectValue = 0.40f, isPercentage = true,
                    rarityColor = Color.yellow
                }
            });
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        goldSystem = GoldSystem.Instance;
        if (goldSystem == null)
        {
            Debug.LogError("[ArtifactGachaSystem] GoldSystem을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 확률 검증
    /// </summary>
    private void ValidateProbabilities()
    {
        float total = commonChance + rareChance + epicChance + legendaryChance;
        
        if (showProbabilityCheck || enableDebugLogs)
        {
            Debug.Log($"[ArtifactGachaSystem] 확률 검증: {total}% (일반:{commonChance}%, 레어:{rareChance}%, 에픽:{epicChance}%, 전설:{legendaryChance}%)");
        }
        
        if (Mathf.Abs(total - 100f) > 0.1f)
        {
            Debug.LogWarning($"[ArtifactGachaSystem] 확률의 합이 100%가 아닙니다: {total}%");
        }
    }
    
    /// <summary>
    /// 보유 유물 로드
    /// </summary>
    private void LoadOwnedArtifacts()
    {
        string savedData = PlayerPrefs.GetString(ARTIFACTS_SAVE_KEY, "");
        ownedArtifactIds.Clear();
        
        if (!string.IsNullOrEmpty(savedData))
        {
            string[] idStrings = savedData.Split(',');
            foreach (string idString in idStrings)
            {
                if (int.TryParse(idString, out int id))
                {
                    ownedArtifactIds.Add(id);
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ArtifactGachaSystem] 보유 유물 로드: {ownedArtifactIds.Count}개");
        }
    }
    
    /// <summary>
    /// 보유 유물 저장
    /// </summary>
    private void SaveOwnedArtifacts()
    {
        string saveData = string.Join(",", ownedArtifactIds);
        PlayerPrefs.SetString(ARTIFACTS_SAVE_KEY, saveData);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 가챠 실행
    /// </summary>
    /// <returns>획득한 유물 (null이면 실패)</returns>
    public Artifact PullGacha()
    {
        if (goldSystem == null || !goldSystem.CanAfford(gachaCost))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ArtifactGachaSystem] 골드 부족: 필요 {gachaCost}, 보유 {goldSystem?.CurrentGold ?? 0}");
            }
            return null;
        }
        
        // 골드 차감
        goldSystem.SpendGold(gachaCost);
        
        // 등급 결정
        ArtifactRarity rarity = DetermineRarity();
        
        // 해당 등급에서 랜덤 유물 선택
        Artifact artifact = GetRandomArtifactByRarity(rarity);
        
        if (artifact != null)
        {
            // 보유 목록에 추가 (중복 가능)
            ownedArtifactIds.Add(artifact.id);
            SaveOwnedArtifacts();
            
            // 이벤트 발생
            OnArtifactObtained?.Invoke(artifact);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[ArtifactGachaSystem] 🎁 유물 획득: {artifact.displayName} ({rarity})");
            }
        }
        
        return artifact;
    }
    
    /// <summary>
    /// 등급 결정
    /// </summary>
    /// <returns>결정된 등급</returns>
    private ArtifactRarity DetermineRarity()
    {
        float random = Random.Range(0f, 100f);
        float cumulative = 0f;
        
        cumulative += legendaryChance;
        if (random <= cumulative) return ArtifactRarity.Legendary;
        
        cumulative += epicChance;
        if (random <= cumulative) return ArtifactRarity.Epic;
        
        cumulative += rareChance;
        if (random <= cumulative) return ArtifactRarity.Rare;
        
        return ArtifactRarity.Common;
    }
    
    /// <summary>
    /// 등급별 랜덤 유물 선택
    /// </summary>
    /// <param name="rarity">등급</param>
    /// <returns>선택된 유물</returns>
    private Artifact GetRandomArtifactByRarity(ArtifactRarity rarity)
    {
        List<Artifact> artifactPool = rarity switch
        {
            ArtifactRarity.Common => commonArtifacts,
            ArtifactRarity.Rare => rareArtifacts,
            ArtifactRarity.Epic => epicArtifacts,
            ArtifactRarity.Legendary => legendaryArtifacts,
            _ => commonArtifacts
        };
        
        if (artifactPool.Count == 0)
        {
            Debug.LogWarning($"[ArtifactGachaSystem] {rarity} 등급 유물이 없습니다!");
            return null;
        }
        
        int randomIndex = Random.Range(0, artifactPool.Count);
        return artifactPool[randomIndex];
    }
    
    /// <summary>
    /// 보유 유물 목록 조회
    /// </summary>
    /// <returns>보유 유물 ID 목록</returns>
    public List<int> GetOwnedArtifactIds()
    {
        return new List<int>(ownedArtifactIds);
    }
    
    /// <summary>
    /// 유물 ID로 데이터 조회
    /// </summary>
    /// <param name="artifactId">유물 ID</param>
    /// <returns>유물 데이터</returns>
    public Artifact GetArtifactById(int artifactId)
    {
        var allArtifacts = new List<Artifact>();
        allArtifacts.AddRange(commonArtifacts);
        allArtifacts.AddRange(rareArtifacts);
        allArtifacts.AddRange(epicArtifacts);
        allArtifacts.AddRange(legendaryArtifacts);
        
        foreach (var artifact in allArtifacts)
        {
            if (artifact.id == artifactId)
                return artifact;
        }
        
        return null;
    }
    
    /// <summary>
    /// 보유 유물 개수 조회
    /// </summary>
    /// <returns>보유 유물 개수</returns>
    public int GetOwnedArtifactCount()
    {
        return ownedArtifactIds.Count;
    }
    
    /// <summary>
    /// 가챠 비용 조회
    /// </summary>
    /// <returns>가챠 비용</returns>
    public int GetGachaCost()
    {
        return gachaCost;
    }
    
    /// <summary>
    /// 가챠 실행 가능 여부
    /// </summary>
    /// <returns>실행 가능 여부</returns>
    public bool CanPullGacha()
    {
        return goldSystem != null && goldSystem.CanAfford(gachaCost);
    }
    
    /// <summary>
    /// 보유 유물 데이터 리셋 (디버그용)
    /// </summary>
    [ContextMenu("보유 유물 리셋")]
    public void ResetOwnedArtifacts()
    {
        ownedArtifactIds.Clear();
        PlayerPrefs.DeleteKey(ARTIFACTS_SAVE_KEY);
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
        {
            Debug.Log("[ArtifactGachaSystem] 🔄 보유 유물 데이터가 리셋되었습니다.");
        }
    }
    
    /// <summary>
    /// 테스트 가챠 (골드 소모 없음)
    /// </summary>
    [ContextMenu("테스트 가챠")]
    public void TestGacha()
    {
        ArtifactRarity rarity = DetermineRarity();
        Artifact artifact = GetRandomArtifactByRarity(rarity);
        
        Debug.Log($"[ArtifactGachaSystem] 🎲 테스트 가챠 결과: {artifact?.displayName ?? "없음"} ({rarity})");
    }
}