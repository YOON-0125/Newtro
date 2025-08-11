using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameManager의 이벤트에 반응하여 경험치 UI를 업데이트하는 스크립트
/// </summary>
public class ExpBarUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Slider expBarSlider; // 인스펙터에서 UI Slider를 연결

    private void Start()
    {
        // GameManager 인스턴스에 접근
        if (GameManager.Instance != null)
        {
            // 경험치 획득 이벤트에 업데이트 메서드를 등록합니다.
            GameManager.Instance.Events.OnExperienceGained.AddListener(UpdateExpBar);

            // 게임 시작 시 초기 UI를 설정합니다.
            UpdateExpBar(0); // 매개변수는 실제로 사용되지 않음
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Events.OnExperienceGained.RemoveListener(UpdateExpBar);
        }
    }

    /// <summary>
    /// 경험치 바를 업데이트하는 메서드
    /// </summary>
    /// <param name="gainedExp">획득한 경험치 (사용하지 않음)</param>
    private void UpdateExpBar(int gainedExp)
    {
        if (expBarSlider != null && GameManager.Instance != null)
        {
            // 현재 경험치와 필요 경험치를 가져와서 비율 계산
            float currentExp = GameManager.Instance.PlayerExperience;
            float expRequired = GameManager.Instance.ExpToNextLevel;
            float expRatio = currentExp / expRequired;
            
            // 슬라이더 값 업데이트 (0~1 비율)
            expBarSlider.value = expRatio;
            
            // 디버그 로그
            Debug.Log($"EXP UI Updated: {currentExp}/{expRequired} ({expRatio:P0})");
        }
    }
}

