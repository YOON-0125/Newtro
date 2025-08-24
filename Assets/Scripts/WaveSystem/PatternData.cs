using UnityEngine;

/// <summary>
/// 웨이브 패턴 타입 열거형
/// </summary>
public enum PatternType
{
    CircleSiege,    // 원형 포위 - BasicSkeleton
    ShieldWall,     // 방패 결계 - ShieldSkeleton
    MixedBarrier,   // 혼합 결계 - ShieldSkeleton(내부) + BasicSkeleton(외부)
    LineCharge      // 직선 돌격 - DualBladeSkeleton
}

/// <summary>
/// 웨이브 패턴 데이터
/// </summary>
[System.Serializable]
public class WavePatternData
{
    [Header("기본 설정")]
    public PatternType patternType;
    public string patternName;
    [TextArea(2, 4)]
    public string description;
    
    [Header("스폰 설정")]
    public int enemyCount = 10;
    public float spawnRadius = 12f;
    public float expMultiplier = 1.5f; // 패턴 전용 경험치 배율
    public int weight = 1; // 랜덤 선택 가중치
    
    [Header("직선 돌격 전용 설정")]
    [Range(0.1f, 2f)] public float chargeInterval = 0.2f; // 적들 사이 간격
    public float chargeSpeed = 8f; // 돌격 속도
    public float despawnDistance = 20f; // 플레이어 기준 소멸 거리
    
    [Header("혼합 결계 전용 설정")]
    public int innerEnemyCount = 6; // 내부 적 수량
    public int outerEnemyCount = 12; // 외부 적 수량
    public float innerRadius = 6f; // 내부 반지름
    public float outerRadius = 10f; // 외부 반지름
    public EnemyType innerEnemyType = EnemyType.ShieldSkeleton; // 내부 적 타입
    public EnemyType outerEnemyType = EnemyType.BasicSkeleton; // 외부 적 타입 (나중에 SkeletonMage로 변경)
}

/// <summary>
/// 패턴 확률 설정
/// </summary>
[System.Serializable]
public class PatternProbabilitySettings
{
    [Header("기본 확률 설정")]
    [Range(0f, 1f)] public float baseChance = 0.15f; // 기본 15% 확률
    [Range(0f, 0.1f)] public float waveBonus = 0.05f; // 웨이브당 추가 확률
    public int bonusStartWave = 5; // 보너스 시작 웨이브
    public int maxWave = 20; // 최대 확률 적용 웨이브
    
    [Header("패턴별 최소 웨이브")]
    public int circleSiegeMinWave = 1;
    public int shieldWallMinWave = 3;
    public int mixedBarrierMinWave = 5;
    public int lineChargeMinWave = 7;
}

/// <summary>
/// 적 타입 열거형 (패턴용)
/// </summary>
public enum EnemyType
{
    BasicSkeleton,
    ShieldSkeleton,
    DualBladeSkeleton,
    SkeletonMage // 추후 구현
}