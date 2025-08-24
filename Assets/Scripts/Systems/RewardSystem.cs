using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보물상자 보상 타입
/// </summary>
public enum TreasureRewardType
{
    Gold,           // 골드
    Health,         // 체력 회복 (1하트)
    ClearMap,       // 모든 적 처치
    Awakening       // 각성 (강화된 업그레이드)
}

/// <summary>
/// 보물상자 보상 데이터
/// </summary>
[System.Serializable]
public class TreasureReward
{
    public TreasureRewardType type;
    public int goldAmount;      // 골드량 (골드 보상일 때)
    public string nameText;     // 표시할 텍스트
    public string description;  // 설명
}

/// <summary>
/// 보상 시스템 - 보물상자 보상 결정 및 지급
/// </summary>
public class RewardSystem : MonoBehaviour
{
    [Header("보상 확률 설정")]
    [SerializeField] [Range(0f, 100f)] private float goldChance = 50f;
    [SerializeField] [Range(0f, 100f)] private float healthChance = 39f;
    [SerializeField] [Range(0f, 100f)] private float clearMapChance = 10f;
    [SerializeField] [Range(0f, 100f)] private float awakeningChance = 1f;
    
    [Header("골드 보상 설정 (시간대별)")]
    [SerializeField] private Vector2Int goldRange0to3min = new Vector2Int(5, 20);   // ~3분
    [SerializeField] private Vector2Int goldRange3to5min = new Vector2Int(15, 30);  // ~5분
    [SerializeField] private Vector2Int goldRange5to10min = new Vector2Int(25, 40); // ~10분
    [SerializeField] private Vector2Int goldRange10to15min = new Vector2Int(35, 50); // ~15분
    
    [Header("각성 업그레이드 설정")]
    [SerializeField] private List<AwakeningUpgrade> awakeningUpgrades = new List<AwakeningUpgrade>();
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 싱글톤
    public static RewardSystem Instance { get; private set; }
    
