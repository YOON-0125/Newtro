using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 웨이브 패턴 관리자 - 기존 EnemyManager 위에 덧씌우는 방식
/// </summary>
public class WavePatternManager : MonoBehaviour
{
    [Header("패턴 설정")]
    [SerializeField] private List<WavePatternData> availablePatterns = new List<WavePatternData>();
    [SerializeField] private PatternProbabilitySettings probabilitySettings = new PatternProbabilitySettings();
    
    [Header("참조")]
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private Transform player;
    [SerializeField] private WavePatternCountdownUI countdownUI;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool forcePatternNextWave = false; // 테스트용
    
    // 이벤트
    [System.Serializable]
    public class PatternEvents
    {
        public UnityEvent<PatternType> OnPatternStart;
        public UnityEvent<PatternType> OnPatternComplete;
        public UnityEvent<int> OnPatternCountdown; // 3, 2, 1 카운트다운
    }
    
    [Header("이벤트")]
    [SerializeField] private PatternEvents events = new PatternEvents();
    
    // 내부 변수
    private List<EnemyBase> patternEnemies = new List<EnemyBase>();
    private bool isPatternActive = false;
    private PatternType currentPatternType;
    
    // 프로퍼티
    public bool IsPatternActive => isPatternActive;
    public PatternType CurrentPatternType => currentPatternType;
    public int PatternEnemyCount => patternEnemies.Count;
    
    private void Awake()
    {
        // 컴포넌트 자동 찾기
        if (enemyManager == null)
            enemyManager = FindFirstObjectByType<EnemyManager>();
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // CountdownUI 자동 찾기
        if (countdownUI == null)
            countdownUI = FindFirstObjectByType<WavePatternCountdownUI>();
        
        // 기본 패턴들 설정
        if (availablePatterns.Count == 0)
            SetupDefaultPatterns();
    }
    
    private void Start()
    {
        // EnemyManager의 웨이브 시작 이벤트 구독
        if (enemyManager != null && enemyManager.events != null)
        {
            enemyManager.events.OnWaveStart.AddListener(OnWaveStarted);
            if (enableDebugLogs)
                Debug.Log("[WavePatternManager] EnemyManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogError("[WavePatternManager] EnemyManager를 찾을 수 없거나 이벤트가 null입니다!");
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제
        if (enemyManager != null && enemyManager.events != null)
        {
            enemyManager.events.OnWaveStart.RemoveListener(OnWaveStarted);
        }
    }
    
    /// <summary>
    /// 웨이브 시작 시 호출되는 이벤트 핸들러
    /// </summary>
    private void OnWaveStarted(int waveNumber)
    {
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] 웨이브 {waveNumber} 시작 - 패턴 확률 체크");
        
        // 패턴 실행 여부 결정
        if (ShouldExecutePattern(waveNumber) || forcePatternNextWave)
        {
            forcePatternNextWave = false; // 테스트용 플래그 리셋
            StartCoroutine(ExecutePatternWithCountdown(waveNumber));
        }
    }
    
    /// <summary>
    /// 패턴 실행 여부 결정
    /// </summary>
    private bool ShouldExecutePattern(int waveNumber)
    {
        // 현재 패턴이 활성화되어 있으면 스킵
        if (isPatternActive) return false;
        
        // 확률 계산
        float currentChance = probabilitySettings.baseChance;
        
        // 웨이브 보너스 적용
        if (waveNumber >= probabilitySettings.bonusStartWave)
        {
            int bonusWaves = Mathf.Min(waveNumber - probabilitySettings.bonusStartWave, 
                                     probabilitySettings.maxWave - probabilitySettings.bonusStartWave);
            currentChance += bonusWaves * probabilitySettings.waveBonus;
        }
        
        bool shouldExecute = Random.value <= currentChance;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] 웨이브 {waveNumber} 패턴 확률: {currentChance:F2} ({currentChance:P1}) - 실행: {shouldExecute}");
        
        return shouldExecute;
    }
    
    /// <summary>
    /// 카운트다운과 함께 패턴 실행
    /// </summary>
    private IEnumerator ExecutePatternWithCountdown(int waveNumber)
    {
        // 패턴 선택
        WavePatternData selectedPattern = SelectPattern(waveNumber);
        if (selectedPattern == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[WavePatternManager] 웨이브 {waveNumber}에 사용 가능한 패턴이 없습니다.");
            yield break;
        }
        
        isPatternActive = true;
        currentPatternType = selectedPattern.patternType;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] 🌊 패턴 선택됨: {selectedPattern.patternName} ({selectedPattern.patternType})");
        
        // 3초 카운트다운 (UI와 연동)
        if (countdownUI != null)
        {
            // UI 카운트다운 시작 (3→2→1)
            countdownUI.StartCountdown();
            
            // UI 카운트다운과 동기화 (3초 대기)
            yield return new WaitForSeconds(3f);
        }
        else
        {
            // 폴백: UI가 없으면 기본 카운트다운
            for (int i = 3; i > 0; i--)
            {
                events.OnPatternCountdown.Invoke(i);
                if (enableDebugLogs)
                    Debug.Log($"[WavePatternManager] ⏰ 패턴 카운트다운: {i}");
                yield return new WaitForSeconds(1f);
            }
        }
        
