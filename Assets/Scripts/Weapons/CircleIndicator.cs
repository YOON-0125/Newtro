using UnityEngine;
using System.Collections;

/// <summary>
/// LoL 스타일 원형 범위 인디케이터 - 반투명 원형 테두리 표시
/// </summary>
public class CircleIndicator : MonoBehaviour
{
    [Header("Circle Settings")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private Color indicatorColor = new Color(0f, 1f, 1f, 0.4f); // 반투명 청록색
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private int circleResolution = 64; // 원형의 부드러움
    
    [Header("Animation")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.2f;
    
    private LineRenderer circleRenderer;
    private Material indicatorMaterial;
    private float originalAlpha;
    private bool isActive = false;
    
    private void Awake()
    {
        CreateCircleRenderer();
        originalAlpha = indicatorColor.a;
    }
    
    /// <summary>
    /// LineRenderer로 원형 생성
    /// </summary>
    private void CreateCircleRenderer()
    {
        // LineRenderer 컴포넌트 추가
        circleRenderer = gameObject.AddComponent<LineRenderer>();
        
        // 기본 설정
        circleRenderer.positionCount = circleResolution + 1; // +1로 원형을 완전히 닫기
        circleRenderer.startWidth = lineWidth;
        circleRenderer.endWidth = lineWidth;
        circleRenderer.useWorldSpace = false;
        circleRenderer.loop = true;
        
        // 매테리얼 생성 및 설정
        CreateIndicatorMaterial();
        circleRenderer.material = indicatorMaterial;
        
        // 초기에는 비활성화
        circleRenderer.enabled = false;
        
        // 원형 포지션 계산 및 설정
        UpdateCirclePositions();
    }
    
    /// <summary>
    /// 반투명 매테리얼 생성
    /// </summary>
    private void CreateIndicatorMaterial()
    {
        // Unlit/Color 쉐이더를 사용한 반투명 매테리얼 생성
        indicatorMaterial = new Material(Shader.Find("Sprites/Default"));
        indicatorMaterial.color = indicatorColor;
        
        // 반투명 설정
        indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMaterial.SetInt("_ZWrite", 0);
        indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
        indicatorMaterial.DisableKeyword("_ALPHABLEND_ON");
        indicatorMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        indicatorMaterial.renderQueue = 3000;
    }
    
    /// <summary>
    /// 원형 포지션 업데이트
    /// </summary>
    private void UpdateCirclePositions()
    {
        if (circleRenderer == null) return;
        
        for (int i = 0; i <= circleResolution; i++)
        {
            float angle = i * Mathf.PI * 2f / circleResolution;
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            circleRenderer.SetPosition(i, pos);
        }
    }
    
    /// <summary>
    /// 인디케이터 표시
    /// </summary>
    /// <param name="newRadius">새로운 반지름</param>
    /// <param name="color">색상 (선택사항)</param>
    /// <param name="duration">표시 시간 (0이면 무한대)</param>
    public void ShowIndicator(float newRadius, Color? color = null, float duration = 0f)
    {
        radius = newRadius;
        
        if (color.HasValue)
        {
            indicatorColor = color.Value;
            originalAlpha = indicatorColor.a;
            if (indicatorMaterial != null)
                indicatorMaterial.color = indicatorColor;
        }
        
        UpdateCirclePositions();
        
        if (circleRenderer != null)
        {
            circleRenderer.enabled = true;
            isActive = true;
        }
        
        // 지속 시간이 설정된 경우 애니메이션과 함께 숨김
        if (duration > 0f)
        {
            StartCoroutine(ShowWithAnimation(duration));
        }
        
        Debug.Log($"[CircleIndicator] 인디케이터 표시: 반지름={radius:F1}, 색상={indicatorColor}");
    }
    
    /// <summary>
    /// 인디케이터 숨김
    /// </summary>
    public void HideIndicator()
    {
        if (circleRenderer != null)
        {
            circleRenderer.enabled = false;
            isActive = false;
        }
        
        // 알파값 초기화 (스케일은 건드리지 않음)
        if (indicatorMaterial != null)
        {
            Color resetColor = indicatorColor;
            resetColor.a = originalAlpha;
            indicatorMaterial.color = resetColor;
        }
        
        Debug.Log("[CircleIndicator] 인디케이터 숨김");
    }
    
    /// <summary>
    /// Scale + Fade 애니메이션과 함께 표시
    /// </summary>
    private IEnumerator ShowWithAnimation(float duration)
    {
        float scaleInTime = 0.1f; // Scale In 시간
        float fadeOutTime = duration - scaleInTime; // 나머지 시간은 Fade Out
        
        // 1단계: Scale In (0.1초)
        float elapsedTime = 0f;
        Vector3 originalScale = transform.localScale;
        Vector3 startScale = originalScale * 0.1f; // 10% 크기로 시작
        transform.localScale = startScale;
        
        while (elapsedTime < scaleInTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / scaleInTime;
            transform.localScale = Vector3.Lerp(startScale, originalScale, progress);
            yield return null;
        }
        transform.localScale = originalScale;
        
        // 2단계: Fade Out (나머지 시간)
        elapsedTime = 0f;
        while (elapsedTime < fadeOutTime && indicatorMaterial != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutTime;
            float currentAlpha = Mathf.Lerp(originalAlpha, 0f, progress);
            
            Color currentColor = indicatorColor;
            currentColor.a = currentAlpha;
            indicatorMaterial.color = currentColor;
            
            yield return null;
        }
        
        // 완전히 숨김
        HideIndicator();
    }
    
    /// <summary>
    /// 지속 시간 후 자동 숨김 (기존 방식 - 호환성 유지)
    /// </summary>
    private IEnumerator HideAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideIndicator();
    }
    
    /// <summary>
    /// 반지름 동적 변경
    /// </summary>
    public void UpdateRadius(float newRadius)
    {
        radius = newRadius;
        UpdateCirclePositions();
    }
    
    /// <summary>
    /// 색상 동적 변경
    /// </summary>
    public void UpdateColor(Color newColor)
    {
        indicatorColor = newColor;
        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = indicatorColor;
        }
    }
    
    /// <summary>
    /// 펄스 애니메이션 업데이트
    /// </summary>
    private void Update()
    {
        if (isActive && enablePulse && indicatorMaterial != null)
        {
            // 펄스 효과 계산
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            float currentAlpha = originalAlpha + pulse;
            currentAlpha = Mathf.Clamp01(currentAlpha);
            
            // 알파값 적용
            Color currentColor = indicatorColor;
            currentColor.a = currentAlpha;
            indicatorMaterial.color = currentColor;
        }
    }
    
    /// <summary>
    /// 펄스 효과 활성화/비활성화
    /// </summary>
    public void SetPulseEnabled(bool enabled)
    {
        enablePulse = enabled;
        if (!enabled && indicatorMaterial != null)
        {
            // 펄스 비활성화 시 원래 알파값으로 복원
            Color currentColor = indicatorColor;
            currentColor.a = originalAlpha;
            indicatorMaterial.color = currentColor;
        }
    }
    
    /// <summary>
    /// 인디케이터 활성 상태 확인
    /// </summary>
    public bool IsActive => isActive;
    
    /// <summary>
    /// 현재 반지름 반환
    /// </summary>
    public float CurrentRadius => radius;
    
    private void OnDestroy()
    {
        // 매테리얼 정리
        if (indicatorMaterial != null)
        {
            DestroyImmediate(indicatorMaterial);
        }
    }
    
    /// <summary>
    /// Gizmos로 에디터에서 범위 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}