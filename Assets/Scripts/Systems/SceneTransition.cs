using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 처리하는 스크립트 (제단, 포털 등에 사용)
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("씬 전환 설정")]
    public string targetSceneName = "BossScene";
    public float transitionDelay = 0.5f;
    
    [Header("시각적 피드백")]
    public GameObject interactionPrompt; // "E키로 입장" UI (선택적)
    public ParticleSystem portalEffect; // 포털 이펙트 (선택적)
    
    private bool playerInRange = false;
    
    private void Awake()
    {
        // 씬 전환 시 SceneTransition 오브젝트도 유지
        DontDestroyOnLoad(gameObject);
        Debug.Log("[SceneTransition] DontDestroyOnLoad 설정 완료");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[SceneTransition] 플레이어가 포털에 진입!");
            
            // UI 표시
            if (interactionPrompt) interactionPrompt.SetActive(true);
            
            // 이펙트 재생
            if (portalEffect) portalEffect.Play();
            
            // 자동 전환 (딜레이 후)
            Invoke("TransitionToScene", transitionDelay);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[SceneTransition] 플레이어가 포털에서 나감");
            
            // UI 숨김
            if (interactionPrompt) interactionPrompt.SetActive(false);
            
            // 이펙트 중지
            if (portalEffect) portalEffect.Stop();
            
            // 전환 취소
            CancelInvoke("TransitionToScene");
        }
    }
    
    void TransitionToScene()
    {
        Debug.Log($"[SceneTransition] 씬 전환 시작: {targetSceneName}");
        
        // 씬 로드 완료 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 씬 전환
        SceneManager.LoadScene(targetSceneName);
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneTransition] 씬 로드 완료: {scene.name}");
        
        // DontDestroyOnLoad 오브젝트 복구를 위한 지연 처리
        StartCoroutine(SetupPlayerPositionDelayed());
        
        // 이벤트 해제 (메모리 누수 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private System.Collections.IEnumerator SetupPlayerPositionDelayed()
    {
        // 한 프레임 대기 (DontDestroyOnLoad 오브젝트 복구 대기)
        yield return null;
        
        // 최대 3초간 플레이어를 찾으려고 시도
        float timeout = 3f;
        float elapsed = 0f;
        
        GameObject player = null;
        GameObject spawnPoint = null;
        
        while (elapsed < timeout)
        {
            spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawn");
            player = GameObject.FindGameObjectWithTag("Player");
            
            if (player && spawnPoint)
            {
                player.transform.position = spawnPoint.transform.position;
                Debug.Log($"[SceneTransition] 플레이어 위치 설정 완료: {spawnPoint.transform.position}");
                yield break; // 성공적으로 완료
            }
            
            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }
        
        // 타임아웃 시 경고
        if (!spawnPoint) Debug.LogWarning("[SceneTransition] PlayerSpawn 태그를 가진 오브젝트를 찾을 수 없습니다!");
        if (!player) Debug.LogWarning("[SceneTransition] Player 태그를 가진 오브젝트를 찾을 수 없습니다! (타임아웃)");
    }
    
    /// <summary>
    /// 키 입력으로 전환하고 싶은 경우 사용
    /// </summary>
    void Update()
    {
        // E키로 수동 전환 (선택적 기능)
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            CancelInvoke("TransitionToScene");
            TransitionToScene();
        }
    }
}