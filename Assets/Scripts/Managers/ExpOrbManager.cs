using UnityEngine;

/// <summary>
/// EXP 오브 전역 설정 관리자
/// </summary>
public class ExpOrbManager : MonoBehaviour
{
    [Header("EXP 오브 설정")]
    [SerializeField] private GameObject expOrbPrefab;
    
    [Header("자석 효과 설정")]
    [SerializeField] private float globalMagnetRange = 2.5f;     // 전역 자석 범위 (조금 줄임)
    [SerializeField] private float globalMagnetStrength = 3f;    // 전역 자석 강도 (줄임)
    [SerializeField] private float globalMaxMoveSpeed = 8f;      // 전역 최대 이동 속도 (줄임)
    [SerializeField] private float globalAcceleration = 5f;      // 전역 가속도 (부드럽게)
    
    [Header("EXP 값 설정")]
    [SerializeField] private int defaultExpValue = 5;            // 기본 경험치 값
    
    // 싱글톤
    public static ExpOrbManager Instance { get; private set; }
    
    // 프로퍼티
    public GameObject ExpOrbPrefab => expOrbPrefab;
    public float GlobalMagnetRange => globalMagnetRange;
    public float GlobalMagnetStrength => globalMagnetStrength;
    public float GlobalMaxMoveSpeed => globalMaxMoveSpeed;
    public float GlobalAcceleration => globalAcceleration;
    public int DefaultExpValue => defaultExpValue;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요한 경우 주석 해제
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// EXP 오브 생성
    /// </summary>
    /// <param name="position">생성 위치</param>
    /// <param name="expValue">경험치 값 (0이면 기본값 사용)</param>
    /// <returns>생성된 EXP 오브 GameObject</returns>
    public GameObject CreateExpOrb(Vector3 position, int expValue = 0)
    {
        if (expOrbPrefab == null)
        {
            Debug.LogError("ExpOrbManager: ExpOrb 프리팹이 설정되지 않았습니다!");
            return null;
        }
        
        // EXP 오브 인스턴스화
        GameObject expOrb = Instantiate(expOrbPrefab, position, Quaternion.identity);
        ExpOrb expOrbScript = expOrb.GetComponent<ExpOrb>();
        
        if (expOrbScript != null)
        {
            // 경험치 값 설정
            int finalExpValue = expValue > 0 ? expValue : defaultExpValue;
            expOrbScript.SetExpValue(finalExpValue);
            
            // 전역 자석 설정 적용
            ApplyGlobalSettings(expOrbScript);
        }
        else
        {
            Debug.LogError("ExpOrbManager: 프리팹에 ExpOrb 스크립트가 없습니다!");
        }
        
        return expOrb;
    }
    
    /// <summary>
    /// 기존 EXP 오브에 전역 설정 적용
    /// </summary>
    /// <param name="expOrb">ExpOrb 스크립트</param>
    public void ApplyGlobalSettings(ExpOrb expOrb)
    {
        if (expOrb != null)
        {
            expOrb.SetMagnetRange(globalMagnetRange);
            expOrb.SetMagnetStrength(globalMagnetStrength);
            expOrb.SetMaxMoveSpeed(globalMaxMoveSpeed);
            expOrb.SetAcceleration(globalAcceleration);
        }
    }
    
    /// <summary>
    /// 전역 자석 범위 설정
    /// </summary>
    /// <param name="range">자석 범위</param>
    public void SetGlobalMagnetRange(float range)
    {
        globalMagnetRange = range;
        
        // 현재 활성화된 모든 EXP 오브에 적용
        ApplySettingsToAllActiveOrbs();
    }
    
    /// <summary>
    /// 전역 자석 강도 설정
    /// </summary>
    /// <param name="strength">자석 강도</param>
    public void SetGlobalMagnetStrength(float strength)
    {
        globalMagnetStrength = strength;
        ApplySettingsToAllActiveOrbs();
    }
    
    /// <summary>
    /// 전역 최대 이동 속도 설정
    /// </summary>
    /// <param name="speed">최대 이동 속도</param>
    public void SetGlobalMaxMoveSpeed(float speed)
    {
        globalMaxMoveSpeed = speed;
        ApplySettingsToAllActiveOrbs();
    }
    
    /// <summary>
    /// 전역 가속도 설정
    /// </summary>
    /// <param name="acceleration">가속도</param>
    public void SetGlobalAcceleration(float acceleration)
    {
        globalAcceleration = acceleration;
        ApplySettingsToAllActiveOrbs();
    }
    
    /// <summary>
    /// 현재 활성화된 모든 EXP 오브에 설정 적용
    /// </summary>
    private void ApplySettingsToAllActiveOrbs()
    {
        ExpOrb[] allOrbs = FindObjectsOfType<ExpOrb>();
        
        foreach (ExpOrb orb in allOrbs)
        {
            ApplyGlobalSettings(orb);
        }
    }
    
    /// <summary>
    /// 모든 EXP 오브 수집 (치트/디버그용)
    /// </summary>
    public void CollectAllExpOrbs()
    {
        ExpOrb[] allOrbs = FindObjectsOfType<ExpOrb>();
        
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            foreach (ExpOrb orb in allOrbs)
            {
                // 직접 경험치 지급
                gameManager.AddExperience(orb.GetComponent<ExpOrb>() != null ? defaultExpValue : defaultExpValue);
                Destroy(orb.gameObject);
            }
        }
    }
    
    /// <summary>
    /// EXP 오브 개수 반환
    /// </summary>
    /// <returns>현재 활성화된 EXP 오브 개수</returns>
    public int GetActiveExpOrbCount()
    {
        return FindObjectsOfType<ExpOrb>().Length;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 설정 변경 시 실시간 적용
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplySettingsToAllActiveOrbs();
        }
    }
#endif
}