    // 참조
    private GoldSystem goldSystem;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    
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
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 각성 업그레이드 기본값 설정
        if (awakeningUpgrades.Count == 0)
        {
            InitializeDefaultAwakeningUpgrades();
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        goldSystem = GoldSystem.Instance;
        playerHealth = FindObjectOfType<PlayerHealth>();
        gameManager = GameManager.Instance;
        
        if (goldSystem == null)
        {
            Debug.LogError("[RewardSystem] GoldSystem을 찾을 수 없습니다!");
        }
        
        if (playerHealth == null)
        {
            Debug.LogWarning("[RewardSystem] PlayerHealth를 찾을 수 없습니다!");
        }
        
        if (gameManager == null)
        {
            Debug.LogWarning("[RewardSystem] GameManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 보상 결정 및 지급
    /// </summary>
    /// <returns>지급된 보상 정보</returns>
    public TreasureReward DetermineAndGiveReward()
    {
        // 보상 타입 결정
        TreasureRewardType rewardType = DetermineRewardType();
        
        // 보상 생성 및 지급
        TreasureReward reward = CreateReward(rewardType);
        GiveReward(reward);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[RewardSystem] 🎁 보상 지급: {reward.nameText}");
        }
        
        return reward;
    }
    
    /// <summary>
    /// 보상 타입 결정 (확률 기반)
    /// </summary>
    /// <returns>결정된 보상 타입</returns>
    private TreasureRewardType DetermineRewardType()
    {
        float random = Random.Range(0f, 100f);
        float cumulative = 0f;
        
        // 각성 (1%)
        cumulative += awakeningChance;
        if (random <= cumulative)
        {
            return TreasureRewardType.Awakening;
        }
        
        // 맵클리어 (10%)
        cumulative += clearMapChance;
        if (random <= cumulative)
        {
            return TreasureRewardType.ClearMap;
        }
        
        // 체력회복 (39%)
        cumulative += healthChance;
        if (random <= cumulative)
        {
            return TreasureRewardType.Health;
        }
        
        // 나머지는 골드 (50%)
        return TreasureRewardType.Gold;
    }
    
    /// <summary>
    /// 보상 생성
    /// </summary>
    /// <param name="type">보상 타입</param>
    /// <returns>생성된 보상</returns>
    private TreasureReward CreateReward(TreasureRewardType type)
    {
        TreasureReward reward = new TreasureReward();
        reward.type = type;
        
        switch (type)
        {
            case TreasureRewardType.Gold:
                int goldAmount = GetGoldAmountByTime();
                reward.goldAmount = goldAmount;
                reward.nameText = $"골드 {goldAmount}";
                reward.description = $"{goldAmount} 골드를 획득했습니다!";
                break;
                
            case TreasureRewardType.Health:
                reward.nameText = "체력 회복";
                reward.description = "체력이 1하트 회복되었습니다!";
                break;
                
            case TreasureRewardType.ClearMap:
                reward.nameText = "모든 적 처치";
                reward.description = "맵의 모든 적이 처치되었습니다!";
                break;
                
            case TreasureRewardType.Awakening:
                AwakeningUpgrade awakening = GetRandomAwakeningUpgrade();
                reward.nameText = awakening.displayName;
                reward.description = awakening.description;
                break;
        }
        
        return reward;
    }
    
    /// <summary>
    /// 현재 게임 시간에 따른 골드 획득량 결정
    /// </summary>
    /// <returns>골드량</returns>
    private int GetGoldAmountByTime()
    {
        float gameTime = gameManager != null ? gameManager.GetGameTime() : 0f;
        Vector2Int range;
        
        if (gameTime <= 180f) // ~3분
        {
            range = goldRange0to3min;
        }
        else if (gameTime <= 300f) // ~5분
        {
            range = goldRange3to5min;
        }
        else if (gameTime <= 600f) // ~10분
        {
            range = goldRange5to10min;
        }
        else // ~15분+
        {
            range = goldRange10to15min;
        }
        
        return Random.Range(range.x, range.y + 1);
    }
    
    /// <summary>
    /// 보상 지급 실행
    /// </summary>
    /// <param name="reward">지급할 보상</param>
    private void GiveReward(TreasureReward reward)
    {
        switch (reward.type)
        {
            case TreasureRewardType.Gold:
                if (goldSystem != null)
                {
                    goldSystem.AddGold(reward.goldAmount);
                }
                break;
                
            case TreasureRewardType.Health:
                if (playerHealth != null)
                {
                    playerHealth.RestoreHealth(4); // 1하트 = 4 체력
                }
                break;
                
            case TreasureRewardType.ClearMap:
                ClearAllEnemies();
                break;
                
            case TreasureRewardType.Awakening:
                ApplyAwakeningUpgrade();
                break;
        }
    }
    
    /// <summary>
    /// 모든 적 처치 (범위 9999, 데미지 10000000 스킬)
    /// </summary>
    private void ClearAllEnemies()
    {
        // 모든 적에게 초고데미지 적용
        Collider2D[] enemies = Physics2D.OverlapCircleAll(Vector3.zero, 9999f, LayerMask.GetMask("Enemy"));
        
        foreach (var enemy in enemies)
        {
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.TakeDamage(10000000f);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[RewardSystem] ⚡ 모든 적 처치: {enemies.Length}마리");
        }
    }
    
    /// <summary>
    /// 각성 업그레이드 적용
    /// </summary>
    private void ApplyAwakeningUpgrade()
    {
        AwakeningUpgrade awakening = GetRandomAwakeningUpgrade();
        
        // TODO: 실제 업그레이드 적용 로직 구현
        if (enableDebugLogs)
        {
            Debug.Log($"[RewardSystem] ✨ 각성 적용: {awakening.displayName}");
        }
    }
    
    /// <summary>
    /// 랜덤 각성 업그레이드 선택
    /// </summary>
    /// <returns>선택된 각성 업그레이드</returns>
    private AwakeningUpgrade GetRandomAwakeningUpgrade()
    {
        if (awakeningUpgrades.Count == 0)
        {
            Debug.LogWarning("[RewardSystem] 각성 업그레이드가 없습니다!");
            return new AwakeningUpgrade { displayName = "알 수 없는 각성", description = "효과 없음" };
        }
        
        int randomIndex = Random.Range(0, awakeningUpgrades.Count);
        return awakeningUpgrades[randomIndex];
    }
    
    /// <summary>
    /// 기본 각성 업그레이드 초기화
    /// </summary>
    private void InitializeDefaultAwakeningUpgrades()
    {
        awakeningUpgrades.AddRange(new List<AwakeningUpgrade>
        {
            new AwakeningUpgrade 
            { 
                displayName = "신속한 발걸음", 
                description = "이동속도가 15% 증가합니다",
                statType = "moveSpeed",
                value = 0.15f
            },
            new AwakeningUpgrade 
            { 
                displayName = "강력한 타격", 
                description = "데미지가 25% 증가합니다",
                statType = "damage",
                value = 0.25f
            },
            new AwakeningUpgrade 
            { 
                displayName = "풍부한 지식", 
                description = "경험치 획득량이 20% 증가합니다",
                statType = "expGain",
                value = 0.20f
            },
            new AwakeningUpgrade 
            { 
                displayName = "튼튼한 육체", 
                description = "최대 체력이 2하트 증가합니다",
                statType = "maxHealth",
                value = 8f // 2하트 = 8 체력
            }
        });
    }
}

/// <summary>
/// 각성 업그레이드 데이터
/// </summary>
[System.Serializable]
public class AwakeningUpgrade
{
    public string displayName;
    public string description;
    public string statType;     // 적용할 스탯 타입
    public float value;         // 효과값
}