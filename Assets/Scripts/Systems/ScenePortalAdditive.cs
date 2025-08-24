using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Additive 방식으로 보스 씬을 로드하는 포털
/// 플레이어, 시스템들을 그대로 유지하면서 보스 씬만 추가
/// </summary>
public class ScenePortalAdditive : MonoBehaviour
{
    [Header("씬 설정")]
    public string bossSceneName = "Boss1Scene";
    public Vector3 playerMovePosition = new Vector3(50, 30, 0);
    
    [Header("배경 처리")]
    public bool hideBackgroundLayer = true;    // Layer 0: Background
    public bool hideGroundLayer = true;        // Layer 1: Ground  
    public bool hideObjectLayer = false;       // Layer 2: Object
    public bool hideInnerWallsLayer = false;   // Layer 3: InnerWalls
    public bool hideWallsLayer = false;        // Layer 4: Walls
    public string backgroundTag = "Background";
    
    [Header("디버그")]
    public bool enableDebugLogs = true;
    
    private Vector3 originalPlayerPosition;
    private GameObject[] hiddenObjects;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            DebugLog("[ScenePortalAdditive] 플레이어가 보스 포털에 진입!");
            StartBossMode(other.gameObject);
        }
    }
    
    void StartBossMode(GameObject player)
    {
        DebugLog("[ScenePortalAdditive] 보스 모드 시작!");
        
        // 1. 원래 플레이어 위치 저장
        originalPlayerPosition = player.transform.position;
        DebugLog($"[ScenePortalAdditive] 원래 위치 저장: {originalPlayerPosition}");
        
        // 2. 플레이어를 보스 위치로 이동
        player.transform.position = playerMovePosition;
        DebugLog($"[ScenePortalAdditive] 플레이어 이동: {playerMovePosition}");
        
        // 3. 기존 배경 숨기기 (선택적)
        if (hideBackgroundLayer)
        {
            HideMainSceneObjects();
        }
        
        // 4. 보스 씬 추가 로드
        DebugLog($"[ScenePortalAdditive] 보스 씬 로드 시작: {bossSceneName}");
        SceneManager.LoadScene(bossSceneName, LoadSceneMode.Additive);
        
        // 5. 로드 완료 대기
        StartCoroutine(WaitForBossSceneLoad());
    }
    
    System.Collections.IEnumerator WaitForBossSceneLoad()
    {
        // 보스 씬이 로드될 때까지 대기
        yield return new WaitUntil(() => SceneManager.GetSceneByName(bossSceneName).isLoaded);
        
        DebugLog("[ScenePortalAdditive] 보스 씬 로드 완료!");
        
        // 보스 씬을 활성 씬으로 설정 (조명, 오디오 등)
        Scene bossScene = SceneManager.GetSceneByName(bossSceneName);
        SceneManager.SetActiveScene(bossScene);
        
        DebugLog($"[ScenePortalAdditive] 활성 씬 변경: {bossScene.name}");
        
        // 보스전 시작 알림
        NotifyBossStart();
    }
    
    void HideMainSceneObjects()
    {
        DebugLog("[ScenePortalAdditive] 메인 씬 오브젝트 숨기기 시작");
        
        System.Collections.Generic.List<GameObject> objectsToHide = new System.Collections.Generic.List<GameObject>();
        
        // 1. 배경 태그 오브젝트들
        if (hideBackgroundLayer)
        {
            GameObject[] backgrounds = GameObject.FindGameObjectsWithTag(backgroundTag);
            objectsToHide.AddRange(backgrounds);
            DebugLog($"[ScenePortalAdditive] Background 태그 오브젝트: {backgrounds.Length}개");
        }
        
        // 2. 타일맵 레이어들 선택적 숨김
        var tilemapRenderers = FindObjectsOfType<UnityEngine.Tilemaps.TilemapRenderer>();
        foreach (var renderer in tilemapRenderers)
        {
            string objName = renderer.gameObject.name;
            bool shouldHide = false;
            
            // Layer 0 Background
            if (hideBackgroundLayer && objName == "Layer 0 Background")
            {
                shouldHide = true;
                DebugLog($"[ScenePortalAdditive] Layer 0 Background 숨김");
            }
            
            // Layer 1 Ground
            else if (hideGroundLayer && objName == "Layer 1 Ground")
            {
                shouldHide = true;
                DebugLog($"[ScenePortalAdditive] Layer 1 Ground 숨김");
            }
            
            // Layer 2 Object
            else if (hideObjectLayer && objName == "Layer 2 Object")
            {
                shouldHide = true;
                DebugLog($"[ScenePortalAdditive] Layer 2 Object 숨김");
            }
            
            // Layer 3 InnerWalls
            else if (hideInnerWallsLayer && objName == "Layer 3 InnerWalls")
            {
                shouldHide = true;
                DebugLog($"[ScenePortalAdditive] Layer 3 InnerWalls 숨김");
            }
            
            // Layer 4 Walls
            else if (hideWallsLayer && objName == "Layer 4 Walls")
            {
                shouldHide = true;
                DebugLog($"[ScenePortalAdditive] Layer 4 Walls 숨김");
            }
            
            if (shouldHide)
            {
                objectsToHide.Add(renderer.gameObject);
            }
        }
        
        // 숨기기 실행
        hiddenObjects = objectsToHide.ToArray();
        foreach (GameObject obj in hiddenObjects)
        {
            obj.SetActive(false);
        }
        
        DebugLog($"[ScenePortalAdditive] 총 {hiddenObjects.Length}개 오브젝트 숨김");
    }
    
    void NotifyBossStart()
    {
        DebugLog("[ScenePortalAdditive] 🔥 보스전 시작! 🔥");
        
        // 여기서 보스전 시작 이벤트 등을 발생시킬 수 있음
        // 예: UI 표시, BGM 변경 등
    }
    
    /// <summary>
    /// 보스 클리어 후 메인 씬으로 복귀 (다른 스크립트에서 호출)
    /// </summary>
    public void ReturnToMainScene()
    {
        DebugLog("[ScenePortalAdditive] 메인 씬 복귀 시작");
        
        // 1. 보스 씬 언로드
        SceneManager.UnloadSceneAsync(bossSceneName);
        
        // 2. 메인 씬 활성화
        Scene mainScene = SceneManager.GetSceneByName("PlayScene"); // 또는 현재 메인 씬 이름
        if (mainScene.IsValid())
        {
            SceneManager.SetActiveScene(mainScene);
        }
        
        // 3. 숨겨진 오브젝트들 다시 표시
        ShowMainSceneObjects();
        
        // 4. 플레이어를 원래 위치로
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = originalPlayerPosition;
            DebugLog($"[ScenePortalAdditive] 플레이어 원위치 복귀: {originalPlayerPosition}");
        }
        
        DebugLog("[ScenePortalAdditive] 메인 씬 복귀 완료!");
    }
    
    void ShowMainSceneObjects()
    {
        // 숨겨진 오브젝트들 다시 표시
        if (hiddenObjects != null)
        {
            foreach (GameObject obj in hiddenObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    DebugLog($"[ScenePortalAdditive] 복구: {obj.name}");
                }
            }
        }
        
        // 타일맵들도 다시 표시
        var tilemapRenderers = FindObjectsOfType<UnityEngine.Tilemaps.TilemapRenderer>();
        foreach (var renderer in tilemapRenderers)
        {
            if (!renderer.gameObject.activeInHierarchy)
            {
                renderer.gameObject.SetActive(true);
                DebugLog($"[ScenePortalAdditive] 타일맵 복구: {renderer.gameObject.name}");
            }
        }
    }
    
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// 에디터에서 플레이어 이동 위치 시각화
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerMovePosition, 1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, playerMovePosition);
    }
}