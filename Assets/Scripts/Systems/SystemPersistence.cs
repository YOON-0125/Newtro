using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 시스템 매니저들을 씬 전환 시에도 유지시키는 컴포넌트
/// SystemManagers GameObject에 추가하여 DontDestroyOnLoad 적용
/// </summary>
public class SystemPersistence : MonoBehaviour
{
    [Header("Persistence Settings")]
    [SerializeField] private bool enableDebugLog = false;
    
    private static SystemPersistence instance;
    
    void Awake()
    {
        // 이미 다른 SystemPersistence가 존재하는지 확인
        if (instance != null && instance != this)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SystemPersistence] 중복 인스턴스 감지됨. 제거: {gameObject.name}");
            }
            Destroy(gameObject);
            return;
        }
        
        // 첫 번째 인스턴스 등록
        instance = this;
        
        // 씬 전환 시에도 유지
        DontDestroyOnLoad(gameObject);
        
        if (enableDebugLog)
        {
            Debug.Log($"[SystemPersistence] 시스템 지속성 적용됨: {gameObject.name}");
            LogSystemComponents();
        }
    }
    
    void Start()
    {
        // 씬 변경 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SystemPersistence] 씬 로드됨: {scene.name}, 모드: {mode}");
            Debug.Log($"[SystemPersistence] 시스템들이 계속 유지됨: {gameObject.name}");
        }
    }
    
    private void LogSystemComponents()
    {
        Component[] components = GetComponents<Component>();
        Debug.Log($"[SystemPersistence] 유지되는 컴포넌트들:");
        foreach (Component comp in components)
        {
            if (comp != this && comp != transform)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }
        }
    }
    
    /// <summary>
    /// 현재 SystemPersistence 인스턴스 반환
    /// </summary>
    public static SystemPersistence Instance => instance;
    
    /// <summary>
    /// 시스템이 초기화되었는지 확인
    /// </summary>
    public static bool IsSystemReady => instance != null;
}