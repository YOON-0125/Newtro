using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 데미지 숫자 텍스트 표시를 관리하는 싱글톤 클래스
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    [Header("데미지 텍스트 설정")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1.5f);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1f, 1, 0f);
    
    [Header("데미지 타입별 색상")]
    [SerializeField] private Color electricDamageColor = Color.yellow;
    [SerializeField] private Color fireDamageColor = Color.red;
    [SerializeField] private Color iceDamageColor = Color.cyan;
    [SerializeField] private Color physicalDamageColor = Color.white;
    
    [Header("Object Pool 설정")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private int maxPoolSize = 50;
    
    // Object Pool 관련
    private Queue<GameObject> damageTextPool = new Queue<GameObject>();
    private List<GameObject> activeDamageTexts = new List<GameObject>();
    
    private static DamageTextManager instance;
    public static DamageTextManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DamageTextManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DamageTextManager");
                    instance = go.AddComponent<DamageTextManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeDamageTextSystem();
    }
    
    /// <summary>
    /// 데미지 텍스트 시스템 초기화
    /// </summary>
    private void InitializeDamageTextSystem()
    {
        // UI 캔버스 찾기 (기존 UI 캔버스 사용)
        if (worldCanvas == null)
        {
            // 기존의 Screen Space Overlay 캔버스 찾기
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    worldCanvas = canvas;
                    Debug.Log("[DamageTextManager] 기존 ScreenSpaceOverlay 캔버스 사용");
                    break;
                }
            }
            
            // 캔버스가 없으면 새로 생성
            if (worldCanvas == null)
            {
                GameObject canvasGO = new GameObject("DamageTextCanvas");
                worldCanvas = canvasGO.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                worldCanvas.sortingOrder = 1000; // 다른 UI보다 위에 표시
                
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // GraphicRaycaster 추가
                canvasGO.AddComponent<GraphicRaycaster>();
                
                Debug.Log("[DamageTextManager] ScreenSpaceOverlay 캔버스 생성 완료");
            }
        }
        
        // 데미지 텍스트 프리팹이 없으면 기본 생성
        if (damageTextPrefab == null)
        {
            CreateDefaultDamageTextPrefab();
        }
        
        // Object Pool 초기화
        InitializeObjectPool();
    }
    
    /// <summary>
    /// 기본 데미지 텍스트 프리팹 생성
    /// </summary>
    private void CreateDefaultDamageTextPrefab()
    {
        damageTextPrefab = new GameObject("DamageTextPrefab");
        
        // RectTransform 먼저 설정
        RectTransform rect = damageTextPrefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 60); // 더 넉넉한 크기
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        Text textComponent = damageTextPrefab.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 32; // 더 큰 폰트
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;
        textComponent.text = "-99";
        
        // Outline 효과 추가 (가독성 향상)
        Outline outline = damageTextPrefab.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2); // 더 강한 아웃라인
        
        damageTextPrefab.SetActive(false);
        
        Debug.Log("[DamageTextManager] 기본 데미지 텍스트 프리팹 생성 완료 (크기: 150x60, 폰트: 32)");
    }
    
    /// <summary>
    /// 데미지 텍스트를 표시합니다.
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <param name="damage">데미지 값</param>
    /// <param name="damageType">데미지 타입</param>
    public void ShowDamageText(Vector3 worldPosition, float damage, DamageTextType damageType = DamageTextType.Physical)
    {
        if (damageTextPrefab == null || worldCanvas == null)
        {
            Debug.LogWarning("[DamageTextManager] 데미지 텍스트 시스템이 초기화되지 않았습니다.");
            InitializeDamageTextSystem(); // 재시도
            if (damageTextPrefab == null || worldCanvas == null)
            {
                Debug.LogError("[DamageTextManager] 초기화 실패!");
                return;
            }
        }
        
        // 카메라 확인
        if (Camera.main == null)
        {
            Debug.LogError("[DamageTextManager] Main Camera가 없습니다!");
            return;
        }
        
        // Object Pool에서 데미지 텍스트 가져오기
        GameObject textInstance = GetPooledDamageText();
        textInstance.SetActive(true);
        
        // 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
        // 스크린 좌표를 캔버스 로컬 좌표로 변환
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        
        // ScreenSpaceOverlay인 경우
        if (worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 스크린 좌표를 그대로 사용하되 캔버스 크기에 맞춰 조정
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null, // ScreenSpaceOverlay는 null
                out localPosition
            );
            
            // 적 위에 표시되도록 Y 오프셋 추가
            localPosition.y += 50f;
            
            textInstance.transform.localPosition = localPosition;
        }
        
        // 데미지 텍스트 설정
        Text textComponent = textInstance.GetComponent<Text>();
        textComponent.text = $"-{damage:F0}";
        textComponent.color = GetDamageColor(damageType);
        
        // 애니메이션 시작
        StartCoroutine(AnimateDamageText(textInstance));
        
        Debug.Log($"[DamageTextManager] 💥 데미지 텍스트 표시: -{damage:F0} at {worldPosition} -> screen {screenPosition} -> local {textInstance.transform.localPosition}");
    }
    
    /// <summary>
    /// 데미지 타입에 따른 색상 반환
    /// </summary>
    private Color GetDamageColor(DamageTextType damageType)
    {
        return damageType switch
        {
            DamageTextType.Electric => electricDamageColor,
            DamageTextType.Fire => fireDamageColor,
            DamageTextType.Ice => iceDamageColor,
            DamageTextType.Physical => physicalDamageColor,
            _ => physicalDamageColor
        };
    }
    
    /// <summary>
    /// 데미지 텍스트 애니메이션 코루틴 (UI 좌표계 기반)
    /// </summary>
    private IEnumerator AnimateDamageText(GameObject textInstance)
    {
        float elapsed = 0f;
        Vector3 startLocalPosition = textInstance.transform.localPosition;
        Vector3 endLocalPosition = startLocalPosition + Vector3.up * (moveDistance * 100f); // UI 좌표계는 픽셀 단위라 더 큰 값 필요
        Vector3 originalScale = textInstance.transform.localScale;
        
        Text textComponent = textInstance.GetComponent<Text>();
        Color originalColor = textComponent.color;
        
        // 텍스트를 즉시 완전히 불투명하게 표시 (Fade In 없음)
        textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        
        Debug.Log($"[DamageTextManager] 애니메이션 시작 (0.5초): {startLocalPosition} -> {endLocalPosition}");
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            // 위치 애니메이션 (위로 이동) - localPosition 사용
            textInstance.transform.localPosition = Vector3.Lerp(startLocalPosition, endLocalPosition, progress);
            
            // 스케일 애니메이션 (처음에는 원래 크기, 나중에 약간 커짐)
            float scaleMultiplier = Mathf.Lerp(1f, 1.2f, progress);
            textInstance.transform.localScale = originalScale * scaleMultiplier;
            
            // Fade Out만 적용 (1.0에서 0.0으로)
            float alpha = Mathf.Lerp(1f, 0f, progress);
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            yield return null;
        }
        
        Debug.Log($"[DamageTextManager] 애니메이션 완료 (0.5초), 오브젝트 풀로 반환");
        
        // 애니메이션 완료 후 Object Pool로 반환
        ReturnToPool(textInstance);
    }
    
    /// <summary>
    /// Object Pool 초기화
    /// </summary>
    private void InitializeObjectPool()
    {
        if (damageTextPrefab == null || worldCanvas == null)
        {
            Debug.LogError("[DamageTextManager] Object Pool 초기화 실패: 필수 컴포넌트 없음");
            return;
        }
        
        // 초기 풀 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject pooledText = Instantiate(damageTextPrefab, worldCanvas.transform);
            pooledText.SetActive(false);
            damageTextPool.Enqueue(pooledText);
        }
        
        Debug.Log($"[DamageTextManager] Object Pool 초기화 완료: {poolSize}개 오브젝트 생성");
    }
    
    /// <summary>
    /// 풀에서 데미지 텍스트 오브젝트 가져오기
    /// </summary>
    private GameObject GetPooledDamageText()
    {
        if (damageTextPool.Count > 0)
        {
            GameObject pooledText = damageTextPool.Dequeue();
            activeDamageTexts.Add(pooledText);
            return pooledText;
        }
        
        // 풀이 비어있고 최대 크기에 도달하지 않았으면 새로 생성
        if (activeDamageTexts.Count < maxPoolSize)
        {
            GameObject newText = Instantiate(damageTextPrefab, worldCanvas.transform);
            activeDamageTexts.Add(newText);
            Debug.Log("[DamageTextManager] 풀 확장: 새 오브젝트 생성");
            return newText;
        }
        
        Debug.LogWarning("[DamageTextManager] Object Pool 한계 도달! 가장 오래된 텍스트 재사용");
        // 가장 오래된 활성 텍스트 재사용
        return activeDamageTexts[0];
    }
    
    /// <summary>
    /// 데미지 텍스트를 풀로 반환
    /// </summary>
    public void ReturnToPool(GameObject damageText)
    {
        if (damageText == null) return;
        
        // 활성 리스트에서 제거
        activeDamageTexts.Remove(damageText);
        
        // 오브젝트 리셋
        damageText.SetActive(false);
        damageText.transform.localPosition = Vector3.zero;
        damageText.transform.localScale = Vector3.one;
        
        // 풀로 반환
        damageTextPool.Enqueue(damageText);
    }
    
    /// <summary>
    /// 전기 데미지 전용 편의 메서드
    /// </summary>
    public void ShowElectricDamage(Vector3 worldPosition, float damage)
    {
        ShowDamageText(worldPosition, damage, DamageTextType.Electric);
    }
}

/// <summary>
/// 데미지 텍스트 타입 열거형
/// </summary>
public enum DamageTextType
{
    Physical,
    Electric,
    Fire,
    Ice
}