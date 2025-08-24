using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 정적 씬 전환 매니저 (파괴되지 않음)
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SceneTransitionManager] 싱글톤 생성 완료");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void TransitionToScene(string sceneName)
    {
        Debug.Log($"[SceneTransitionManager] 씬 전환 시작: {sceneName}");
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }
    
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneTransitionManager] 씬 로드 완료: {scene.name}");
        
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.SetupPlayerPositionDelayed());
        }
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private System.Collections.IEnumerator SetupPlayerPositionDelayed()
    {
        yield return null;
        
        // === 디버그 정보 수집 ===
        Debug.Log("=== 씬 전환 디버그 시작 ===");
        
        // 1. 모든 오브젝트 확인
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        Debug.Log($"[Debug] 씬의 총 오브젝트 수: {allObjects.Length}");
        
        // 2. Player 태그 오브젝트 확인
        GameObject[] playersWithTag = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log($"[Debug] Player 태그 오브젝트 수: {playersWithTag.Length}");
        
        // 3. DontDestroyOnLoad 씬 오브젝트들 확인
        int dontDestroyCount = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.scene.name == "DontDestroyOnLoad")
            {
                dontDestroyCount++;
                Debug.Log($"[Debug] DontDestroyOnLoad 오브젝트: {obj.name}, 태그: {obj.tag}, 활성화: {obj.activeInHierarchy}");
            }
        }
        Debug.Log($"[Debug] DontDestroyOnLoad 총 오브젝트 수: {dontDestroyCount}");
        
        // 4. 현재 활성 씬 확인
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"[Debug] 현재 활성 씬: {activeScene.name}");
        
        // 5. PlayerSpawn 확인
        GameObject[] playerSpawns = GameObject.FindGameObjectsWithTag("PlayerSpawn");
        Debug.Log($"[Debug] PlayerSpawn 태그 오브젝트 수: {playerSpawns.Length}");
        foreach (GameObject spawn in playerSpawns)
        {
            Debug.Log($"[Debug] PlayerSpawn 위치: {spawn.transform.position}, 씬: {spawn.scene.name}");
        }
        
        Debug.Log("=== 디버그 정보 수집 완료 ===");
        
        // === 실제 로직 시작 ===
        float timeout = 3f;
        float elapsed = 0f;
        
        GameObject player = null;
        GameObject spawnPoint = null;
        
        while (elapsed < timeout)
        {
            spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawn");
            player = GameObject.FindGameObjectWithTag("Player");
            
            Debug.Log($"[Debug] 시도 {elapsed:F1}초 - Player: {(player ? "발견" : "없음")}, Spawn: {(spawnPoint ? "발견" : "없음")}");
            
            if (player && spawnPoint)
            {
                Debug.Log($"[Debug] 플레이어 발견! 이름: {player.name}, 현재 위치: {player.transform.position}");
                Debug.Log($"[Debug] 스폰포인트 발견! 이름: {spawnPoint.name}, 위치: {spawnPoint.transform.position}");
                
                player.transform.position = spawnPoint.transform.position;
                Debug.Log($"[SceneTransitionManager] 플레이어 위치 설정 완료: {spawnPoint.transform.position}");
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // === 타임아웃 시 상세 분석 ===
        Debug.Log("=== 타임아웃 발생, 상세 분석 ===");
        if (!spawnPoint) 
        {
            Debug.LogWarning("[SceneTransitionManager] PlayerSpawn 태그를 가진 오브젝트를 찾을 수 없습니다!");
            Debug.Log("[Debug] 혹시 태그를 'PlayerSpawn'으로 정확히 설정했는지 확인하세요.");
        }
        if (!player) 
        {
            Debug.LogWarning("[SceneTransitionManager] Player 태그를 가진 오브젝트를 찾을 수 없습니다! (타임아웃)");
            Debug.Log("[Debug] DontDestroyOnLoad 플레이어가 사라졌거나 태그가 변경되었을 수 있습니다.");
        }
    }
}