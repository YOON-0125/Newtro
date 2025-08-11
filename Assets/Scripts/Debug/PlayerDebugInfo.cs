using UnityEngine;

/// <summary>
/// 플레이어 오브젝트 디버그 정보 표시
/// </summary>
public class PlayerDebugInfo : MonoBehaviour
{
    private void Start()
    {
        AnalyzePlayerObjects();
    }
    
    private void AnalyzePlayerObjects()
    {
        Debug.Log("=== 플레이어 오브젝트 분석 시작 ===");
        
        // Player 태그로 찾기
        GameObject[] playerTagObjects = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log($"'Player' 태그를 가진 오브젝트 수: {playerTagObjects.Length}");
        
        for (int i = 0; i < playerTagObjects.Length; i++)
        {
            GameObject obj = playerTagObjects[i];
            Debug.Log($"Player 태그 오브젝트 {i + 1}: {obj.name}");
            AnalyzeGameObject(obj, "Player Tag");
        }
        
        // PlayerHealth 컴포넌트로 찾기
        PlayerHealth[] playerHealthObjects = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        Debug.Log($"PlayerHealth 컴포넌트를 가진 오브젝트 수: {playerHealthObjects.Length}");
        
        for (int i = 0; i < playerHealthObjects.Length; i++)
        {
            PlayerHealth health = playerHealthObjects[i];
            Debug.Log($"PlayerHealth 오브젝트 {i + 1}: {health.name}");
            AnalyzeGameObject(health.gameObject, "PlayerHealth Component");
        }
        
        // PlayerObj 컴포넌트로 찾기
        PlayerObj[] playerObjComponents = FindObjectsByType<PlayerObj>(FindObjectsSortMode.None);
        Debug.Log($"PlayerObj 컴포넌트를 가진 오브젝트 수: {playerObjComponents.Length}");
        
        for (int i = 0; i < playerObjComponents.Length; i++)
        {
            PlayerObj playerObj = playerObjComponents[i];
            Debug.Log($"PlayerObj 오브젝트 {i + 1}: {playerObj.name}");
            AnalyzeGameObject(playerObj.gameObject, "PlayerObj Component");
        }
        
        Debug.Log("=== 플레이어 오브젝트 분석 완료 ===");
    }
    
    private void AnalyzeGameObject(GameObject obj, string foundBy)
    {
        Debug.Log($"[{foundBy}] 오브젝트: {obj.name}");
        Debug.Log($"  - 위치: {obj.transform.position}");
        Debug.Log($"  - 태그: {obj.tag}");
        Debug.Log($"  - 레이어: {LayerMask.LayerToName(obj.layer)}");
        Debug.Log($"  - 활성화: {obj.activeInHierarchy}");
        
        // 콜라이더 확인
        Collider[] colliders = obj.GetComponents<Collider>();
        Collider2D[] colliders2D = obj.GetComponents<Collider2D>();
        
        Debug.Log($"  - 3D 콜라이더 수: {colliders.Length}");
        for (int i = 0; i < colliders.Length; i++)
        {
            Debug.Log($"    - {i + 1}: {colliders[i].GetType().Name}, isTrigger: {colliders[i].isTrigger}");
        }
        
        Debug.Log($"  - 2D 콜라이더 수: {colliders2D.Length}");
        for (int i = 0; i < colliders2D.Length; i++)
        {
            Debug.Log($"    - {i + 1}: {colliders2D[i].GetType().Name}, isTrigger: {colliders2D[i].isTrigger}");
        }
        
        // 주요 컴포넌트 확인
        PlayerHealth playerHealth = obj.GetComponent<PlayerHealth>();
        PlayerObj playerObj = obj.GetComponent<PlayerObj>();
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        Rigidbody2D rb2D = obj.GetComponent<Rigidbody2D>();
        
        Debug.Log($"  - PlayerHealth: {playerHealth != null}");
        Debug.Log($"  - PlayerObj: {playerObj != null}");
        Debug.Log($"  - Rigidbody: {rb != null}");
        Debug.Log($"  - Rigidbody2D: {rb2D != null}");
        
        Debug.Log($"  ==================");
    }
}