using UnityEngine;

/// <summary>
/// 간단한 씬 포털 트리거 (파괴되어도 상관없음)
/// </summary>
public class ScenePortal : MonoBehaviour
{
    [Header("포털 설정")]
    public string targetSceneName = "Boss1Scene";
    public float transitionDelay = 0.5f;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[ScenePortal] 플레이어가 포털에 진입!");
            Invoke("TriggerTransition", transitionDelay);
        }
    }
    
    private void TriggerTransition()
    {
        // 정적 매니저를 통해 씬 전환
        SceneTransitionManager.TransitionToScene(targetSceneName);
    }
}