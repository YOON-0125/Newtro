using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상태효과 아이콘 컴포넌트
/// </summary>
public class StatusEffectIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackText; // 스택 수 표시 (x3, x5 등)
    [SerializeField] private TextMeshProUGUI durationText; // 남은 시간 표시 (3.2s, 1.5s 등)
    
    private StatusType statusType;
    private float remainingDuration;
    private float maxDuration;
    
    public void Initialize(StatusType type, float duration, int stacks = 1)
    {
        statusType = type;
        maxDuration = duration;
        remainingDuration = duration;
        
        // 상태효과 타입에 따른 아이콘 설정
        SetIconSprite(type);
        
        // 스택 수 표시
        UpdateStackDisplay(stacks);
    }
    
    public void UpdateDuration(float newDuration, int stacks = 1)
    {
        remainingDuration = newDuration;
        
        // 지속시간 텍스트 업데이트 (소수점 1자리까지)
        if (durationText != null)
        {
            durationText.text = $"{remainingDuration:F1}s";
        }
        
        // 스택 수 업데이트
        UpdateStackDisplay(stacks);
    }
    
    /// <summary>
    /// 스택 수 표시 업데이트
    /// </summary>
    private void UpdateStackDisplay(int stacks)
    {
        if (stackText != null)
        {
            if (stacks > 1)
            {
                stackText.text = $"x{stacks}";
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false); // 1스택이면 숨김
            }
        }
    }
    
    private void SetIconSprite(StatusType type)
    {
        if (iconImage == null) return;
        
        // TODO: StatusType에 따른 스프라이트 설정
        // 현재는 임시로 색상만 변경
        switch (type)
        {
            case StatusType.Fire:
                iconImage.color = Color.red;
                break;
            case StatusType.Ice:
                iconImage.color = Color.blue;
                break;
            case StatusType.Lightning:
                iconImage.color = Color.yellow;
                break;
            default:
                iconImage.color = Color.white;
                break;
        }
    }
}