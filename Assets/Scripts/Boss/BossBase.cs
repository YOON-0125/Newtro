using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모든 보스의 기본이 되는 클래스 (EnemyBase 상속)
/// 전투 패턴은 미구현, 기본 기능만 포함
/// </summary>
public class BossBase : EnemyBase
{
    [Header("보스 기본 설정")]
    [SerializeField] protected string bossName = "Unknown Boss";
    [SerializeField] protected BossType bossType = BossType.Fire;
    [SerializeField] protected Sprite bossIcon;
    [SerializeField] protected Color bossColor = Color.white;
    
    [Header("보스 스탯")]
    [SerializeField] protected float bossMaxHealth = 1000f;
    [SerializeField] protected float bossDamage = 50f;
    [SerializeField] protected float bossMoveSpeed = 3f;
    
    [Header("보스 특성")]
    [SerializeField] protected bool isImmuneToCrowdControl = true; // 상태이상 면역
    [SerializeField] protected float statusEffectResistance = 0.5f; // 상태이상 저항 (50%)
    [SerializeField] protected bool canBeKnockedBack = false; // 넉백 면역
    
    [Header("보스 전투 설정")]
    [SerializeField] protected float[] phaseHealthThresholds = { 0.7f, 0.4f }; // 페이즈 전환 체력 (70%, 40%)
    [SerializeField] protected int currentPhase = 1;
    [SerializeField] protected int maxPhases = 3;
    
    [Header("전투 패턴 기본 설정")]
    [SerializeField] protected float patternCooldownMin = 2f; // 패턴 간 최소 대기시간
    [SerializeField] protected float patternCooldownMax = 4f; // 패턴 간 최대 대기시간
    [SerializeField] protected bool enableDebugPattern = true; // 패턴 디버그 로그
    
    [Header("패턴 선택 가중치")]
    [SerializeField] protected int chargeWeight = 1; // 돌진 패턴 가중치
    [SerializeField] protected int projectileWeight = 1; // 투사체 패턴 가중치  
    [SerializeField] protected int summonWeight = 1; // 소환 패턴 가중치
    
    [Header("돌진 패턴 설정")]
    [SerializeField] protected float chargeSpeed = 15f; // 돌진 속도
    [SerializeField] protected float chargePrepareTime = 1f; // 돌진 예고 시간
    [SerializeField] protected float chargeDuration = 1f; // 돌진 지속 시간
    [SerializeField] protected float chargeStunTime = 0.8f; // 돌진 후 경직 시간
    [SerializeField] protected float chargeDamage = 20f; // 돌진 데미지
    
    [Header("돌진 패턴 - 1페이즈")]
    [SerializeField] protected int phase1ChargeCount = 1; // 1페이즈 돌진 횟수
    [SerializeField] protected bool phase1WallBounce = false; // 1페이즈 벽 튕김
    
    [Header("돌진 패턴 - 2페이즈")]
    [SerializeField] protected int phase2ChargeCountMin = 1; // 2페이즈 최소 돌진 횟수
    [SerializeField] protected int phase2ChargeCountMax = 3; // 2페이즈 최대 돌진 횟수
    [SerializeField] protected bool phase2WallBounce = true; // 2페이즈 벽 튕김
    [SerializeField] protected float wallBounceSpeedMultiplier = 0.7f; // 벽 튕김 후 속도 배율
    
    [Header("투사체 패턴 설정")]
    [SerializeField] protected GameObject projectilePrefab; // 투사체 프리팹
    [SerializeField] protected float projectileSpeed = 8f; // 투사체 속도
    [SerializeField] protected float projectileDamage = 10f; // 투사체 데미지
    [SerializeField] protected Transform projectileSpawnPoint; // 투사체 발사 위치
    
    [Header("투사체 패턴 - 1페이즈")]
    [SerializeField] protected float phase1SingleShotDelay = 0.3f; // 단발 발사 딜레이
    [SerializeField] protected int phase1BurstCount = 3; // 3연발 개수
    [SerializeField] protected float phase1BurstInterval = 0.2f; // 3연발 간격
    
    [Header("투사체 패턴 - 2페이즈")]
    [SerializeField] protected int phase2FanProjectileCount = 5; // 부채꼴 투사체 개수
    [SerializeField] protected float phase2FanAngle = 60f; // 부채꼴 각도 범위
    [SerializeField] protected int phase2CircleProjectileCount = 8; // 원형 투사체 개수
    
    [Header("하수인 소환 패턴 설정")]
    [SerializeField] protected Transform spawnArea; // 소환 범위 (Collider나 Transform)
    [SerializeField] protected float spawnAreaRadius = 5f; // 소환 범위 반지름 (spawnArea가 없을 때)
    
