using UnityEngine;

/// <summary>
/// 테스트용 - 키보드 입력으로 경험치 획득
/// </summary>
public class ExpTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private int expPerPress = 10;
    [SerializeField] private KeyCode expKey = KeyCode.E;
    [SerializeField] private KeyCode bigExpKey = KeyCode.R;
    [SerializeField] private int bigExpAmount = 50;
    
    private void Update()
    {
        // E키로 일반 경험치 획득
        if (Input.GetKeyDown(expKey))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddExperience(expPerPress);
                Debug.Log($"테스트: 경험치 {expPerPress} 획득!");
            }
        }
        
        // R키로 큰 경험치 획득
        if (Input.GetKeyDown(bigExpKey))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddExperience(bigExpAmount);
                Debug.Log($"테스트: 경험치 {bigExpAmount} 획득!");
            }
        }
    }
    
    private void OnGUI()
    {
        // 화면에 안내 텍스트 표시
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label("=== EXP 테스트 ===");
        GUILayout.Label($"E키: 경험치 +{expPerPress}");
        GUILayout.Label($"R키: 경험치 +{bigExpAmount}");
        
        if (GameManager.Instance != null)
        {
            GUILayout.Label($"현재 레벨: {GameManager.Instance.PlayerLevel}");
            GUILayout.Label($"현재 경험치: {GameManager.Instance.PlayerExperience}/{GameManager.Instance.ExpToNextLevel}");
        }
        GUILayout.EndArea();
    }
}