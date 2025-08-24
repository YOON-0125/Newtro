using UnityEngine;

/// <summary>
/// 골드 시스템 - 영구 골드 저장 및 관리
/// </summary>
public class GoldSystem : MonoBehaviour
{
    [Header("골드 설정")]
    [SerializeField] private int currentGold = 0;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 싱글톤
    public static GoldSystem Instance { get; private set; }
    
    // 이벤트
    public System.Action<int> OnGoldChanged;
    
    // 프로퍼티
    public int CurrentGold => currentGold;
    
    // PlayerPrefs 키
    private const string GOLD_SAVE_KEY = "PersistentGold";
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGold();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 골드 로드 (게임 시작 시)
    /// </summary>
    private void LoadGold()
    {
        currentGold = PlayerPrefs.GetInt(GOLD_SAVE_KEY, 0);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldSystem] 💰 골드 로드됨: {currentGold}");
        }
        
        // 골드 변경 이벤트 발생
        OnGoldChanged?.Invoke(currentGold);
    }
    
    /// <summary>
    /// 골드 저장
    /// </summary>
    private void SaveGold()
    {
        PlayerPrefs.SetInt(GOLD_SAVE_KEY, currentGold);
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldSystem] 💾 골드 저장됨: {currentGold}");
        }
    }
    
    /// <summary>
    /// 골드 추가
    /// </summary>
    /// <param name="amount">추가할 골드량</param>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        int oldGold = currentGold;
        currentGold += amount;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldSystem] ➕ 골드 추가: +{amount} ({oldGold} → {currentGold})");
        }
        
        SaveGold();
        OnGoldChanged?.Invoke(currentGold);
    }
    
    /// <summary>
    /// 골드 사용 (차감)
    /// </summary>
    /// <param name="amount">사용할 골드량</param>
    /// <returns>사용 성공 여부</returns>
    public bool SpendGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[GoldSystem] 사용할 골드량이 0 이하입니다.");
            return false;
        }
        
        if (currentGold < amount)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GoldSystem] ❌ 골드 부족: 필요 {amount}, 보유 {currentGold}");
            }
            return false;
        }
        
        int oldGold = currentGold;
        currentGold -= amount;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldSystem] ➖ 골드 사용: -{amount} ({oldGold} → {currentGold})");
        }
        
        SaveGold();
        OnGoldChanged?.Invoke(currentGold);
        
        return true;
    }
    
    /// <summary>
    /// 골드 사용 가능 여부 확인
    /// </summary>
    /// <param name="amount">확인할 골드량</param>
    /// <returns>사용 가능 여부</returns>
    public bool CanAfford(int amount)
    {
        return currentGold >= amount;
    }
    
    
    
    /// <summary>
    /// 골드 설정 (디버그용)
    /// </summary>
    /// <param name="amount">설정할 골드량</param>
    public void SetGold(int amount)
    {
        if (amount < 0) amount = 0;
        
        int oldGold = currentGold;
        currentGold = amount;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldSystem] 🔧 골드 설정: {oldGold} → {currentGold}");
        }
        
        SaveGold();
        OnGoldChanged?.Invoke(currentGold);
    }
    
    /// <summary>
    /// 골드 데이터 리셋 (디버그용)
    /// </summary>
    public void ResetGold()
    {
        currentGold = 0;
        PlayerPrefs.DeleteKey(GOLD_SAVE_KEY);
        PlayerPrefs.Save();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GoldSystem] 🔄 골드 리셋됨");
        }
        
        OnGoldChanged?.Invoke(currentGold);
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGold();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveGold();
        }
    }
    
    private void OnDestroy()
    {
        SaveGold();
    }
}