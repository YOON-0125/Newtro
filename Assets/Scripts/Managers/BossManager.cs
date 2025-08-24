using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 보스 전투 시스템을 총괄 관리하는 매니저
/// </summary>
public class BossManager : MonoBehaviour
{
    [Header("보스 시스템 설정")]
    [SerializeField] private bool debugMode = true;
    
    [Header("보스 타입별 프리팹")]
    [SerializeField] private BossData[] bossDatabase;
    
    [Header("보스 UI")]
    [SerializeField] private GameObject bossUICanvas;
    [SerializeField] private BossUI bossUI;
    
    [Header("보스 보상 UI")]
    [SerializeField] private GameObject bossRewardUIPanel;
    [SerializeField] private BossRewardManager bossRewardManager;
    
    // 이벤트 시스템
    [System.Serializable]
    public class BossEvents
    {
        public UnityEvent OnBossEncounterStart;
        public UnityEvent OnBossEncounterEnd;
        public UnityEvent<BossBase> OnBossSpawned;
        public UnityEvent<BossBase> OnBossDefeated;
        public UnityEvent<BossType> OnBossRewardOffered;
    }
    
    [Header("이벤트")]
    [SerializeField] private BossEvents events;
    
    // 싱글톤
    public static BossManager Instance { get; private set; }
    
    // 현재 보스 전투 상태
    private bool isBossEncounterActive = false;
    private BossBase currentBoss = null;
    private BossRoomTrigger currentBossRoom = null;
    
    // 외부 매니저 참조
    private GameManager gameManager;
    private EnemyManager enemyManager;
    private LevelUpManager levelUpManager;
    
    // 프로퍼티
    public bool IsBossEncounterActive => isBossEncounterActive;
    public BossBase CurrentBoss => currentBoss;
    public BossEvents Events => events;
    
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
        