    [Header("하수인 소환 - 1페이즈")]
    [SerializeField] protected float phase1SummonCastTime = 0.5f; // 1페이즈 소환 시전시간
    [SerializeField] protected int phase1MinionCount = 2; // 1페이즈 소환 개수
    [SerializeField] protected GameObject[] phase1MinionPrefabs; // 1페이즈 소환할 적들 (BasicSkeleton 등)
    
    [Header("하수인 소환 - 2페이즈")]
    [SerializeField] protected float phase2SummonCastTime = 1.2f; // 2페이즈 소환 시전시간
    [SerializeField] protected int phase2MinionCount = 3; // 2페이즈 소환 개수
    [SerializeField] protected GameObject[] phase2MinionPrefabs; // 2페이즈 소환할 적들 (Shield, Magic, DualBlade 등)
    
    // 보스 이벤트 시스템
    [System.Serializable]
    public class BossEvents
    {
        public UnityEvent OnBossSpawned;
        public UnityEvent OnBossDefeated;
        public UnityEvent<int> OnPhaseChanged; // 페이즈 변경 시
        public UnityEvent<float> OnHealthPercentageChanged; // 체력 비율 변경
    }
    
    [Header("보스 이벤트")]
    [SerializeField] protected BossEvents bossEvents;
    
    // 보스 처치 이벤트 (BossManager에서 구독)
    public System.Action<BossBase> OnBossDefeated;
    
    // State Machine
    protected enum BossState
    {
        Idle,               // 기본 추적 상태
        PatternCooldown,    // 패턴 사용 후 대기
        ChargePrepare,      // 돌진 준비
        Charging,           // 돌진 중
        ChargeStunned,      // 돌진 후 경직
        ShootingPrepare,    // 투사체 발사 준비
        Shooting,           // 투사체 발사 중
        SummonPrepare,      // 소환 준비
        Summoning,          // 소환 중
        SummonComplete      // 소환 완료
    }
    
    [Header("State Machine 디버깅")]
    [SerializeField] protected BossState currentBossState = BossState.Idle;
    
    // 패턴 실행 관련 변수들
    protected float patternCooldownTimer = 0f;
    protected float stateTimer = 0f;
    protected int currentChargeCount = 0;
    protected int maxChargeCount = 1;
    protected Vector3 chargeDirection;
    protected Vector3 chargeStartPosition;
    protected bool enableWallBounce = false;
    
    // 돌진 시각 효과 관련
    protected GameObject chargeWarningIndicator;
    protected SpriteRenderer chargeWarningRenderer;
    
    // 투사체 패턴 관련
    protected int currentBurstCount = 0;
    protected float burstTimer = 0f;
    
    // 소환 패턴 관련
    protected float summonCastTimer = 0f;
    
    // 프로퍼티
    public string BossName => bossName;
    public BossType BossType => bossType;
    public Sprite BossIcon => bossIcon;
    public Color BossColor => bossColor;
    public int CurrentPhase => currentPhase;
    public int MaxPhases => maxPhases;
    public BossEvents Events => bossEvents;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        // 보스 전용 초기화
        InitializeBossStats();
        
        // 보스 이름이 비어있으면 기본값 설정
        if (string.IsNullOrEmpty(bossName))
        {
            bossName = $"{bossType} Boss";
        }
        
        enemyName = bossName; // EnemyBase의 enemyName도 설정
        
        Debug.Log($"[BossBase] 보스 초기화 완료: {bossName} (체력: {maxHealth})");
        
