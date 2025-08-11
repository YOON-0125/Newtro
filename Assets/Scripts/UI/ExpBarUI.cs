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
            GameManager.Instance.events.OnExperienceGained.AddListener(UpdateExpBar);

            // 게임 시작 시 초기 UI를 설정합니다.
            UpdateExpBar(GameManager.Instance.PlayerExperience);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.events.OnExperienceGained.RemoveListener(UpdateExpBar);
        }
    }

    /// <summary>
    /// 경험치 바를 업데이트하는 메서드
    /// </summary>
    /// <param name="currentExp">현재 경험치</param>
    private void UpdateExpBar(int currentExp)
    {
        if (expBarSlider != null && GameManager.Instance != null)
        {
            expBarSlider.maxValue = GameManager.Instance.ExpToNextLevel;
            expBarSlider.value = GameManager.Instance.PlayerExperience;
        }
    }
}