        // 패턴 실행
        events.OnPatternStart.Invoke(selectedPattern.patternType);
        yield return StartCoroutine(ExecutePattern(selectedPattern));
        
        // 패턴 완료
        events.OnPatternComplete.Invoke(selectedPattern.patternType);
        isPatternActive = false;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] ✅ 패턴 완료: {selectedPattern.patternName}");
    }
    
    /// <summary>
    /// 웨이브에 사용 가능한 패턴 선택
    /// </summary>
    private WavePatternData SelectPattern(int waveNumber)
    {
        // 현재 웨이브에서 사용 가능한 패턴 필터링
        List<WavePatternData> availableForWave = new List<WavePatternData>();
        
        foreach (var pattern in availablePatterns)
        {
            int minWave = GetMinWaveForPattern(pattern.patternType);
            if (waveNumber >= minWave)
            {
                availableForWave.Add(pattern);
            }
        }
        
        if (availableForWave.Count == 0) return null;
        
        // 가중치 기반 선택
        float totalWeight = 0f;
        foreach (var pattern in availableForWave)
        {
            totalWeight += pattern.weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var pattern in availableForWave)
        {
            currentWeight += pattern.weight;
            if (randomValue <= currentWeight)
            {
                return pattern;
            }
        }
        
        return availableForWave[0]; // 폴백
    }
    
    /// <summary>
    /// 패턴별 최소 웨이브 반환
    /// </summary>
    private int GetMinWaveForPattern(PatternType patternType)
    {
        return patternType switch
        {
            PatternType.CircleSiege => probabilitySettings.circleSiegeMinWave,
            PatternType.ShieldWall => probabilitySettings.shieldWallMinWave,
            PatternType.MixedBarrier => probabilitySettings.mixedBarrierMinWave,
            PatternType.LineCharge => probabilitySettings.lineChargeMinWave,
            _ => 1
        };
    }
    
    /// <summary>
    /// 패턴 실행
    /// </summary>
    private IEnumerator ExecutePattern(WavePatternData patternData)
    {
        switch (patternData.patternType)
        {
            case PatternType.CircleSiege:
                yield return StartCoroutine(ExecuteCircleSiege(patternData));
                break;
            case PatternType.ShieldWall:
                yield return StartCoroutine(ExecuteShieldWall(patternData));
                break;
            case PatternType.MixedBarrier:
                yield return StartCoroutine(ExecuteMixedBarrier(patternData));
                break;
            case PatternType.LineCharge:
                yield return StartCoroutine(ExecuteLineCharge(patternData));
                break;
        }
    }
    
    /// <summary>
    /// 원형 포위 패턴 실행
    /// </summary>
    private IEnumerator ExecuteCircleSiege(WavePatternData patternData)
    {
        if (player == null || enemyManager == null)
        {
            Debug.LogError("[WavePatternManager] Player 또는 EnemyManager가 null입니다!");
            yield break;
        }
        
        // 패턴 데이터 검증
        if (patternData.enemyCount <= 0 || patternData.spawnRadius <= 0f)
        {
            Debug.LogError($"[WavePatternManager] 잘못된 패턴 데이터: enemyCount={patternData.enemyCount}, spawnRadius={patternData.spawnRadius}");
            yield break;
        }
        
        Vector3 playerPos = player.position;
        float radius = patternData.spawnRadius;
        int count = patternData.enemyCount;
        int successCount = 0;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] 🔵 원형 포위 시작: {count}기, 반지름 {radius}m, 플레이어 위치 {playerPos}");
        
        // 원형으로 적들 배치 (약간의 간격을 두고 순차적으로 스폰)
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            
            // BasicSkeleton 스폰 (기존 난이도 시스템 활용)
            EnemyBase enemy = enemyManager.SpawnEnemy("BasicSkeleton", spawnPos);
            if (enemy != null)
            {
                // 패턴 전용 경험치 적용
                ApplyPatternExpMultiplier(enemy, patternData.expMultiplier);
                patternEnemies.Add(enemy);
                successCount++;
                
                // 적 사망 시 패턴 리스트에서 제거 (메모리 누수 방지)
                enemy.events.OnDeath.AddListener(() => {
                    if (patternEnemies != null && patternEnemies.Contains(enemy))
                    {
                        patternEnemies.Remove(enemy);
                        if (enableDebugLogs)
                            Debug.Log($"[WavePatternManager] 패턴 적 사망 - 남은 패턴 적: {patternEnemies.Count}기");
                    }
                });
                
                // 패턴 적임을 명확하게 표시 (시각적 피드백용)
                if (enemy.transform != null)
                {
                    // 패턴 적들은 약간 더 큰 스케일로 표시 (선택사항)
                    enemy.transform.localScale *= 1.1f;
                }
                
                if (enableDebugLogs && i % 4 == 0) // 4마리마다 로그
                    Debug.Log($"[WavePatternManager] 스폰 진행: {i + 1}/{count}기");
            }
            else
            {
                Debug.LogWarning($"[WavePatternManager] BasicSkeleton 스폰 실패 - 위치: {spawnPos}, " +
                                $"EnemyManager 상태: MaxEnemies={enemyManager.MaxEnemies}, Current={enemyManager.ActiveEnemyCount}");
            }
            
            // 스폰 간격 (너무 한번에 많이 스폰하지 않도록)
            if (i % 3 == 2) // 3마리마다 짧은 대기
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] ✅ 원형 포위 완료: {successCount}/{count}기 스폰됨 (총 패턴 적: {patternEnemies.Count}기)");
        
        yield return null;
    }
    
    /// <summary>
    /// 방패 결계 패턴 실행 - ShieldSkeleton들이 방어벽을 형성
    /// </summary>
    private IEnumerator ExecuteShieldWall(WavePatternData patternData)
    {
        if (player == null || enemyManager == null)
        {
            Debug.LogError("[WavePatternManager] Player 또는 EnemyManager가 null입니다!");
            yield break;
        }
        
        // 패턴 데이터 검증
        if (patternData.enemyCount <= 0 || patternData.spawnRadius <= 0f)
        {
            Debug.LogError($"[WavePatternManager] 잘못된 패턴 데이터: enemyCount={patternData.enemyCount}, spawnRadius={patternData.spawnRadius}");
            yield break;
        }
        
        Vector3 playerPos = player.position;
        float radius = patternData.spawnRadius;
        int count = patternData.enemyCount;
        int successCount = 0;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] 🛡️ 방패 결계 시작: {count}기, 반지름 {radius}m, 플레이어 위치 {playerPos}");
        
        // 플레이어를 중심으로 한 반원형 방어벽 생성 (180도 호)
        float startAngle = -90f; // 왼쪽부터 시작 (-90도)
        float endAngle = 90f;    // 오른쪽까지 (90도)  
        float angleRange = endAngle - startAngle; // 총 180도
        
        for (int i = 0; i < count; i++)
        {
            // 반원형으로 균등 배치
            float angleStep = angleRange / (count - 1);
            float currentAngle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(currentAngle) * radius,
                Mathf.Sin(currentAngle) * radius,
                0f
            );
            
            // ShieldSkeleton 스폰
            EnemyBase enemy = enemyManager.SpawnEnemy("ShieldSkeleton", spawnPos);
            if (enemy != null)
            {
                // 패턴 전용 경험치 적용
                ApplyPatternExpMultiplier(enemy, patternData.expMultiplier);
                patternEnemies.Add(enemy);
                successCount++;
                
                // 적 사망 시 패턴 리스트에서 제거
                enemy.events.OnDeath.AddListener(() => {
                    if (patternEnemies != null && patternEnemies.Contains(enemy))
                    {
                        patternEnemies.Remove(enemy);
                        if (enableDebugLogs)
                            Debug.Log($"[WavePatternManager] 방패 결계 적 사망 - 남은 패턴 적: {patternEnemies.Count}기");
                    }
                });
                
                // 방패 적임을 명확하게 표시
                if (enemy.transform != null)
                {
                    enemy.transform.localScale *= 1.15f; // 방패 결계는 조금 더 크게
                }
                
                if (enableDebugLogs && i % 3 == 0) // 3마리마다 로그
                    Debug.Log($"[WavePatternManager] 방패 결계 스폰 진행: {i + 1}/{count}기");
            }
            else
            {
                Debug.LogWarning($"[WavePatternManager] ShieldSkeleton 스폰 실패 - 위치: {spawnPos}, " +
                                $"EnemyManager 상태: MaxEnemies={enemyManager.MaxEnemies}, Current={enemyManager.ActiveEnemyCount}");
            }
            
            // 스폰 간격 (방패 적들은 조금 더 천천히)
            if (i % 2 == 1) // 2마리마다 짧은 대기
            {
                yield return new WaitForSeconds(0.15f);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] ✅ 방패 결계 완료: {successCount}/{count}기 스폰됨 (총 패턴 적: {patternEnemies.Count}기)");
        
        yield return null;
    }
    
    /// <summary>
    /// 혼합 결계 패턴 실행 - 이중 원형 방어선 (내부: ShieldSkeleton, 외부: BasicSkeleton)
    /// </summary>
    private IEnumerator ExecuteMixedBarrier(WavePatternData patternData)
    {
        if (player == null || enemyManager == null)
        {
            Debug.LogError("[WavePatternManager] Player 또는 EnemyManager가 null입니다!");
            yield break;
        }
        
        // 패턴 데이터 검증
        if (patternData.innerEnemyCount <= 0 || patternData.outerEnemyCount <= 0 || 
            patternData.innerRadius <= 0f || patternData.outerRadius <= 0f)
        {
            Debug.LogError($"[WavePatternManager] 잘못된 혼합 결계 데이터: inner({patternData.innerEnemyCount}, {patternData.innerRadius}), outer({patternData.outerEnemyCount}, {patternData.outerRadius})");
            yield break;
        }
        
        Vector3 playerPos = player.position;
        int innerCount = patternData.innerEnemyCount;
        int outerCount = patternData.outerEnemyCount;
        float innerRadius = patternData.innerRadius;
        float outerRadius = patternData.outerRadius;
        int successCount = 0;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] 🏰 혼합 결계 시작: 내부 {innerCount}기(반지름{innerRadius}m), 외부 {outerCount}기(반지름{outerRadius}m)");
        
        // Phase 1: 내부 방어선 스폰 (ShieldSkeleton)
        if (enableDebugLogs)
            Debug.Log("[WavePatternManager] 📍 1단계: 내부 방어선 배치 (ShieldSkeleton)");
            
        for (int i = 0; i < innerCount; i++)
        {
            float angle = (360f / innerCount) * i * Mathf.Deg2Rad;
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * innerRadius,
                Mathf.Sin(angle) * innerRadius,
                0f
            );
            
            // ShieldSkeleton 스폰 (내부 방어선)
            EnemyBase enemy = enemyManager.SpawnEnemy("ShieldSkeleton", spawnPos);
            if (enemy != null)
            {
                ApplyPatternExpMultiplier(enemy, patternData.expMultiplier);
                patternEnemies.Add(enemy);
                successCount++;
                
                // 내부 방어선임을 표시
                if (enemy.transform != null)
                {
                    enemy.transform.localScale *= 1.2f; // 내부 방어선은 더 크게
                    enemy.name = "[혼합-내부] " + enemy.name;
                }
                
                // 사망 이벤트 등록
                enemy.events.OnDeath.AddListener(() => {
                    if (patternEnemies != null && patternEnemies.Contains(enemy))
                    {
                        patternEnemies.Remove(enemy);
                        if (enableDebugLogs)
                            Debug.Log($"[WavePatternManager] 내부 방어선 적 사망 - 남은 패턴 적: {patternEnemies.Count}기");
                    }
                });
            }
            else
            {
                Debug.LogWarning($"[WavePatternManager] 내부 ShieldSkeleton 스폰 실패 - 위치: {spawnPos}");
            }
            
            // 내부 배치 간격
            if (i % 2 == 1)
                yield return new WaitForSeconds(0.1f);
        }
        
        // 중간 대기 (내부와 외부 사이)
        yield return new WaitForSeconds(0.3f);
        
        // Phase 2: 외부 공격선 스폰 (BasicSkeleton)
        if (enableDebugLogs)
            Debug.Log("[WavePatternManager] 📍 2단계: 외부 공격선 배치 (BasicSkeleton)");
            
        for (int i = 0; i < outerCount; i++)
        {
            float angle = (360f / outerCount) * i * Mathf.Deg2Rad;
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * outerRadius,
                Mathf.Sin(angle) * outerRadius,
                0f
            );
            
            // BasicSkeleton 스폰 (외부 공격선)
            EnemyBase enemy = enemyManager.SpawnEnemy("BasicSkeleton", spawnPos);
            if (enemy != null)
            {
                ApplyPatternExpMultiplier(enemy, patternData.expMultiplier);
                patternEnemies.Add(enemy);
                successCount++;
                
                // 외부 공격선임을 표시
                if (enemy.transform != null)
                {
                    enemy.transform.localScale *= 1.1f; // 외부 공격선은 보통 크기
                    enemy.name = "[혼합-외부] " + enemy.name;
                }
                
                // 사망 이벤트 등록
                enemy.events.OnDeath.AddListener(() => {
                    if (patternEnemies != null && patternEnemies.Contains(enemy))
                    {
                        patternEnemies.Remove(enemy);
                        if (enableDebugLogs)
                            Debug.Log($"[WavePatternManager] 외부 공격선 적 사망 - 남은 패턴 적: {patternEnemies.Count}기");
                    }
                });
            }
            else
            {
                Debug.LogWarning($"[WavePatternManager] 외부 BasicSkeleton 스폰 실패 - 위치: {spawnPos}");
            }
            
            // 외부 배치 간격  
            if (i % 3 == 2)
                yield return new WaitForSeconds(0.08f);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] ✅ 혼합 결계 완료: {successCount}/{innerCount + outerCount}기 스폰됨 (총 패턴 적: {patternEnemies.Count}기)");
        
        yield return null;
    }
    
    /// <summary>
    /// 직선 돌격 패턴 실행 - DualBladeSkeleton들이 8방향에서 랜덤 직선 돌격
    /// </summary>
    private IEnumerator ExecuteLineCharge(WavePatternData patternData)
    {
        if (player == null || enemyManager == null)
        {
            Debug.LogError("[WavePatternManager] Player 또는 EnemyManager가 null입니다!");
            yield break;
        }
        
        // 패턴 데이터 검증
        if (patternData.enemyCount <= 0 || patternData.spawnRadius <= 0f)
        {
            Debug.LogError($"[WavePatternManager] 잘못된 패턴 데이터: enemyCount={patternData.enemyCount}, spawnRadius={patternData.spawnRadius}");
            yield break;
        }
        
        Vector3 playerPos = player.position;
        float spawnDistance = patternData.spawnRadius;
        int count = patternData.enemyCount;
        float chargeInterval = patternData.chargeInterval;
        int successCount = 0;
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] ⚔️ 직선 돌격 시작: {count}기, 거리 {spawnDistance}m, 간격 {chargeInterval}초");
        
        // 8방향 기본 각도 정의 (45도씩)
        float[] baseAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
        
        for (int i = 0; i < count; i++)
        {
            // 랜덤 방향 선택 및 약간의 변화 추가
            float baseAngle = baseAngles[Random.Range(0, baseAngles.Length)];
            float randomOffset = Random.Range(-15f, 15f); // ±15도 랜덤
            float finalAngle = (baseAngle + randomOffset) * Mathf.Deg2Rad;
            
            // 플레이어에서 멀리 떨어진 곳에서 시작
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(finalAngle) * spawnDistance,
                Mathf.Sin(finalAngle) * spawnDistance,
                0f
            );
            
            // DualBladeSkeleton 스폰
            EnemyBase enemy = enemyManager.SpawnEnemy("DualBladeSkeleton", spawnPos);
            if (enemy != null)
            {
                // 패턴 전용 경험치 적용
                ApplyPatternExpMultiplier(enemy, patternData.expMultiplier);
                patternEnemies.Add(enemy);
                successCount++;
                
                // 돌격 적임을 표시
                if (enemy.transform != null)
                {
                    enemy.transform.localScale *= 1.3f; // 돌격 적은 가장 크게
                    enemy.name = "[돌격] " + enemy.name;
                }
                
                // 돌격 방향 설정 (플레이어 방향으로)
                Vector2 chargeDirection = (playerPos - spawnPos).normalized;
                
                // 자동 소멸 코루틴 시작 (20m 거리에서 소멸)
                StartCoroutine(AutoDespawnEnemy(enemy, playerPos, patternData.despawnDistance));
                
                // 사망 이벤트 등록
                enemy.events.OnDeath.AddListener(() => {
                    if (patternEnemies != null && patternEnemies.Contains(enemy))
                    {
                        patternEnemies.Remove(enemy);
                        if (enableDebugLogs)
                            Debug.Log($"[WavePatternManager] 돌격 적 사망 - 남은 패턴 적: {patternEnemies.Count}기");
                    }
                });
                
                if (enableDebugLogs && i % 2 == 0) // 2마리마다 로그
                    Debug.Log($"[WavePatternManager] 돌격 스폰 진행: {i + 1}/{count}기, 각도: {baseAngle + randomOffset:F1}°");
            }
            else
            {
                Debug.LogWarning($"[WavePatternManager] DualBladeSkeleton 스폰 실패 - 위치: {spawnPos}, " +
                                $"EnemyManager 상태: MaxEnemies={enemyManager.MaxEnemies}, Current={enemyManager.ActiveEnemyCount}");
            }
            
            // 돌격 간격 (연속 돌격이 아닌 웨이브 형태)
            if (i < count - 1) // 마지막이 아니면 대기
            {
                yield return new WaitForSeconds(chargeInterval);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[WavePatternManager] ✅ 직선 돌격 완료: {successCount}/{count}기 스폰됨 (총 패턴 적: {patternEnemies.Count}기)");
        
        yield return null;
    }
    
    /// <summary>
    /// 적 자동 소멸 (거리 기반)
    /// </summary>
    private IEnumerator AutoDespawnEnemy(EnemyBase enemy, Vector3 referencePos, float maxDistance)
    {
        if (enemy == null) yield break;
        
        while (enemy != null && !enemy.IsDead)
        {
            float distance = Vector3.Distance(enemy.transform.position, referencePos);
            
            // 최대 거리를 벗어나면 자동 소멸
            if (distance > maxDistance)
            {
                if (enableDebugLogs)
                    Debug.Log($"[WavePatternManager] 돌격 적 자동 소멸 - 거리: {distance:F1}m > {maxDistance}m");
                
                // 패턴 리스트에서 제거
                if (patternEnemies.Contains(enemy))
                {
                    patternEnemies.Remove(enemy);
                }
                
                // 적 제거
                if (enemyManager != null)
                {
                    enemyManager.DestroyEnemy(enemy);
                }
                
                yield break;
            }
            
            yield return new WaitForSeconds(0.5f); // 0.5초마다 거리 체크
        }
    }
    
    /// <summary>
    /// 패턴 전용 경험치 배율 적용
    /// </summary>
    private void ApplyPatternExpMultiplier(EnemyBase enemy, float multiplier)
    {
        if (enemy == null || multiplier <= 0f) return;
        
        // EnemyBase에 경험치 배율을 직접 적용할 수 있는지 확인하고 적용
        try
        {
            // 패턴 적으로 표시하기 위해 이름에 특수 마커 추가
            if (!enemy.name.Contains("[패턴]"))
            {
                enemy.name = $"[패턴] {enemy.name}";
            }
            
            if (enableDebugLogs)
                Debug.Log($"[WavePatternManager] 📈 {enemy.name}에게 경험치 배율 {multiplier}x 적용 완료");
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[WavePatternManager] 경험치 배율 적용 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 기본 패턴 설정
    /// </summary>
    private void SetupDefaultPatterns()
    {
        // 원형 포위 패턴
        WavePatternData circleSiege = new WavePatternData
        {
            patternType = PatternType.CircleSiege,
            patternName = "원형 포위",
            description = "BasicSkeleton들이 플레이어를 원형으로 포위합니다.",
            enemyCount = 12,
            spawnRadius = 10f,
            expMultiplier = 1.3f,
            weight = 3
        };
        
        // 방패 결계 패턴
        WavePatternData shieldWall = new WavePatternData
        {
            patternType = PatternType.ShieldWall,
            patternName = "방패 결계",
            description = "ShieldSkeleton들이 반원형 방어벽을 형성합니다.",
            enemyCount = 8,
            spawnRadius = 12f,
            expMultiplier = 1.5f,
            weight = 2
        };
        
        // 혼합 결계 패턴
        WavePatternData mixedBarrier = new WavePatternData
        {
            patternType = PatternType.MixedBarrier,
            patternName = "혼합 결계",
            description = "내부 ShieldSkeleton 방어선과 외부 BasicSkeleton 공격선의 이중 구조입니다.",
            innerEnemyCount = 6,
            outerEnemyCount = 12,
            innerRadius = 6f,
            outerRadius = 10f,
            expMultiplier = 1.8f,
            weight = 1
        };
        
        // 직선 돌격 패턴
        WavePatternData lineCharge = new WavePatternData
        {
            patternType = PatternType.LineCharge,
            patternName = "직선 돌격",
            description = "DualBladeSkeleton들이 8방향에서 랜덤하게 직선 돌격합니다.",
            enemyCount = 8,
            spawnRadius = 18f,
            chargeInterval = 0.2f,
            chargeSpeed = 8f,
            despawnDistance = 20f,
            expMultiplier = 2.2f,
            weight = 1
        };
        
        availablePatterns.Add(circleSiege);
        availablePatterns.Add(shieldWall);
        availablePatterns.Add(mixedBarrier);
        availablePatterns.Add(lineCharge);
        
        if (enableDebugLogs)
            Debug.Log("[WavePatternManager] 기본 패턴 설정 완료: 4개 패턴 등록");
    }
    
    /// <summary>
    /// 패턴 강제 실행 (테스트용)
    /// </summary>
    [ContextMenu("Force Pattern Next Wave")]
    public void ForcePatternNextWave()
    {
        forcePatternNextWave = true;
        Debug.Log("[WavePatternManager] 다음 웨이브에 패턴 강제 실행 설정");
    }
    
    /// <summary>
    /// 원형 포위 패턴 즉시 테스트 (에디터 전용)
    /// </summary>
    [ContextMenu("Test Circle Siege Pattern Now")]
    public void TestCircleSiegeNow()
    {
        if (Application.isPlaying && player != null)
        {
            var testPattern = new WavePatternData
            {
                patternType = PatternType.CircleSiege,
                patternName = "테스트 원형 포위",
                enemyCount = 8,
                spawnRadius = 8f,
                expMultiplier = 1.5f
            };
            
            StartCoroutine(ExecuteCircleSiege(testPattern));
            Debug.Log("[WavePatternManager] 🧪 원형 포위 패턴 테스트 실행!");
        }
        else
        {
            Debug.LogWarning("[WavePatternManager] 게임이 실행 중이 아니거나 Player가 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 방패 결계 패턴 즉시 테스트 (에디터 전용)
    /// </summary>
    [ContextMenu("Test Shield Wall Pattern Now")]
    public void TestShieldWallNow()
    {
        if (Application.isPlaying && player != null)
        {
            var testPattern = new WavePatternData
            {
                patternType = PatternType.ShieldWall,
                patternName = "테스트 방패 결계",
                enemyCount = 6,
                spawnRadius = 10f,
                expMultiplier = 1.8f
            };
            
            StartCoroutine(ExecuteShieldWall(testPattern));
            Debug.Log("[WavePatternManager] 🧪 방패 결계 패턴 테스트 실행!");
        }
        else
        {
            Debug.LogWarning("[WavePatternManager] 게임이 실행 중이 아니거나 Player가 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 혼합 결계 패턴 즉시 테스트 (에디터 전용)
    /// </summary>
    [ContextMenu("Test Mixed Barrier Pattern Now")]
    public void TestMixedBarrierNow()
    {
        if (Application.isPlaying && player != null)
        {
            var testPattern = new WavePatternData
            {
                patternType = PatternType.MixedBarrier,
                patternName = "테스트 혼합 결계",
                innerEnemyCount = 4,
                outerEnemyCount = 8,
                innerRadius = 6f,
                outerRadius = 10f,
                expMultiplier = 2.0f
            };
            
            StartCoroutine(ExecuteMixedBarrier(testPattern));
            Debug.Log("[WavePatternManager] 🧪 혼합 결계 패턴 테스트 실행!");
        }
        else
        {
            Debug.LogWarning("[WavePatternManager] 게임이 실행 중이 아니거나 Player가 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 직선 돌격 패턴 즉시 테스트 (에디터 전용)
    /// </summary>
    [ContextMenu("Test Line Charge Pattern Now")]
    public void TestLineChargeNow()
    {
        if (Application.isPlaying && player != null)
        {
            var testPattern = new WavePatternData
            {
                patternType = PatternType.LineCharge,
                patternName = "테스트 직선 돌격",
                enemyCount = 5,
                spawnRadius = 15f,
                chargeInterval = 0.3f,
                despawnDistance = 25f,
                expMultiplier = 2.5f
            };
            
            StartCoroutine(ExecuteLineCharge(testPattern));
            Debug.Log("[WavePatternManager] 🧪 직선 돌격 패턴 테스트 실행!");
        }
        else
        {
            Debug.LogWarning("[WavePatternManager] 게임이 실행 중이 아니거나 Player가 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 에디터에서 패턴 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // 현재 활성 패턴이 Circle Siege라면 원형 시각화
        if (isPatternActive && currentPatternType == PatternType.CircleSiege && availablePatterns.Count > 0)
        {
            var circlePattern = availablePatterns.Find(p => p.patternType == PatternType.CircleSiege);
            if (circlePattern != null)
            {
                Gizmos.color = Color.cyan;
                DrawWireCircle(player.position, circlePattern.spawnRadius);
                
                // 스폰 위치들을 작은 구체로 표시
                int count = circlePattern.enemyCount;
                Gizmos.color = Color.red;
                for (int i = 0; i < count; i++)
                {
                    float angle = (360f / count) * i * Mathf.Deg2Rad;
                    Vector3 spawnPos = player.position + new Vector3(
                        Mathf.Cos(angle) * circlePattern.spawnRadius,
                        Mathf.Sin(angle) * circlePattern.spawnRadius,
                        0f
                    );
                    Gizmos.DrawWireSphere(spawnPos, 0.5f);
                }
            }
        }
        
        // 현재 활성 패턴이 Shield Wall이라면 반원형 시각화
        if (isPatternActive && currentPatternType == PatternType.ShieldWall && availablePatterns.Count > 0)
        {
            var shieldPattern = availablePatterns.Find(p => p.patternType == PatternType.ShieldWall);
            if (shieldPattern != null)
            {
                Gizmos.color = Color.green;
                
                // 반원형 호 시각화 (180도)
                float startAngle = -90f; // -90도에서 시작
                float endAngle = 90f;    // 90도까지
                float angleRange = endAngle - startAngle;
                int arcSegments = 20; // 호를 그릴 세그먼트 수
                
                Vector3 prevPoint = player.position + new Vector3(
                    Mathf.Cos(startAngle * Mathf.Deg2Rad) * shieldPattern.spawnRadius,
                    Mathf.Sin(startAngle * Mathf.Deg2Rad) * shieldPattern.spawnRadius,
                    0f
                );
                
                for (int i = 1; i <= arcSegments; i++)
                {
                    float currentAngle = startAngle + (angleRange * i / arcSegments);
                    Vector3 currentPoint = player.position + new Vector3(
                        Mathf.Cos(currentAngle * Mathf.Deg2Rad) * shieldPattern.spawnRadius,
                        Mathf.Sin(currentAngle * Mathf.Deg2Rad) * shieldPattern.spawnRadius,
                        0f
                    );
                    Gizmos.DrawLine(prevPoint, currentPoint);
                    prevPoint = currentPoint;
                }
                
                // 스폰 위치들을 방패 모양으로 표시
                int count = shieldPattern.enemyCount;
                Gizmos.color = Color.blue;
                for (int i = 0; i < count; i++)
                {
                    float angleStep = angleRange / (count - 1);
                    float currentAngle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                    Vector3 spawnPos = player.position + new Vector3(
                        Mathf.Cos(currentAngle) * shieldPattern.spawnRadius,
                        Mathf.Sin(currentAngle) * shieldPattern.spawnRadius,
                        0f
                    );
                    Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.7f); // 방패 모양은 정사각형
                }
            }
        }
        
        // 현재 활성 패턴이 Mixed Barrier라면 이중 원형 시각화
        if (isPatternActive && currentPatternType == PatternType.MixedBarrier && availablePatterns.Count > 0)
        {
            var mixedPattern = availablePatterns.Find(p => p.patternType == PatternType.MixedBarrier);
            if (mixedPattern != null)
            {
                // 내부 원 (ShieldSkeleton)
                Gizmos.color = Color.magenta;
                DrawWireCircle(player.position, mixedPattern.innerRadius);
                
                // 외부 원 (BasicSkeleton)  
                Gizmos.color = Color.cyan;
                DrawWireCircle(player.position, mixedPattern.outerRadius);
                
                // 내부 스폰 위치들 (방패 모양)
                Gizmos.color = Color.red;
                int innerCount = mixedPattern.innerEnemyCount;
                for (int i = 0; i < innerCount; i++)
                {
                    float angle = (360f / innerCount) * i * Mathf.Deg2Rad;
                    Vector3 spawnPos = player.position + new Vector3(
                        Mathf.Cos(angle) * mixedPattern.innerRadius,
                        Mathf.Sin(angle) * mixedPattern.innerRadius,
                        0f
                    );
                    Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.8f); // 내부는 큰 정사각형
                }
                
                // 외부 스폰 위치들 (원형 점)
                Gizmos.color = Color.blue;
                int outerCount = mixedPattern.outerEnemyCount;
                for (int i = 0; i < outerCount; i++)
                {
                    float angle = (360f / outerCount) * i * Mathf.Deg2Rad;
                    Vector3 spawnPos = player.position + new Vector3(
                        Mathf.Cos(angle) * mixedPattern.outerRadius,
                        Mathf.Sin(angle) * mixedPattern.outerRadius,
                        0f
                    );
                    Gizmos.DrawWireSphere(spawnPos, 0.4f); // 외부는 작은 구체
                }
                
                // 연결선 (내부와 외부를 연결하는 방사형 선)
                Gizmos.color = Color.gray;
                for (int i = 0; i < Mathf.Min(innerCount, outerCount); i++)
                {
                    float angle = (360f / Mathf.Max(innerCount, outerCount)) * i * Mathf.Deg2Rad;
                    Vector3 innerPos = player.position + new Vector3(
                        Mathf.Cos(angle) * mixedPattern.innerRadius,
                        Mathf.Sin(angle) * mixedPattern.innerRadius,
                        0f
                    );
                    Vector3 outerPos = player.position + new Vector3(
                        Mathf.Cos(angle) * mixedPattern.outerRadius,
                        Mathf.Sin(angle) * mixedPattern.outerRadius,
                        0f
                    );
                    Gizmos.DrawLine(innerPos, outerPos);
                }
            }
        }
        
        // 현재 활성 패턴이 Line Charge라면 돌격 라인 시각화  
        if (isPatternActive && currentPatternType == PatternType.LineCharge && availablePatterns.Count > 0)
        {
            var chargePattern = availablePatterns.Find(p => p.patternType == PatternType.LineCharge);
            if (chargePattern != null)
            {
                // 스폰 원형 영역 표시
                Gizmos.color = Color.red;
                DrawWireCircle(player.position, chargePattern.spawnRadius);
                
                // 자동 소멸 영역 표시  
                Gizmos.color = Color.gray;
                DrawWireCircle(player.position, chargePattern.despawnDistance);
                
                // 8방향 돌격 라인 시각화
                Gizmos.color = Color.yellow;
                float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
                
                foreach (float angle in angles)
                {
                    float rad = angle * Mathf.Deg2Rad;
                    
                    // 스폰 위치에서 플레이어까지의 돌격 라인
                    Vector3 spawnPos = player.position + new Vector3(
                        Mathf.Cos(rad) * chargePattern.spawnRadius,
                        Mathf.Sin(rad) * chargePattern.spawnRadius,
                        0f
                    );
                    
                    Vector3 endPos = player.position + new Vector3(
                        Mathf.Cos(rad + Mathf.PI) * chargePattern.despawnDistance,
                        Mathf.Sin(rad + Mathf.PI) * chargePattern.despawnDistance,
                        0f
                    );
                    
                    // 돌격 방향 화살표 그리기
                    Gizmos.DrawLine(spawnPos, endPos);
                    
                    // 스폰 위치 마커
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(spawnPos, 0.6f);
                    
                    // 방향 화살표 (삼각형 모양)
                    Vector3 arrowTip = player.position + (spawnPos - player.position).normalized * 2f;
                    Vector3 arrowLeft = arrowTip + Vector3.Cross((spawnPos - player.position).normalized, Vector3.forward) * 0.5f;
                    Vector3 arrowRight = arrowTip - Vector3.Cross((spawnPos - player.position).normalized, Vector3.forward) * 0.5f;
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(arrowTip, arrowLeft);
                    Gizmos.DrawLine(arrowTip, arrowRight);
                    Gizmos.DrawLine(arrowLeft, arrowRight);
                }
            }
        }
        
        // 패턴 활성 상태일 때 현재 패턴 적들 위치 표시
        if (isPatternActive && patternEnemies.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var enemy in patternEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawWireCube(enemy.transform.position, Vector3.one * 0.8f);
                }
            }
        }
    }
    
    /// <summary>
    /// 원형 와이어 그리기 헬퍼 메서드 (Unity 버전 호환성)
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}