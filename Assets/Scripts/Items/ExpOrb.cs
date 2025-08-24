using UnityEngine;

/// <summary>
/// 경험치 오브 - 적이 죽을 때 생성되어 플레이어가 획득
/// </summary>
public class ExpOrb : MonoBehaviour
{
    [Header("설정")]
    public int experienceValue = 5;
    public float magnetRange = 3f;
    public float magnetSpeed = 8f;
    [SerializeField] private float lifetime = 30f;
    [SerializeField] private LayerMask playerLayer = 1; // Player 레이어만
    
    [Header("시각 효과")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private bool useCustomVisuals = true; // Inspector에서 체크하면 SetupVisuals 스킵
    
    // 내부 변수
    private Vector3 startPosition;
    private bool isBeingCollected = false;
    private float spawnTime;
    
    private void Start()
    {
        startPosition = transform.position;
        spawnTime = Time.time;
        
        // 디버그 로그 추가
        // Debug.Log($"[ExpOrb] useCustomVisuals = {useCustomVisuals}");
        
        // 시각적 설정 (프리팹을 사용하지 않는 경우에만)
        if (!useCustomVisuals)
        {
            // Debug.Log("[ExpOrb] 기본 구체 생성");
            SetupVisuals();
        }
        else
        {
            // Debug.Log("[ExpOrb] 커스텀 비주얼 사용 - SetupVisuals 스킵");
        }
        
        // 프리팹 사용 시에도 콜라이더는 확인
        EnsureColliderSetup();
    }
    
    private void Update()
    {
        // 수명 체크
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        // GameManager와 플레이어 확인
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[ExpOrb] GameManager.Instance가 null입니다!");
            IdleBobbing();
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
            return;
        }
        
        if (GameManager.Instance.Player == null) 
        {
            Debug.LogWarning("[ExpOrb] GameManager.Instance.Player가 null입니다!");
            IdleBobbing();
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
            return;
        }
        
        Vector3 playerPos = GameManager.Instance.Player.position;
        float distance = Vector3.Distance(transform.position, playerPos);
        
        // 플레이어가 가까이 오면 자석 효과로 끌어당김
        if (distance <= magnetRange)
        {
            isBeingCollected = true;
            Vector3 direction = (playerPos - transform.position).normalized;
            transform.position += direction * magnetSpeed * Time.deltaTime;
            
            // 플레이어와 충돌하면 수집
            if (distance <= 0.5f)
            {
                CollectExperience();
            }
        }
        else
        {
            // 제자리에서 둥둥 떠다니는 효과
            IdleBobbing();
        }
        
        // 항상 회전
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// 시각적 설정 (기본 구체만)
    /// </summary>
    private void SetupVisuals()
    {
        // 메인 구체 생성
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.4f;
        
        // 발광하는 초록색 머티리얼
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 1f, 0.3f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.2f, 1f));
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Glossiness", 0.9f);
        renderer.material = mat;
        
        // 자식 오브젝트 콜라이더 제거
        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            Destroy(sphereCollider);
        }
    }
    
    /// <summary>
    /// 콜라이더 설정 확인 (프리팹용)
    /// </summary>
    private void EnsureColliderSetup()
    {
        // 이미 콜라이더가 있는지 확인
        CircleCollider2D existingCollider = GetComponent<CircleCollider2D>();
        if (existingCollider == null)
        {
            // 콜라이더가 없으면 추가
            CircleCollider2D trigger = gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
        }
        else
        {
            // 기존 콜라이더가 있으면 트리거로 설정
            existingCollider.isTrigger = true;
        }
        
        // Rigidbody2D 확인
        Rigidbody2D existingRb = GetComponent<Rigidbody2D>();
        if (existingRb == null)
        {
            Rigidbody2D rb2D = gameObject.AddComponent<Rigidbody2D>();
            rb2D.gravityScale = 0f;
            rb2D.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            existingRb.gravityScale = 0f;
            existingRb.bodyType = RigidbodyType2D.Kinematic;
        }
    }
    
    /// <summary>
    /// 경험치 수집 처리
    /// </summary>
    private void CollectExperience()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddExperience(experienceValue);
            Debug.Log($"[ExpOrb] ✅ 경험치 {experienceValue} 획득!");
        }
        
        // 오브젝트 파괴
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 제자리에서 위아래로 둥둥
    /// </summary>
    private void IdleBobbing()
    {
        if (!isBeingCollected)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 newPos = startPosition;
            newPos.y += bobOffset;
            transform.position = newPos;
        }
    }
    
    /// <summary>
    /// 2D 충돌 처리
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log($"[ExpOrb] OnTriggerEnter2D - 충돌한 오브젝트: {other.name}, 태그: {other.tag}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("[ExpOrb] 플레이어와 충돌! 경험치 수집 시작");
            CollectExperience();
        }
        else
        {
            // Debug.Log($"[ExpOrb] 플레이어가 아닌 오브젝트와 충돌: {other.name}");
        }
    }
    
    /// <summary>
    /// 경험치 값 설정 (외부에서 호출)
    /// </summary>
    public void SetExpValue(int value)
    {
        experienceValue = value;
    }
    
    /// <summary>
    /// 자석 범위 설정
    /// </summary>
    public void SetMagnetRange(float range)
    {
        magnetRange = range;
    }
    
    /// <summary>
    /// 자석 강도 설정
    /// </summary>
    public void SetMagnetSpeed(float speed)
    {
        magnetSpeed = speed;
    }
    
    /// <summary>
    /// 자석 강도 설정 (ExpOrbManager 호환용)
    /// </summary>
    public void SetMagnetStrength(float strength)
    {
        magnetSpeed = strength;
    }
    
    /// <summary>
    /// 최대 이동 속도 설정 (ExpOrbManager 호환용)
    /// </summary>
    public void SetMaxMoveSpeed(float speed)
    {
        magnetSpeed = speed;
    }
    
    /// <summary>
    /// 가속도 설정 (ExpOrbManager 호환용)
    /// </summary>
    public void SetAcceleration(float acceleration)
    {
        // 현재 구조에서는 사용하지 않지만 호환성을 위해 유지
    }

#if UNITY_EDITOR
    /// <summary>
    /// Scene 뷰에서 자석 범위 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
#endif
}