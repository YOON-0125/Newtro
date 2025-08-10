using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 적 생성 및 관리 매니저
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private Transform player;
    [SerializeField] private int maxEnemies = 50;
    [SerializeField] private float spawnRate = 2f;
    [SerializeField] private float spawnDistance = 15f;
    [SerializeField] private float despawnDistance = 25f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("적 프리팹")]
    [SerializeField] private List<EnemySpawnData> enemyTypes = new List<EnemySpawnData>();
    
    [Header("웨이브 설정")]
    [SerializeField] private bool useWaveSystem = true;
    [SerializeField] private float waveInterval = 60f; // 60초마다 웨이브
    [SerializeField] private float difficultyScale = 1.2f; // 웨이브마다 난이도 증가
    
    [Header("스폰 영역")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(30f, 30f);
    [SerializeField] private bool visualizeSpawnArea = true;
    
    // 이벤트
    [System.Serializable]
    public class EnemyManagerEvents
    {
        public UnityEvent<EnemyBase> OnEnemySpawned;
        public UnityEvent<EnemyBase> OnEnemyDestroyed;
        public UnityEvent<int> OnWaveStart;
        public UnityEvent<int> OnEnemyCountChanged;
    }
    
    [Header("이벤트")]
    [SerializeField] private EnemyManagerEvents events;
    
    // 내부 변수
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private Coroutine spawnCoroutine;
    private int currentWave = 1;
    private float nextWaveTime;
    private float currentDifficultyMultiplier = 1f;
    
    /// <summary>
    /// 적 스폰 데이터
    /// </summary>
    [System.Serializable]
    public class EnemySpawnData
    {
        public string enemyName;
        public GameObject enemyPrefab;
        public float spawnWeight = 1f; // 스폰 가중치
        public int minWave = 1; // 최소 웨이브
        public int maxPerWave = 10; // 웨이브당 최대 스폰 수
        public float spawnCooldown = 1f; // 개별 스폰 쿨다운
        [HideInInspector] public float lastSpawnTime;
    }
    
    // 프로퍼티
    public int ActiveEnemyCount => activeEnemies.Count;
    public int MaxEnemies => maxEnemies;
    public int CurrentWave => currentWave;
    public float DifficultyMultiplier => currentDifficultyMultiplier;
    public bool IsSpawning => spawnCoroutine != null;
    
    private void Awake()
    {
        // 플레이어 자동 찾기
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void Update()
    {
        UpdateEnemies();
        UpdateWaveSystem();
    }
    
    /// <summary>
    /// 초기화
    /// </summary>
    private void Initialize()
    {
        nextWaveTime = Time.time + waveInterval;
        StartSpawning();
    }
    
    /// <summary>
    /// 적 업데이트 (거리 기반 제거 등)
    /// </summary>
    private void UpdateEnemies()
    {
        if (player == null) return;
        
        // 거꾸로 순회하여 안전하게 제거
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }
            
            // 거리 기반 제거
            float distance = Vector2.Distance(activeEnemies[i].transform.position, player.position);
            if (distance > despawnDistance)
            {
                DestroyEnemy(activeEnemies[i]);
            }
        }
    }
    
    /// <summary>
    /// 웨이브 시스템 업데이트
    /// </summary>
    private void UpdateWaveSystem()
    {
        if (!useWaveSystem) return;
        
        if (Time.time >= nextWaveTime)
        {
            StartNewWave();
        }
    }
    
    /// <summary>
    /// 새 웨이브 시작
    /// </summary>
    private void StartNewWave()
    {
        currentWave++;
        currentDifficultyMultiplier = Mathf.Pow(difficultyScale, currentWave - 1);
        nextWaveTime = Time.time + waveInterval;
        
        events?.OnWaveStart?.Invoke(currentWave);
        
        Debug.Log($"웨이브 {currentWave} 시작! 난이도 배율: {currentDifficultyMultiplier:F2}");
    }
    
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnCoroutine());
        }
    }
    
    /// <summary>
    /// 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    
    /// <summary>
    /// 스폰 코루틴
    /// </summary>
    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            if (activeEnemies.Count < maxEnemies && player != null)
            {
                SpawnRandomEnemy();
            }
            
            yield return new WaitForSeconds(1f / spawnRate);
        }
    }
    
    /// <summary>
    /// 랜덤 적 스폰
    /// </summary>
    private void SpawnRandomEnemy()
    {
        List<EnemySpawnData> availableEnemies = GetAvailableEnemies();
        
        if (availableEnemies.Count == 0) return;
        
        // 가중치 기반 선택
        EnemySpawnData selectedEnemy = SelectEnemyByWeight(availableEnemies);
        
        if (selectedEnemy != null && Time.time >= selectedEnemy.lastSpawnTime + selectedEnemy.spawnCooldown)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                SpawnEnemy(selectedEnemy, spawnPosition);
                selectedEnemy.lastSpawnTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// 현재 웨이브에서 사용 가능한 적 목록
    /// </summary>
    private List<EnemySpawnData> GetAvailableEnemies()
    {
        List<EnemySpawnData> available = new List<EnemySpawnData>();
        
        foreach (var enemy in enemyTypes)
        {
            if (enemy.enemyPrefab != null && enemy.minWave <= currentWave)
            {
                available.Add(enemy);
            }
        }
        
        return available;
    }
    
    /// <summary>
    /// 가중치 기반 적 선택
    /// </summary>
    private EnemySpawnData SelectEnemyByWeight(List<EnemySpawnData> enemies)
    {
        float totalWeight = 0f;
        foreach (var enemy in enemies)
        {
            totalWeight += enemy.spawnWeight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var enemy in enemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return enemy;
            }
        }
        
        return enemies[0]; // 폴백
    }
    
    /// <summary>
    /// 랜덤 스폰 위치 생성
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        if (player == null) return Vector3.zero;
        
        int attempts = 0;
        int maxAttempts = 20;
        
        while (attempts < maxAttempts)
        {
            // 플레이어 주변 링 형태로 스폰
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = player.position + (Vector3)(randomDirection * spawnDistance);
            
            // 장애물 체크
            if (!Physics2D.OverlapCircle(spawnPos, 1f, obstacleLayer))
            {
                return spawnPos;
            }
            
            attempts++;
        }
        
        // 실패 시 기본 위치
        Vector2 fallbackDirection = Random.insideUnitCircle.normalized;
        return player.position + (Vector3)(fallbackDirection * spawnDistance);
    }
    
    /// <summary>
    /// 적 스폰
    /// </summary>
    public EnemyBase SpawnEnemy(EnemySpawnData enemyData, Vector3 position)
    {
        GameObject enemyObject = Instantiate(enemyData.enemyPrefab, position, Quaternion.identity);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        
        if (enemy != null)
        {
            // 난이도에 따른 스탯 조정
            ApplyDifficultyScaling(enemy);
            
            // 적 등록
            activeEnemies.Add(enemy);
            
            // 적 이벤트 연결
            enemy.events.OnDeath.AddListener(() => OnEnemyDied(enemy));
            
            events?.OnEnemySpawned?.Invoke(enemy);
            events?.OnEnemyCountChanged?.Invoke(activeEnemies.Count);
            
            return enemy;
        }
        else
        {
            Debug.LogError($"EnemyBase 컴포넌트가 없습니다: {enemyData.enemyName}");
            Destroy(enemyObject);
            return null;
        }
    }
    
    /// <summary>
    /// 특정 위치에 특정 적 스폰
    /// </summary>
    public EnemyBase SpawnEnemy(string enemyName, Vector3 position)
    {
        EnemySpawnData enemyData = enemyTypes.Find(e => e.enemyName == enemyName);
        if (enemyData != null)
        {
            return SpawnEnemy(enemyData, position);
        }
        
        Debug.LogError($"적을 찾을 수 없습니다: {enemyName}");
        return null;
    }
    
    /// <summary>
    /// 난이도 스케일링 적용
    /// </summary>
    private void ApplyDifficultyScaling(EnemyBase enemy)
    {
        // 체력 증가
        float healthMultiplier = 1f + (currentDifficultyMultiplier - 1f) * 0.8f;
        enemy.GetType().GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, enemy.MaxHealth * healthMultiplier);
        enemy.GetType().GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, enemy.MaxHealth);
        
        // 데미지 증가
        float damageMultiplier = 1f + (currentDifficultyMultiplier - 1f) * 0.6f;
        enemy.GetType().GetField("damage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, enemy.GetType().GetField("damage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(enemy));
        
        // 이동 속도 약간 증가
        float speedMultiplier = 1f + (currentDifficultyMultiplier - 1f) * 0.3f;
        enemy.GetType().GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(enemy, enemy.GetType().GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(enemy));
    }
    
    /// <summary>
    /// 적 사망 처리
    /// </summary>
    private void OnEnemyDied(EnemyBase enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            events?.OnEnemyDestroyed?.Invoke(enemy);
            events?.OnEnemyCountChanged?.Invoke(activeEnemies.Count);
        }
    }
    
    /// <summary>
    /// 적 강제 파괴
    /// </summary>
    public void DestroyEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            OnEnemyDied(enemy);
            Destroy(enemy.gameObject);
        }
    }
    
    /// <summary>
    /// 모든 적 파괴
    /// </summary>
    public void DestroyAllEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                DestroyEnemy(activeEnemies[i]);
            }
        }
        
        activeEnemies.Clear();
    }
    
    /// <summary>
    /// 스폰 레이트 설정
    /// </summary>
    public void SetSpawnRate(float newRate)
    {
        spawnRate = Mathf.Max(0.1f, newRate);
    }
    
    /// <summary>
    /// 최대 적 수 설정
    /// </summary>
    public void SetMaxEnemies(int newMax)
    {
        maxEnemies = Mathf.Max(1, newMax);
    }
    
    /// <summary>
    /// 매니저 정보
    /// </summary>
    public string GetManagerInfo()
    {
        return $"적 매니저 정보\n" +
               $"활성 적: {ActiveEnemyCount}/{MaxEnemies}\n" +
               $"현재 웨이브: {CurrentWave}\n" +
               $"난이도 배율: {DifficultyMultiplier:F2}\n" +
               $"스폰 레이트: {spawnRate:F1}/s\n" +
               $"다음 웨이브: {nextWaveTime - Time.time:F0}초 후";
    }
    
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // 스폰 거리
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, spawnDistance);
        
        // 제거 거리
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(player.position, despawnDistance);
        
        // 스폰 영역
        if (visualizeSpawnArea)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(player.position, spawnAreaSize);
        }
    }
}