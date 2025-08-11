using UnityEngine;

/// <summary>
/// 테스트용 ExpOrb 스포너
/// </summary>
public class ExpOrbTestSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject expOrbPrefab;
    [SerializeField] private KeyCode spawnKey = KeyCode.O;
    [SerializeField] private float spawnDistance = 5f;
    [SerializeField] private int expValue = 10;
    
    [Header("대체 생성 설정")]
    [SerializeField] private bool useBuiltinOrb = true; // ExpOrb 스크립트로 직접 생성
    
    private Transform playerTransform;
    
    private void Start()
    {
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"[ExpOrbTestSpawner] Player 찾음: {player.name}");
        }
        else
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerTransform = playerHealth.transform;
                Debug.Log($"[ExpOrbTestSpawner] PlayerHealth로 Player 찾음: {playerHealth.name}");
            }
            else
            {
                Debug.LogError("[ExpOrbTestSpawner] Player를 찾을 수 없습니다!");
            }
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnExpOrb();
        }
    }
    
    private void SpawnExpOrb()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[ExpOrbTestSpawner] Player가 없어서 ExpOrb를 생성할 수 없습니다!");
            return;
        }
        
        // 플레이어 주변 랜덤 위치에 스폰
        Vector2 randomOffset = Random.insideUnitCircle * spawnDistance;
        Vector3 spawnPosition = playerTransform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
        
        GameObject orbObj = null;
        
        if (expOrbPrefab != null && !useBuiltinOrb)
        {
            // 프리팹 사용
            orbObj = Instantiate(expOrbPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[ExpOrbTestSpawner] ExpOrb 프리팹 생성: {spawnPosition}");
        }
        else
        {
            // 직접 생성
            orbObj = new GameObject("ExpOrb_Test");
            orbObj.transform.position = spawnPosition;
            
            ExpOrb expOrbScript = orbObj.AddComponent<ExpOrb>();
            expOrbScript.SetExpValue(expValue);
            
            Debug.Log($"[ExpOrbTestSpawner] ExpOrb 직접 생성: {spawnPosition}");
        }
        
        // ExpOrb 컴포넌트 설정
        ExpOrb expOrb = orbObj.GetComponent<ExpOrb>();
        if (expOrb != null)
        {
            expOrb.SetExpValue(expValue);
            Debug.Log($"[ExpOrbTestSpawner] ExpOrb 설정 완료 - ExpValue: {expValue}");
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 150, 300, 100));
        GUILayout.Label("=== ExpOrb 테스트 ===");
        GUILayout.Label($"O키: ExpOrb 생성 (거리: {spawnDistance})");
        GUILayout.Label($"ExpValue: {expValue}");
        GUILayout.Label($"UseBuiltinOrb: {useBuiltinOrb}");
        GUILayout.EndArea();
    }
}