        // 보스 스폰 이벤트
        bossEvents?.OnBossSpawned?.Invoke();
    }
    
    /// <summary>
    /// 보스 데이터로 초기화 (BossManager에서 호출)
    /// </summary>
    public virtual void InitializeBoss(BossData bossData)
    {
        bossName = bossData.bossName;
        bossType = bossData.bossType;
        bossIcon = bossData.bossIcon;
        bossColor = bossData.bossColor;
        
        // 스탯 설정
        bossMaxHealth = bossData.maxHealth;
        bossDamage = bossData.damage;
        bossMoveSpeed = bossData.moveSpeed;
        
        // EnemyBase 스탯에도 적용
        maxHealth = bossMaxHealth;
        currentHealth = maxHealth;
        damage = bossDamage;
        moveSpeed = bossMoveSpeed;
        
        Debug.Log($"[BossBase] 보스 데이터로 초기화: {bossName}");
    }
    
    /// <summary>
    /// 보스 스탯 초기화
    /// </summary>
    protected virtual void InitializeBossStats()
    {
        // Inspector 설정을 EnemyBase 변수에 적용
        maxHealth = bossMaxHealth;
        currentHealth = maxHealth;
        damage = bossDamage;
        moveSpeed = bossMoveSpeed;
        
        // 보스는 기본적으로 높은 체력과 경험치 가치
        if (expValue < 100)
        {
            expValue = 100; // 최소 100 경험치
        }
        
        // 보스는 기본적으로 넓은 감지 범위
        if (detectionRange < 30f)
        {
            detectionRange = 30f;
        }
    }
    
    /// <summary>
    /// 보스 행동 업데이트 (State Machine 기반)
    /// </summary>
    protected override void UpdateBehavior()
    {
        // 페이즈 체크
        CheckPhaseTransition();
        
        // State Machine 업데이트
        UpdateStateMachine();
    }
    
    /// <summary>
    /// State Machine 업데이트
    /// </summary>
    protected virtual void UpdateStateMachine()
    {
        stateTimer += Time.deltaTime;
        
        switch (currentBossState)
        {
            case BossState.Idle:
                UpdateIdleState();
                break;
                
            case BossState.PatternCooldown:
                UpdatePatternCooldownState();
                break;
                
            case BossState.ChargePrepare:
                UpdateChargePrepareState();
                break;
                
            case BossState.Charging:
                UpdateChargingState();
                break;
                
            case BossState.ChargeStunned:
                UpdateChargeStunnedState();
                break;
                
            case BossState.ShootingPrepare:
                UpdateShootingPrepareState();
                break;
                
            case BossState.Shooting:
                UpdateShootingState();
                break;
                
            case BossState.SummonPrepare:
                UpdateSummonPrepareState();
                break;
                
            case BossState.Summoning:
                UpdateSummoningState();
                break;
                
            case BossState.SummonComplete:
                UpdateSummonCompleteState();
                break;
        }
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    protected virtual void ChangeState(BossState newState)
    {
        Debug.Log($"[BossBase] {bossName} 상태 변경: {currentBossState} → {newState}");
        
        currentBossState = newState;
        stateTimer = 0f;
        
        // 상태 진입 시 초기화
        OnStateEnter(newState);
    }
    
    /// <summary>
    /// 상태 진입 시 초기화
    /// </summary>
    protected virtual void OnStateEnter(BossState state)
    {
        switch (state)
        {
            case BossState.ChargePrepare:
                SetupChargePattern();
                break;
                
            case BossState.ShootingPrepare:
                SetupShootingPattern();
                break;
                
            case BossState.SummonPrepare:
                SetupSummonPattern();
                break;
        }
    }
    
    /// <summary>
    /// 보스 공격 실행 (미구현 - 기본 충돌 데미지만)
    /// </summary>
    protected override void ExecuteAttack()
    {
        // TODO: 보스 전용 공격 패턴 구현
        // 현재는 기본 공격만 수행
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        Debug.Log($"[BossBase] {bossName} 기본 공격 실행");
        
        // 짧은 대기 후 공격 완료
        Invoke(nameof(OnAttackComplete), 1f);
    }
    
    /// <summary>
    /// 보스 데미지 처리 (상태이상 저항 적용)
    /// </summary>
    public override void TakeDamage(float damageAmount, DamageTag tag = DamageTag.Physical)
    {
        if (isDead) return;
        
        // 보스는 상태이상에 저항력이 있음
        if (statusController != null && statusEffectResistance > 0f)
        {
            float resistance = statusEffectResistance;
            
            // 특정 상태이상에 완전 면역
            if (isImmuneToCrowdControl && (tag == DamageTag.Lightning || tag == DamageTag.Ice))
            {
                resistance = 1f; // 100% 저항
            }
            
            damageAmount *= (1f - resistance);
        }
        
        float previousHealthPercentage = HealthPercentage;
        
        // 기본 데미지 처리
        base.TakeDamage(damageAmount, tag);
        
        // 체력 비율 변경 이벤트
        if (!Mathf.Approximately(previousHealthPercentage, HealthPercentage))
        {
            bossEvents?.OnHealthPercentageChanged?.Invoke(HealthPercentage);
        }
    }
    
    /// <summary>
    /// 페이즈 전환 체크
    /// </summary>
    protected virtual void CheckPhaseTransition()
    {
        if (isDead || phaseHealthThresholds == null) return;
        
        float healthPercentage = HealthPercentage;
        int targetPhase = 1;
        
        // 현재 체력에 따른 페이즈 계산
        for (int i = 0; i < phaseHealthThresholds.Length; i++)
        {
            if (healthPercentage <= phaseHealthThresholds[i])
            {
                targetPhase = i + 2; // 1페이즈는 기본, 2페이즈부터 시작
            }
        }
        
        // 페이즈 전환
        if (targetPhase != currentPhase && targetPhase <= maxPhases)
        {
            ChangePhase(targetPhase);
        }
    }
    
    /// <summary>
    /// 페이즈 변경 (미구현 - 이벤트만 발생)
    /// </summary>
    protected virtual void ChangePhase(int newPhase)
    {
        int previousPhase = currentPhase;
        currentPhase = newPhase;
        
        Debug.Log($"[BossBase] {bossName} 페이즈 변경: {previousPhase} → {currentPhase}");
        
        // TODO: 페이즈별 전투 패턴 변경 구현
        
        // 페이즈 변경 이벤트
        bossEvents?.OnPhaseChanged?.Invoke(currentPhase);
    }
    
    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    protected override void Die()
    {
        if (isDead) return;
        
        Debug.Log($"[BossBase] {bossName} 처치됨!");
        
        // 사망 시 돌진 예고 효과도 정리
        DestroyChargeWarningIndicator();
        
        // 보스 처치 이벤트 (BossManager가 구독)
        OnBossDefeated?.Invoke(this);
        
        // 보스 이벤트
        bossEvents?.OnBossDefeated?.Invoke();
        
        // 기본 사망 처리
        base.Die();
    }
    
    /// <summary>
    /// 넉백 처리 오버라이드 (보스는 넉백 면역 옵션)
    /// </summary>
    protected override void OnHurt()
    {
        if (!canBeKnockedBack)
        {
            // 넉백 효과 제거하고 기본 피격 처리만
            return;
        }
        
        base.OnHurt();
    }
    
    /// <summary>
    /// 보스 정보 반환
    /// </summary>
    public override string GetEnemyInfo()
    {
        return $"{bossName} (보스)\n" +
               $"타입: {bossType}\n" +
               $"페이즈: {currentPhase}/{maxPhases}\n" +
               $"체력: {currentHealth:F0}/{maxHealth:F0} ({HealthPercentage * 100:F1}%)\n" +
               $"상태: {currentBossState}";
    }
    
    /// <summary>
    /// 에디터에서 보스 시각화
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 보스 타입에 따른 색상으로 추가 표시
        Gizmos.color = bossColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // 페이즈 전환 체력 표시
        if (phaseHealthThresholds != null)
        {
            for (int i = 0; i < phaseHealthThresholds.Length; i++)
            {
                float healthThreshold = phaseHealthThresholds[i];
                Gizmos.color = Color.Lerp(Color.green, Color.red, 1f - healthThreshold);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * (i + 1) * 0.5f, 0.2f);
            }
        }
    }
    
    #region State Machine 구현
    
    /// <summary>
    /// Idle 상태 - 기본 추적 및 패턴 선택
    /// </summary>
    protected virtual void UpdateIdleState()
    {
        // 기본 추적 행동
        base.UpdateBehavior();
        
        // 패턴 발동 조건 체크 (거리 기반, 2D)
        if (target != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, target.position);
            
            if (enableDebugPattern)
            {
                Debug.Log($"[BossBase] {bossName} Idle 상태: 거리={distanceToPlayer:F1}, 감지범위={detectionRange}, stateTimer={stateTimer:F1}, target={target.name}");
            }
            
            // stateTimer가 일정 시간 이상일 때 패턴 발동 (너무 자주 발동 방지)
            if (distanceToPlayer <= detectionRange && stateTimer >= 2f)
            {
                Debug.Log($"[BossBase] {bossName} 패턴 발동 조건 충족! 패턴 선택 시작");
                // 랜덤하게 패턴 선택
                SelectRandomPattern();
            }
        }
        else
        {
            if (enableDebugPattern)
            {
                Debug.LogWarning($"[BossBase] {bossName} Idle 상태: target이 null입니다!");
            }
        }
    }
    
    /// <summary>
    /// 패턴 쿨다운 상태
    /// </summary>
    protected virtual void UpdatePatternCooldownState()
    {
        // 기본 추적만 수행
        base.UpdateBehavior();
        
        patternCooldownTimer -= Time.deltaTime;
        
        if (patternCooldownTimer <= 0f)
        {
            ChangeState(BossState.Idle);
        }
    }
    
    /// <summary>
    /// 가중치 기반 패턴 선택
    /// </summary>
    protected virtual void SelectRandomPattern()
    {
        int totalWeight = chargeWeight + projectileWeight + summonWeight;
        if (totalWeight <= 0)
        {
            Debug.LogWarning("[BossBase] 모든 패턴 가중치가 0 이하입니다. 기본 패턴을 사용합니다.");
            ChangeState(BossState.ChargePrepare);
            return;
        }
        
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        // 돌진 패턴 체크
        currentWeight += chargeWeight;
        if (randomValue < currentWeight)
        {
            ChangeState(BossState.ChargePrepare);
            if (enableDebugPattern) Debug.Log($"[BossBase] {bossName} 패턴 선택: 돌진 (가중치: {chargeWeight}/{totalWeight})");
            return;
        }
        
        // 투사체 패턴 체크
        currentWeight += projectileWeight;
        if (randomValue < currentWeight)
        {
            ChangeState(BossState.ShootingPrepare);
            if (enableDebugPattern) Debug.Log($"[BossBase] {bossName} 패턴 선택: 투사체 (가중치: {projectileWeight}/{totalWeight})");
            return;
        }
        
        // 소환 패턴
        ChangeState(BossState.SummonPrepare);
        if (enableDebugPattern) Debug.Log($"[BossBase] {bossName} 패턴 선택: 소환 (가중치: {summonWeight}/{totalWeight})");
    }
    
    /// <summary>
    /// 패턴 완료 후 쿨다운 시작
    /// </summary>
    protected virtual void StartPatternCooldown()
    {
        patternCooldownTimer = Random.Range(patternCooldownMin, patternCooldownMax);
        ChangeState(BossState.PatternCooldown);
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 패턴 쿨다운 시작: {patternCooldownTimer:F1}초");
        }
    }
    
    #endregion
    
    #region 돌진 패턴 구현
    
    /// <summary>
    /// 돌진 패턴 설정
    /// </summary>
    protected virtual void SetupChargePattern()
    {
        // 페이즈에 따른 돌진 횟수 설정
        if (currentPhase == 1)
        {
            maxChargeCount = phase1ChargeCount;
            enableWallBounce = phase1WallBounce;
        }
        else
        {
            maxChargeCount = Random.Range(phase2ChargeCountMin, phase2ChargeCountMax + 1);
            enableWallBounce = phase2WallBounce;
        }
        
        currentChargeCount = 0;
        
        // 돌진 예고 시각 효과 생성
        CreateChargeWarningIndicator();
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 돌진 패턴 설정: {maxChargeCount}회, 벽튕김 {enableWallBounce}");
        }
    }
    
    /// <summary>
    /// 돌진 준비 상태
    /// </summary>
    protected virtual void UpdateChargePrepareState()
    {
        Debug.Log($"[BossBase] {bossName} ChargePrepare 상태: stateTimer={stateTimer:F2}, chargePrepareTime={chargePrepareTime}");
        
        // 플레이어 방향으로 돌진 방향 설정 (2D)
        if (target != null)
        {
            Vector2 direction2D = ((Vector2)target.position - (Vector2)transform.position).normalized;
            chargeDirection = direction2D;
            chargeStartPosition = transform.position;
            
            // 시각 효과 업데이트
            UpdateChargeWarningIndicator();
            
            Debug.Log($"[BossBase] {bossName} 돌진 방향 설정: {chargeDirection}, 시각효과 업데이트 완료");
        }
        else
        {
            Debug.LogWarning($"[BossBase] {bossName} ChargePrepare 상태에서 target이 null!");
        }
        
        // 예고 시간 완료 시 돌진 시작
        if (stateTimer >= chargePrepareTime)
        {
            Debug.Log($"[BossBase] {bossName} 예고 시간 완료! 돌진 시작");
            // 예고 효과 제거하고 돌진 시작
            DestroyChargeWarningIndicator();
            ChangeState(BossState.Charging);
        }
    }
    
    /// <summary>
    /// 돌진 중 상태
    /// </summary>
    protected virtual void UpdateChargingState()
    {
        // 2D 탑다운 돌진 이동
        Vector2 movement = chargeDirection * chargeSpeed * Time.deltaTime;
        transform.position = (Vector2)transform.position + movement;
        
        // 시간 기반 돌진 종료
        if (stateTimer >= chargeDuration)
        {
            ChangeState(BossState.ChargeStunned);
        }
    }
    
    /// <summary>
    /// 돌진 후 경직 상태
    /// </summary>
    protected virtual void UpdateChargeStunnedState()
    {
        // 경직 시간 완료 시
        if (stateTimer >= chargeStunTime)
        {
            currentChargeCount++;
            
            if (currentChargeCount >= maxChargeCount)
            {
                // 돌진 패턴 완료
                StartPatternCooldown();
            }
            else
            {
                // 다음 돌진 준비
                ChangeState(BossState.ChargePrepare);
            }
        }
    }
    
    /// <summary>
    /// 벽 튕김 체크
    /// </summary>
    protected virtual void CheckWallBounce()
    {
        // TODO: 벽 충돌 감지 및 방향 반전 구현
        // LayerMask를 사용해 벽 감지 후 chargeDirection 반전
    }
    
    /// <summary>
    /// 벽 충돌 체크 (튕김 없음)
    /// </summary>
    protected virtual void CheckWallCollision()
    {
        // TODO: 벽 충돌 시 즉시 경직 상태로 전환
    }
    
    #endregion
    
    #region 투사체 패턴 구현
    
    /// <summary>
    /// 투사체 패턴 설정
    /// </summary>
    protected virtual void SetupShootingPattern()
    {
        // 페이즈에 따른 투사체 패턴 초기화
        currentBurstCount = 0;
        burstTimer = 0f;
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 투사체 패턴 설정 (페이즈 {currentPhase})");
        }
    }
    
    /// <summary>
    /// 투사체 발사 준비 상태
    /// </summary>
    protected virtual void UpdateShootingPrepareState()
    {
        // 즉시 발사 시작
        ChangeState(BossState.Shooting);
    }
    
    /// <summary>
    /// 투사체 발사 중 상태
    /// </summary>
    protected virtual void UpdateShootingState()
    {
        if (currentPhase == 1)
        {
            UpdatePhase1Shooting();
        }
        else
        {
            UpdatePhase2Shooting();
        }
    }
    
    /// <summary>
    /// 1페이즈 투사체 발사 (단발, 3연발)
    /// </summary>
    protected virtual void UpdatePhase1Shooting()
    {
        burstTimer += Time.deltaTime;
        
        // 단발 또는 3연발 랜덤 선택
        bool isBurst = Random.value > 0.5f;
        
        if (isBurst)
        {
            // 3연발
            if (burstTimer >= phase1BurstInterval && currentBurstCount < phase1BurstCount)
            {
                FireProjectileAtPlayer();
                currentBurstCount++;
                burstTimer = 0f;
            }
            
            if (currentBurstCount >= phase1BurstCount)
            {
                StartPatternCooldown();
            }
        }
        else
        {
            // 단발
            if (burstTimer >= phase1SingleShotDelay)
            {
                FireProjectileAtPlayer();
                StartPatternCooldown();
            }
        }
    }
    
    /// <summary>
    /// 2페이즈 투사체 발사 (부채꼴, 원형)
    /// </summary>
    protected virtual void UpdatePhase2Shooting()
    {
        burstTimer += Time.deltaTime;
        
        if (burstTimer >= 0.5f) // 준비 시간
        {
            // 부채꼴 또는 원형 랜덤 선택
            bool isFan = Random.value > 0.5f;
            
            if (isFan)
            {
                FireFanProjectiles();
            }
            else
            {
                FireCircleProjectiles();
            }
            
            StartPatternCooldown();
        }
    }
    
    /// <summary>
    /// 플레이어를 향해 투사체 발사
    /// </summary>
    protected virtual void FireProjectileAtPlayer()
    {
        if (projectilePrefab == null || target == null) return;
        
        Vector2 spawnPos = projectileSpawnPoint != null ? (Vector2)projectileSpawnPoint.position : (Vector2)transform.position;
        Vector2 direction = ((Vector2)target.position - spawnPos).normalized;
        
        CreateProjectile(spawnPos, direction);
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 플레이어 향해 투사체 발사");
        }
    }
    
    /// <summary>
    /// 부채꼴 투사체 발사
    /// </summary>
    protected virtual void FireFanProjectiles()
    {
        if (projectilePrefab == null || target == null) return;
        
        Vector2 spawnPos = projectileSpawnPoint != null ? (Vector2)projectileSpawnPoint.position : (Vector2)transform.position;
        Vector2 baseDirection = ((Vector2)target.position - spawnPos).normalized;
        
        // 2D 탑다운 부채꼴 계산
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        float angleStep = phase2FanAngle / (phase2FanProjectileCount - 1);
        float startAngle = baseAngle - (phase2FanAngle / 2f);
        
        for (int i = 0; i < phase2FanProjectileCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            float radAngle = currentAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radAngle), Mathf.Sin(radAngle));
            CreateProjectile(spawnPos, direction);
        }
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 부채꼴 투사체 발사: {phase2FanProjectileCount}개, 각도 {phase2FanAngle}°");
        }
    }
    
    /// <summary>
    /// 원형 투사체 발사
    /// </summary>
    protected virtual void FireCircleProjectiles()
    {
        if (projectilePrefab == null) return;
        
        Vector2 spawnPos = projectileSpawnPoint != null ? (Vector2)projectileSpawnPoint.position : (Vector2)transform.position;
        float angleStep = 360f / phase2CircleProjectileCount;
        
        for (int i = 0; i < phase2CircleProjectileCount; i++)
        {
            float angle = angleStep * i;
            float radAngle = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radAngle), Mathf.Sin(radAngle));
            CreateProjectile(spawnPos, direction);
        }
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 원형 투사체 발사: {phase2CircleProjectileCount}개");
        }
    }
    
    /// <summary>
    /// 투사체 생성 (2D)
    /// </summary>
    protected virtual void CreateProjectile(Vector2 position, Vector2 direction)
    {
        Vector2 direction2D = direction.normalized;
        
        // 2D 회전 계산 (스프라이트가 오른쪽 방향이 기본이라고 가정)
        float angle = Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        GameObject projectile = Instantiate(projectilePrefab, position, rotation);
        
        // 2D Rigidbody에 속도 적용
        Rigidbody2D rb2D = projectile.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = direction2D * projectileSpeed;
        }
        
        // 투사체에 데미지 설정
        BossProjectile projectileScript = projectile.GetComponent<BossProjectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDamage(projectileDamage);
        }
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] 투사체 생성: 2D방향 {direction2D}, 각도 {angle:F1}°, 데미지 {projectileDamage}");
        }
    }
    
    #endregion
    
    #region 하수인 소환 패턴 구현
    
    /// <summary>
    /// 소환 패턴 설정
    /// </summary>
    protected virtual void SetupSummonPattern()
    {
        summonCastTimer = 0f;
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 소환 패턴 설정 (페이즈 {currentPhase})");
        }
    }
    
    /// <summary>
    /// 소환 준비 상태
    /// </summary>
    protected virtual void UpdateSummonPrepareState()
    {
        // 즉시 소환 시작
        ChangeState(BossState.Summoning);
    }
    
    /// <summary>
    /// 소환 중 상태
    /// </summary>
    protected virtual void UpdateSummoningState()
    {
        summonCastTimer += Time.deltaTime;
        
        float castTime = currentPhase == 1 ? phase1SummonCastTime : phase2SummonCastTime;
        
        if (summonCastTimer >= castTime)
        {
            SpawnMinions();
            ChangeState(BossState.SummonComplete);
        }
    }
    
    /// <summary>
    /// 소환 완료 상태
    /// </summary>
    protected virtual void UpdateSummonCompleteState()
    {
        // 즉시 쿨다운 시작
        StartPatternCooldown();
    }
    
    /// <summary>
    /// 하수인 소환 실행
    /// </summary>
    protected virtual void SpawnMinions()
    {
        GameObject[] prefabs = currentPhase == 1 ? phase1MinionPrefabs : phase2MinionPrefabs;
        int count = currentPhase == 1 ? phase1MinionCount : phase2MinionCount;
        
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning($"[BossBase] {bossName} 페이즈 {currentPhase} 소환용 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] {bossName} 하수인 {count}마리 소환 완료");
        }
    }
    
    /// <summary>
    /// 랜덤 소환 위치 반환 (2D)
    /// </summary>
    protected virtual Vector3 GetRandomSpawnPosition()
    {
        if (spawnArea != null)
        {
            // spawnArea 범위 내에서 랜덤 위치 (2D)
            Collider2D collider2D = spawnArea.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Bounds bounds = collider2D.bounds;
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    transform.position.z
                );
                return randomPoint;
            }
        }
        
        // 보스 주변 원형 범위에서 랜덤 위치 (2D)
        Vector2 randomCircle = Random.insideUnitCircle * spawnAreaRadius;
        return (Vector2)transform.position + randomCircle;
    }
    
    #endregion
    
    #region 돌진 예고 시각 효과 시스템
    
    /// <summary>
    /// 돌진 예고 시각 효과 생성
    /// </summary>
    protected virtual void CreateChargeWarningIndicator()
    {
        Debug.Log($"[BossBase] {bossName} 돌진 예고 시각 효과 생성 시작");
        
        if (chargeWarningIndicator != null)
        {
            DestroyChargeWarningIndicator();
        }
        
        // 직사각형 GameObject 생성
        chargeWarningIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        chargeWarningIndicator.name = "ChargeWarningIndicator";
        
        Debug.Log($"[BossBase] {bossName} Quad GameObject 생성됨: {chargeWarningIndicator.name}");
        
        // Collider 제거 (시각 효과용이므로 충돌 불필요)
        Collider collider = chargeWarningIndicator.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
            Debug.Log($"[BossBase] {bossName} Collider 제거 완료");
        }
        
        // MeshRenderer와 MeshFilter 모두 제거
        MeshRenderer meshRenderer = chargeWarningIndicator.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            DestroyImmediate(meshRenderer);
            Debug.Log($"[BossBase] {bossName} MeshRenderer 제거 완료");
        }
        
        MeshFilter meshFilter = chargeWarningIndicator.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            DestroyImmediate(meshFilter);
            Debug.Log($"[BossBase] {bossName} MeshFilter 제거 완료");
        }
        
        chargeWarningRenderer = chargeWarningIndicator.AddComponent<SpriteRenderer>();
        Debug.Log($"[BossBase] {bossName} SpriteRenderer 추가 완료");
        
        // 텍스처 생성 (1x1 빨간색 텍스처)
        Texture2D redTexture = new Texture2D(1, 1);
        redTexture.SetPixel(0, 0, new Color(1f, 0f, 0f, 1f)); // 빨간색 100% 불투명
        redTexture.Apply();
        // pivot을 길이의 1/7 지점(0.143, 0.5)으로 설정하여 보스 뒤쪽에서 시작하도록
        chargeWarningRenderer.sprite = Sprite.Create(redTexture, new Rect(0, 0, 1, 1), new Vector2(0.143f, 0.5f));
        
        // 색상 설정 (추가 투명도 제어용)
        chargeWarningRenderer.color = new Color(1f, 0f, 0f, 0.5f);
        
        // 정렬 레이어와 순서 설정 (Ground 레이어보다 위에 표시)
        chargeWarningRenderer.sortingLayerName = "Default";
        chargeWarningRenderer.sortingOrder = -8;
        
        Debug.Log($"[BossBase] {bossName} 시각 효과 설정 완료: SortingLayer={chargeWarningRenderer.sortingLayerName}, Order={chargeWarningRenderer.sortingOrder}, Color={chargeWarningRenderer.color}");
    }
    
    /// <summary>
    /// 돌진 예고 시각 효과 업데이트
    /// </summary>
    protected virtual void UpdateChargeWarningIndicator()
    {
        if (chargeWarningIndicator == null || target == null) return;
        
        // 돌진 방향으로 직사각형 회전 및 위치 설정
        Vector2 direction2D = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg;
        
        // 직사각형 크기 설정 (가로 400배, 세로 150배)
        float warningWidth = 400f;
        float warningHeight = 150f;
        chargeWarningIndicator.transform.localScale = new Vector3(warningWidth, warningHeight, 1f);
        
        // pivot이 왼쪽 중앙이므로 보스 위치에 바로 배치 (offset 불필요)
        chargeWarningIndicator.transform.position = (Vector2)transform.position;
        chargeWarningIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Fill 효과 - 시간에 따라 투명도 증가
        float fillProgress = stateTimer / chargePrepareTime;
        fillProgress = Mathf.Clamp01(fillProgress);
        
        // 반투명(0.3) -> 불투명(0.9)으로 변화 (더 선명하게)
        float alpha = Mathf.Lerp(0.3f, 0.9f, fillProgress);
        Color currentColor = chargeWarningRenderer.color;
        currentColor.a = alpha;
        chargeWarningRenderer.color = currentColor;
        
        if (enableDebugPattern)
        {
            Debug.Log($"[BossBase] 돌진 예고 Fill 진행률: {fillProgress * 100:F1}%, Alpha: {alpha:F2}");
        }
    }
    
    /// <summary>
    /// 돌진 예고 시각 효과 제거
    /// </summary>
    protected virtual void DestroyChargeWarningIndicator()
    {
        if (chargeWarningIndicator != null)
        {
            // Texture 정리
            if (chargeWarningRenderer != null && chargeWarningRenderer.sprite != null && chargeWarningRenderer.sprite.texture != null)
            {
                DestroyImmediate(chargeWarningRenderer.sprite.texture);
            }
            
            DestroyImmediate(chargeWarningIndicator);
            chargeWarningIndicator = null;
            chargeWarningRenderer = null;
            
            if (enableDebugPattern)
            {
                Debug.Log($"[BossBase] {bossName} 돌진 예고 효과 제거");
            }
        }
    }
    
    #endregion
    
    #region 미구현 영역 (나중에 확장)
    
    /// <summary>
    /// [미구현] 보스 전용 대시 공격
    /// </summary>
    protected virtual void ExecuteDashAttack()
    {
        // TODO: 대시 공격 패턴 구현
        Debug.Log($"[BossBase] {bossName} 대시 공격 (미구현)");
    }
    
    /// <summary>
    /// [미구현] 보스 전용 투사체 공격
    /// </summary>
    protected virtual void ExecuteProjectileAttack()
    {
        // TODO: 투사체 공격 패턴 구현
        Debug.Log($"[BossBase] {bossName} 투사체 공격 (미구현)");
    }
    
    /// <summary>
    /// [미구현] 보스 전용 광역 공격
    /// </summary>
    protected virtual void ExecuteAreaAttack()
    {
        // TODO: 광역 공격 패턴 구현
        Debug.Log($"[BossBase] {bossName} 광역 공격 (미구현)");
    }
    
    /// <summary>
    /// [미구현] 보스 전용 특수 능력
    /// </summary>
    protected virtual void ExecuteSpecialAbility()
    {
        // TODO: 보스별 고유 특수 능력 구현
        Debug.Log($"[BossBase] {bossName} 특수 능력 (미구현)");
    }
    
    #endregion
}