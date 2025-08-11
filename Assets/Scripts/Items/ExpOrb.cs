using UnityEngine;

/// <summary>
/// 경험치 오브 - 적이 죽을 때 생성되어 플레이어가 획득
/// </summary>
public class ExpOrb : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private int expValue = 5;
    [SerializeField] private float magnetRange = 3f;      // 자석 효과 범위
    [SerializeField] private float magnetStrength = 5f;   // 자석 끌어당기는 힘
    [SerializeField] private float maxMoveSpeed = 10f;    // 최대 이동 속도
    [SerializeField] private float acceleration = 8f;     // 가속도
    [SerializeField] private float lifetime = 30f;        // 수명 (30초 후 자동 삭제)
    
    [Header("시각 효과")]
    [SerializeField] private float bobSpeed = 2f;         // 위아래 움직임 속도
    [SerializeField] private float bobHeight = 0.2f;      // 위아래 움직임 높이
    [SerializeField] private float rotateSpeed = 90f;     // 회전 속도
    
    // 내부 변수
    private Transform player;
    private Vector3 startPosition;
    private Vector3 currentVelocity = Vector3.zero;
    private bool isBeingCollected = false;
    private float spawnTime;
    
    private void Start()
    {
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("ExpOrb: Player를 찾을 수 없습니다!");
        }
        
        startPosition = transform.position;
        spawnTime = Time.time;
        
        // 시각적 설정
        SetupVisuals();
    }
    
    private void Update()
    {
        // 수명 체크
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // 자석 범위 안에 들어오면 플레이어쪽으로 이동
            if (distanceToPlayer <= magnetRange)
            {
                MoveTowardsPlayer();
            }
            else
            {
                // 제자리에서 둥둥 떠다니는 효과
                IdleBobbing();
            }
        }
        
        // 항상 회전
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// 시각적 설정 (개선된 EXP 오브)
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
        mat.color = new Color(0.2f, 1f, 0.3f, 1f); // 밝은 초록색
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.2f, 1f)); // 발광 효과
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Glossiness", 0.9f);
        renderer.material = mat;
        
        // 외곽 링 효과 (선택사항)
        CreateOuterRing(sphere.transform);
        
        // 콜라이더 제거 (부모에서 트리거로 처리)
        Destroy(sphere.GetComponent<Collider>());
        
        // 트리거 콜라이더 설정 (더 큰 범위)
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.8f;
        
        // Rigidbody 추가 (트리거 감지용)
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }
    
    /// <summary>
    /// 외곽 링 효과 생성
    /// </summary>
    private void CreateOuterRing(Transform parent)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(parent);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
        ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        // 반투명 초록색 링
        Renderer ringRenderer = ring.GetComponent<Renderer>();
        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = new Color(0.2f, 1f, 0.3f, 0.3f);
        ringMat.SetFloat("_Mode", 3); // Transparent mode
        ringMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ringMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ringMat.SetInt("_ZWrite", 0);
        ringMat.DisableKeyword("_ALPHATEST_ON");
        ringMat.EnableKeyword("_ALPHABLEND_ON");
        ringMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ringMat.renderQueue = 3000;
        ringRenderer.material = ringMat;
        
        // 콜라이더 제거
        Destroy(ring.GetComponent<Collider>());
    }
    
    /// <summary>
    /// 플레이어 쪽으로 부드럽게 이동 (개선된 자석 효과)
    /// </summary>
    private void MoveTowardsPlayer()
    {
        isBeingCollected = true;
        
        // 플레이어와의 거리와 방향 계산
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        if (distanceToPlayer > 0.2f) // 최소 거리 임계값
        {
            // 정규화된 방향 벡터
            Vector3 normalizedDirection = directionToPlayer.normalized;
            
            // 거리에 따른 자석 힘 계산 (역제곱 법칙 적용)
            float distanceRatio = Mathf.Clamp01(1f - (distanceToPlayer / magnetRange));
            float magnetForce = magnetStrength * distanceRatio * distanceRatio; // 제곱해서 더 강한 효과
            
            // 직접적인 속도 계산 (가속도 누적 방식 대신)
            float targetSpeed = Mathf.Lerp(2f, maxMoveSpeed, distanceRatio);
            Vector3 targetVelocity = normalizedDirection * targetSpeed;
            
            // 부드러운 속도 보간 (급작스러운 변화 방지)
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
            
            // 위치 직접 업데이트
            transform.position += currentVelocity * Time.deltaTime;
        }
        else
        {
            // 매우 가까우면 직접 플레이어 위치로 이동
            transform.position = Vector3.MoveTowards(transform.position, player.position, maxMoveSpeed * 2f * Time.deltaTime);
        }
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
    /// 플레이어와 충돌 시 경험치 지급
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Player 태그와 PlayerHealth 컴포넌트 둘 다 확인
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
        {
            // 중복 수집 방지
            if (isBeingCollected && gameObject != null)
            {
                GameManager gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    gameManager.AddExperience(expValue);
                    Debug.Log($"ExpOrb: 경험치 {expValue} 획득!");
                    StartCoroutine(CollectAnimationAndDestroy());
                }
            }
        }
    }
    
    /// <summary>
    /// 2D 충돌 지원 (혹시 2D 콜라이더 사용하는 경우)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
        {
            if (isBeingCollected && gameObject != null)
            {
                GameManager gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    gameManager.AddExperience(expValue);
                    Debug.Log($"ExpOrb: 경험치 {expValue} 획득! (2D)");
                    StartCoroutine(CollectAnimationAndDestroy());
                }
            }
        }
    }
    /// <summary>
    /// 획득 효과 재생 후 오브젝트 제거
    /// </summary>
    private System.Collections.IEnumerator CollectAnimationAndDestroy()
    {
        // 애니메이션이 재생되는 동안은 isBeingCollected 플래그를 false로 설정하여 중복 처리 방지
        isBeingCollected = false;

        // 간단한 스케일 키우기 효과 (기존 코드와 동일)
        float duration = 0.2f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.5f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 애니메이션이 끝난 후 오브젝트 파괴
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 경험치 값 설정 (외부에서 호출)
    /// </summary>
    public void SetExpValue(int value)
    {
        expValue = value;
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
    public void SetMagnetStrength(float strength)
    {
        magnetStrength = strength;
    }
    
    /// <summary>
    /// 최대 이동 속도 설정
    /// </summary>
    public void SetMaxMoveSpeed(float speed)
    {
        maxMoveSpeed = speed;
    }
    
    /// <summary>
    /// 가속도 설정
    /// </summary>
    public void SetAcceleration(float accel)
    {
        acceleration = accel;
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