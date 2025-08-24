using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보물상자 스포너 - 시간대별 스폰 관리
/// </summary>
public class TreasureSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject treasureChestPrefab;
    [SerializeField] private Transform chestContainer; // 보물상자들을 담을 부모 오브젝트
    [SerializeField] private Transform playerTarget; // 플레이어 오브젝트 (드래그앤드롭)
    
    [Header("스폰 거리 설정")]
    [SerializeField] private float minSpawnDistance = 30f;
    [SerializeField] private float maxSpawnDistance = 50f;
    [SerializeField] private float despawnDistance = 50f; // 플레이어로부터 이 거리 이상 떨어지면 파괴
    
    [Header("스폰 개수 설정 (시간대별)")]
    [SerializeField] private Vector2Int count0to1min = new Vector2Int(0, 1);   // ~1분
    [SerializeField] private Vector2Int count1to3min = new Vector2Int(1, 2);   // ~3분
    [SerializeField] private Vector2Int count3to5min = new Vector2Int(2, 3);   // ~5분
    [SerializeField] private Vector2Int count5to10min = new Vector2Int(3, 5);  // ~10분
    [SerializeField] private Vector2Int count10to15min = new Vector2Int(4, 10); // ~15분
    
    [Header("리스폰 설정")]
    [SerializeField] private float respawnDelay = 5f; // 보물상자 소모 후 리스폰 딜레이
    
    [Header("맵 경계 설정")]
    [SerializeField] private float mapRadius = 100f; // 맵 반지름 (스폰 가능 영역)
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showGizmos = true;
    
    // 내부 변수
    private List<TreasureChest> activeChests = new List<TreasureChest>();
    private List<Vector3> usedSpawnPositions = new List<Vector3>();
    private Transform playerTransform;
    private GameManager gameManager;
    
    // 시간 관리
    private float lastCheckTime = 0f;
    private float checkInterval = 60f; // 1분마다 체크
    private int targetChestCount = 0;
    
    // 싱글톤
    public static TreasureSpawner Instance { get; private set; }
    
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
        StartSpawning();
    }
    
    private void Update()
    {
        UpdateSpawning();
        CleanupInvalidChests();
        CleanupDistantChests(); // 거리 기반 정리 추가
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 컨테이너 오브젝트 생성
        if (chestContainer == null)
        {
            GameObject container = new GameObject("TreasureChests");
            container.transform.SetParent(transform);
            chestContainer = container.transform;
        }
    }
    
    /// <summary>
    /// 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        // 플레이어 참조 설정 (드래그앤드롭 우선, 없으면 태그로 찾기)
        if (playerTarget != null)
        {
            playerTransform = playerTarget;
            Debug.Log("[TreasureSpawner] ✅ 플레이어 참조: 드래그앤드롭으로 설정됨");
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("[TreasureSpawner] ✅ 플레이어 참조: 태그로 자동 찾기됨");
            }
            else
            {
                Debug.LogError("[TreasureSpawner] ❌ 플레이어를 찾을 수 없습니다! PlayerTarget을 드래그앤드롭하거나 Player 태그를 확인하세요!");
            }
        }
        
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[TreasureSpawner] GameManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 스폰 시작
    /// </summary>
    private void StartSpawning()
    {
        // 초기 목표 개수 설정
        UpdateTargetChestCount();
        
        // 초기 스폰
        SpawnInitialChests();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureSpawner] 🏁 스폰 시작 - 목표: {targetChestCount}개");
        }
    }
    
    /// <summary>
    /// 스폰 업데이트 (매 프레임)
    /// </summary>
    private void UpdateSpawning()
    {
        if (playerTransform == null) return;
        
        float currentTime = GetGameTime();
        
        // 1분마다 목표 개수 재계산
        if (currentTime - lastCheckTime >= checkInterval)
        {
            lastCheckTime = currentTime;
            UpdateTargetChestCount();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[TreasureSpawner] ⏰ {currentTime/60f:F1}분 경과 - 새 목표: {targetChestCount}개");
            }
        }
        
        // 부족한 상자들 스폰
        SpawnMissingChests();
    }
    
    /// <summary>
    /// 현재 게임 시간에 따른 목표 보물상자 개수 업데이트
    /// </summary>
    private void UpdateTargetChestCount()
    {
        float gameTime = GetGameTime();
        Vector2Int range = GetChestCountRange(gameTime);
        
        // 목표 개수 랜덤 결정
        targetChestCount = Random.Range(range.x, range.y + 1);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureSpawner] 🎯 목표 개수 업데이트: {targetChestCount}개 (시간: {gameTime/60f:F1}분, 범위: {range.x}-{range.y})");
        }
    }
    
    /// <summary>
    /// 게임 시간에 따른 보물상자 개수 범위 반환
    /// </summary>
    /// <param name="gameTime">게임 시간 (초)</param>
    /// <returns>최소-최대 개수 범위</returns>
    private Vector2Int GetChestCountRange(float gameTime)
    {
        if (gameTime <= 60f) // ~1분
        {
            return count0to1min;
        }
        else if (gameTime <= 180f) // ~3분
        {
            return count1to3min;
        }
        else if (gameTime <= 300f) // ~5분
        {
            return count3to5min;
        }
        else if (gameTime <= 600f) // ~10분
        {
            return count5to10min;
        }
        else // ~15분+
        {
            return count10to15min;
        }
    }
    
    /// <summary>
    /// 초기 보물상자들 스폰
    /// </summary>
    private void SpawnInitialChests()
    {
        for (int i = 0; i < targetChestCount; i++)
        {
            SpawnChest();
        }
    }
    
    /// <summary>
    /// 부족한 보물상자들 스폰
    /// </summary>
    private void SpawnMissingChests()
    {
        int currentCount = GetActiveChestCount();
        int needed = targetChestCount - currentCount;
        
        if (needed > 0)
        {
            // 5초마다 하나씩 스폰
            StartCoroutine(SpawnChestsWithDelay(needed));
        }
    }
    
    /// <summary>
    /// 딜레이를 두고 보물상자 스폰
    /// </summary>
    /// <param name="count">스폰할 개수</param>
    private System.Collections.IEnumerator SpawnChestsWithDelay(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnChest();
            yield return new WaitForSeconds(respawnDelay);
        }
    }
    
    /// <summary>
    /// 보물상자 하나 스폰
    /// </summary>
    private void SpawnChest()
    {
        if (treasureChestPrefab == null || playerTransform == null)
        {
            Debug.LogError("[TreasureSpawner] 프리팹이나 플레이어가 설정되지 않았습니다!");
            return;
        }
        
        Vector3 spawnPosition = FindValidSpawnPosition();
        if (spawnPosition == Vector3.zero)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[TreasureSpawner] ❌ 유효한 스폰 위치를 찾을 수 없습니다!");
            }
            return;
        }
        
        // 보물상자 생성
        GameObject chestObj = Instantiate(treasureChestPrefab, spawnPosition, Quaternion.identity, chestContainer);
        TreasureChest chest = chestObj.GetComponent<TreasureChest>();
        
        if (chest == null)
        {
            chest = chestObj.AddComponent<TreasureChest>();
        }
        
        // 리스트에 추가
        activeChests.Add(chest);
        usedSpawnPositions.Add(spawnPosition);
        
        // 보물상자 열림 이벤트 구독
        chest.OnChestOpened += OnChestOpened;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureSpawner] 📦 보물상자 스폰: {spawnPosition} (총 {GetActiveChestCount()}개)");
        }
    }
    
    /// <summary>
    /// 유효한 스폰 위치 찾기
    /// </summary>
    /// <returns>스폰 위치 (실패시 Vector3.zero)</returns>
    private Vector3 FindValidSpawnPosition()
    {
        int maxAttempts = 30;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 현재 플레이어 위치 기준으로 랜덤 위치 생성 (동적 스폰)
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 candidatePosition = playerTransform.position + (Vector3)(randomDirection * randomDistance);
            
            // 맵 경계 체크
            if (Vector3.Distance(Vector3.zero, candidatePosition) > mapRadius)
            {
                continue;
            }
            
            // 다른 보물상자와의 거리 체크
            if (IsPositionTooClose(candidatePosition))
            {
                continue;
            }
            
            return candidatePosition;
        }
        
        return Vector3.zero; // 실패
    }
    
    /// <summary>
    /// 위치가 기존 보물상자들과 너무 가까운지 체크
    /// </summary>
    /// <param name="position">확인할 위치</param>
    /// <returns>너무 가까운지 여부</returns>
    private bool IsPositionTooClose(Vector3 position)
    {
        float minDistance = 10f; // 보물상자 간 최소 거리
        
        foreach (var usedPosition in usedSpawnPositions)
        {
            if (Vector3.Distance(position, usedPosition) < minDistance)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 보물상자 열림 이벤트 처리
    /// </summary>
    /// <param name="reward">획득한 보상</param>
    private void OnChestOpened(TreasureReward reward)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureSpawner] 🎁 보물상자 열림: {reward.nameText}");
        }
        
        // 리스폰 처리는 UpdateSpawning에서 자동으로 처리됨
    }
    
    /// <summary>
    /// 무효한 보물상자들 정리
    /// </summary>
    private void CleanupInvalidChests()
    {
        for (int i = activeChests.Count - 1; i >= 0; i--)
        {
            if (activeChests[i] == null || activeChests[i].IsOpened())
            {
                // 메모리 최적화: 사용된 스폰 위치도 함께 제거
                if (i < usedSpawnPositions.Count)
                {
                    usedSpawnPositions.RemoveAt(i);
                }
                activeChests.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 플레이어로부터 멀리 떨어진 보물상자들 정리
    /// </summary>
    private void CleanupDistantChests()
    {
        if (playerTransform == null) return;
        
        Vector3 playerPosition = playerTransform.position;
        
        for (int i = activeChests.Count - 1; i >= 0; i--)
        {
            TreasureChest chest = activeChests[i];
            if (chest == null) continue;
            
            // 플레이어로부터의 거리 계산
            float distance = Vector3.Distance(playerPosition, chest.transform.position);
            
            // 설정된 거리보다 멀리 떨어진 보물상자 파괴
            if (distance > despawnDistance)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[TreasureSpawner] 🗑️ 거리 초과로 보물상자 파괴: {distance:F1}f > {despawnDistance}f");
                }
                
                // GameObject 파괴
                if (chest.gameObject != null)
                {
                    Destroy(chest.gameObject);
                }
                
                // 리스트에서 제거 (메모리 최적화)
                if (i < usedSpawnPositions.Count)
                {
                    usedSpawnPositions.RemoveAt(i);
                }
                activeChests.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 현재 활성 보물상자 개수 반환
    /// </summary>
    /// <returns>활성 보물상자 개수</returns>
    private int GetActiveChestCount()
    {
        return activeChests.Count;
    }
    
    /// <summary>
    /// 게임 시간 반환
    /// </summary>
    /// <returns>게임 시간 (초)</returns>
    private float GetGameTime()
    {
        return gameManager != null ? gameManager.GetGameTime() : Time.time;
    }
    
    /// <summary>
    /// 모든 보물상자 제거 (게임 리셋용)
    /// </summary>
    public void ClearAllChests()
    {
        foreach (var chest in activeChests)
        {
            if (chest != null)
            {
                Destroy(chest.gameObject);
            }
        }
        
        activeChests.Clear();
        usedSpawnPositions.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TreasureSpawner] 🧹 모든 보물상자 제거됨");
        }
    }
    
    /// <summary>
    /// 디버그 기즈모 그리기
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || playerTransform == null) return;
        
        // 스폰 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
        
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange 색상
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);
        
        // 맵 경계 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, mapRadius);
        
        // 활성 보물상자 위치 표시
        Gizmos.color = Color.green;
        foreach (var chest in activeChests)
        {
            if (chest != null)
            {
                Gizmos.DrawWireCube(chest.transform.position, Vector3.one * 2f);
            }
        }
    }
}