        InitializeManagers();
    }
    
    private void Start()
    {
        Debug.Log("[BossManager] Start() 호출됨");
        
        // 보스 UI 초기 비활성화
        if (bossUICanvas != null)
        {
            Debug.Log($"[BossManager] bossUICanvas 초기 비활성화 전 상태: {bossUICanvas.activeInHierarchy}");
            bossUICanvas.SetActive(false);
            Debug.Log($"[BossManager] bossUICanvas 초기 비활성화 후 상태: {bossUICanvas.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[BossManager] Start(): bossUICanvas가 null입니다!");
        }
        
        ValidateSetup();
    }
    
    /// <summary>
    /// 매니저들 초기화 및 참조 설정
    /// </summary>
    private void InitializeManagers()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        enemyManager = FindObjectOfType<EnemyManager>();
        levelUpManager = FindObjectOfType<LevelUpManager>();
        
        if (debugMode)
        {
            Debug.Log($"[BossManager] 매니저 초기화 - GameManager: {gameManager != null}, EnemyManager: {enemyManager != null}, LevelUpManager: {levelUpManager != null}");
        }
    }
    
    /// <summary>
    /// 보스 전투 시작
    /// </summary>
    public void StartBossEncounter(BossType bossType, BossRoomTrigger bossRoom)
    {
        if (isBossEncounterActive)
        {
            Debug.LogWarning("[BossManager] 이미 보스 전투가 진행 중입니다!");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"[BossManager] 보스 전투 시작: {bossType}");
        }
        
        isBossEncounterActive = true;
        currentBossRoom = bossRoom;
        
        // 일반 적 스폰 중지
        if (enemyManager != null)
        {
            enemyManager.StopSpawning();
        }
        
        // 보스 스폰
        SpawnBoss(bossType, bossRoom.BossSpawnPoint);
        
        // 보스 UI 활성화
        ShowBossUI();
        
        // 이벤트 발생
        events?.OnBossEncounterStart?.Invoke();
    }
    
    /// <summary>
    /// 보스 전투 종료
    /// </summary>
    public void EndBossEncounter(bool bossDefeated = true)
    {
        if (!isBossEncounterActive)
        {
            Debug.LogWarning("[BossManager] 보스 전투가 진행 중이 아닙니다!");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"[BossManager] 보스 전투 종료 - 보스 처치: {bossDefeated}");
        }
        
        isBossEncounterActive = false;
        
        // 보스 UI 비활성화
        HideBossUI();
        
        // 일반 적 스폰 재개
        if (enemyManager != null)
        {
            enemyManager.StartSpawning();
        }
        
        // 보스 처치 시 보상 제공
        if (bossDefeated && currentBoss != null)
        {
            OfferBossReward(currentBoss.BossType);
        }
        
        // 현재 보스 방 클리어 처리
        if (currentBossRoom != null)
        {
            currentBossRoom.MarkAsCleared();
        }
        
        // 변수 초기화
        currentBoss = null;
        currentBossRoom = null;
        
        // 이벤트 발생
        events?.OnBossEncounterEnd?.Invoke();
    }
    
    /// <summary>
    /// 보스 스폰
    /// </summary>
    private void SpawnBoss(BossType bossType, Transform spawnPoint)
    {
        BossData bossData = GetBossData(bossType);
        if (bossData.bossPrefab == null)
        {
            Debug.LogError($"[BossManager] 보스 데이터를 찾을 수 없습니다: {bossType}");
            return;
        }
        
        // 보스 오브젝트 생성
        GameObject bossObj = Instantiate(bossData.bossPrefab, spawnPoint.position, spawnPoint.rotation);
        currentBoss = bossObj.GetComponent<BossBase>();
        
        if (currentBoss == null)
        {
            Debug.LogError($"[BossManager] 보스 프리팹에 BossBase 스크립트가 없습니다: {bossType}");
            Destroy(bossObj);
            return;
        }
        
        // 보스 초기화
        currentBoss.InitializeBoss(bossData);
        
        // 보스 이벤트 구독
        currentBoss.OnBossDefeated += OnBossDefeated;
        
        if (debugMode)
        {
            Debug.Log($"[BossManager] 보스 스폰 완료: {currentBoss.BossName}");
        }
        
        // 이벤트 발생
        events?.OnBossSpawned?.Invoke(currentBoss);
    }
    
    /// <summary>
    /// 보스 처치 콜백
    /// </summary>
    private void OnBossDefeated(BossBase defeatedBoss)
    {
        if (debugMode)
        {
            Debug.Log($"[BossManager] 보스 처치됨: {defeatedBoss.BossName}");
        }
        
        // 이벤트 발생
        events?.OnBossDefeated?.Invoke(defeatedBoss);
        
        // 보스 전투 종료
        EndBossEncounter(true);
    }
    
    /// <summary>
    /// 보스 보상 제공
    /// </summary>
    private void OfferBossReward(BossType bossType)
    {
        if (debugMode)
        {
            Debug.Log($"[BossManager] 보스 보상 제공: {bossType}");
        }
        
        // 이벤트 발생
        events?.OnBossRewardOffered?.Invoke(bossType);
        
        // 보스 보상 UI 표시
        ShowBossRewardUI(bossType);
    }
    
    /// <summary>
    /// 보스 보상 UI 표시
    /// </summary>
    private void ShowBossRewardUI(BossType bossType)
    {
        Debug.Log($"[BossManager] ShowBossRewardUI 호출됨 - bossType: {bossType}");
        Debug.Log($"[BossManager] bossRewardUIPanel: {(bossRewardUIPanel != null ? bossRewardUIPanel.name : "null")}");
        Debug.Log($"[BossManager] bossRewardManager: {(bossRewardManager != null ? "설정됨" : "null")}");
        
        if (bossRewardUIPanel != null)
        {
            Debug.Log($"[BossManager] bossRewardUIPanel 활성화 전 상태: {bossRewardUIPanel.activeInHierarchy}");
            bossRewardUIPanel.SetActive(true);
            Debug.Log($"[BossManager] bossRewardUIPanel 활성화 후 상태: {bossRewardUIPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[BossManager] bossRewardUIPanel이 null입니다!");
        }
        
        if (bossRewardManager != null)
        {
            bossRewardManager.ShowBossReward(bossType);
        }
        else
        {
            Debug.LogError("[BossManager] BossRewardManager가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 보스 보상 UI 숨기기
    /// </summary>
    public void HideBossRewardUI()
    {
        if (bossRewardUIPanel != null)
        {
            bossRewardUIPanel.SetActive(false);
        }
        
        if (bossRewardManager != null)
        {
            bossRewardManager.HideBossReward();
        }
    }
    
    /// <summary>
    /// 보스 UI 표시
    /// </summary>
    private void ShowBossUI()
    {
        Debug.Log("[BossManager] ShowBossUI 호출됨");
        Debug.Log($"[BossManager] bossUICanvas: {(bossUICanvas != null ? bossUICanvas.name : "null")}");
        Debug.Log($"[BossManager] bossUI: {(bossUI != null ? "설정됨" : "null")}");
        Debug.Log($"[BossManager] currentBoss: {(currentBoss != null ? currentBoss.BossName : "null")}");
        
        if (bossUICanvas != null)
        {
            Debug.Log($"[BossManager] bossUICanvas 활성화 전 상태: {bossUICanvas.activeInHierarchy}");
            bossUICanvas.SetActive(true);
            Debug.Log($"[BossManager] bossUICanvas 활성화 후 상태: {bossUICanvas.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[BossManager] bossUICanvas가 null입니다!");
        }
        
        if (bossUI != null && currentBoss != null)
        {
            bossUI.SetBoss(currentBoss);
            Debug.Log("[BossManager] bossUI에 현재 보스 설정됨");
        }
        else
        {
            Debug.LogWarning($"[BossManager] bossUI 설정 실패 - bossUI: {bossUI != null}, currentBoss: {currentBoss != null}");
        }
    }
    
    /// <summary>
    /// 보스 UI 숨기기
    /// </summary>
    private void HideBossUI()
    {
        Debug.Log("[BossManager] HideBossUI 호출됨");
        
        if (bossUICanvas != null)
        {
            Debug.Log($"[BossManager] bossUICanvas 비활성화 전 상태: {bossUICanvas.activeInHierarchy}");
            bossUICanvas.SetActive(false);
            Debug.Log($"[BossManager] bossUICanvas 비활성화 후 상태: {bossUICanvas.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[BossManager] bossUICanvas가 null입니다!");
        }
        
        if (bossUI != null)
        {
            bossUI.ClearBoss();
            Debug.Log("[BossManager] bossUI 클리어됨");
        }
        else
        {
            Debug.LogWarning("[BossManager] bossUI가 null입니다!");
        }
    }
    
    /// <summary>
    /// 보스 데이터 가져오기
    /// </summary>
    private BossData GetBossData(BossType bossType)
    {
        foreach (BossData data in bossDatabase)
        {
            if (data.bossType == bossType)
            {
                return data;
            }
        }
        return default;
    }
    
    /// <summary>
    /// 설정 검증
    /// </summary>
    private void ValidateSetup()
    {
        if (bossDatabase == null || bossDatabase.Length == 0)
        {
            Debug.LogWarning("[BossManager] 보스 데이터베이스가 설정되지 않았습니다!");
        }
        
        if (bossUICanvas == null)
        {
            Debug.LogWarning("[BossManager] 보스 UI 캔버스가 설정되지 않았습니다!");
        }
        
        if (bossUI == null)
        {
            Debug.LogWarning("[BossManager] BossUI 컴포넌트가 설정되지 않았습니다!");
        }
        
        if (bossRewardUIPanel == null)
        {
            Debug.LogWarning("[BossManager] 보스 보상 UI 패널이 설정되지 않았습니다!");
        }
        
        if (bossRewardManager == null)
        {
            Debug.LogWarning("[BossManager] BossRewardManager가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 디버그 정보 반환
    /// </summary>
    public string GetDebugInfo()
    {
        return $"보스 매니저 정보\n" +
               $"전투 중: {isBossEncounterActive}\n" +
               $"현재 보스: {(currentBoss != null ? currentBoss.BossName : "없음")}\n" +
               $"보스 데이터베이스: {(bossDatabase != null ? bossDatabase.Length : 0)}개";
    }
    
    /// <summary>
    /// 모든 보스 방 클리어 여부 확인
    /// </summary>
    public bool AreAllBossesDefeated()
    {
        BossRoomTrigger[] allBossRooms = FindObjectsOfType<BossRoomTrigger>();
        foreach (var room in allBossRooms)
        {
            if (!room.IsCleared)
            {
                return false;
            }
        }
        return true;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// 보스 타입 열거형
/// </summary>
public enum BossType
{
    Fire,       // 화염 보스
    Ice,        // 얼음 보스
    Lightning,  // 번개 보스
    Shadow,     // 어둠 보스
    Earth       // 대지 보스
}

/// <summary>
/// 보스 데이터 구조체
/// </summary>
[System.Serializable]
public struct BossData
{
    public BossType bossType;
    public string bossName;
    public GameObject bossPrefab;
    public Sprite bossIcon;
    public Color bossColor;
    
    [Header("보스 스탯")]
    public float maxHealth;
    public float damage;
    public float moveSpeed;
    
    [Header("보상 설정")]
    public BossRewardType[] possibleRewards;
}

/// <summary>
/// 보스 보상 타입
/// </summary>
public enum BossRewardType
{
    ElementalSynergy,   // 원소 시너지 해금
    PowerfulUpgrade,    // 강력한 업그레이드
    UniqueAbility,      // 고유 능력 획득
    WeaponEvolution     // 무기 진화
}