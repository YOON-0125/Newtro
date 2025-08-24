using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어 이동 시스템
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private bool smoothMovement = true;
    
    [Header("입력 설정")]
    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private bool enableTouchInput = true;
    [SerializeField] private float touchSensitivity = 2f;
    
    [Header("경계 설정")]
    [SerializeField] private bool limitToBounds = true;
    [SerializeField] private Vector2 boundsMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 boundsMax = new Vector2(10f, 10f);
    
    // 이벤트
    [System.Serializable]
    public class MovementEvents
    {
        public UnityEvent OnStartMoving;
        public UnityEvent OnStopMoving;
        public UnityEvent<Vector2> OnDirectionChanged;
    }
    
    [Header("이벤트")]
    [SerializeField] private MovementEvents events;
    
    // 컴포넌트
    private Rigidbody2D rb;
    private Animator animator;
    
    // 입력 관련
    private Vector2 inputVector;
    private Vector2 currentVelocity;
    private bool isMoving;
    private Vector2 lastDirection;
    
    // 터치 관련
    private Vector2 touchStartPos;
    private bool isTouching;
    
    // 프로퍼티
    public float MoveSpeed 
    { 
        get => moveSpeed; 
        set => moveSpeed = Mathf.Max(0f, value); 
    }
    
    public Vector2 MoveDirection => inputVector;
    public Vector2 Velocity => currentVelocity;
    public bool IsMoving => isMoving;
    public Vector2 LastDirection => lastDirection;
    
    private void Awake()
    {
        // DontDestroyOnLoad 플레이어 확인
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject dontDestroyPlayer = null;
        
        // DontDestroyOnLoad 플레이어가 있는지 확인
        foreach (GameObject player in players)
        {
            if (player.scene.name == "DontDestroyOnLoad")
            {
                dontDestroyPlayer = player;
                break;
            }
        }
        
        // 이미 DontDestroyOnLoad 플레이어가 있으면 새로운 것 제거
        if (dontDestroyPlayer != null && dontDestroyPlayer != gameObject)
        {
            Debug.Log("[Player] 씬에 있는 중복 플레이어 제거");
            Destroy(gameObject);
            return;
        }
        
        // 씬 전환 시 플레이어 유지
        DontDestroyOnLoad(gameObject);
        
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // Rigidbody2D 설정
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
        
        Debug.Log($"[Player] DontDestroyOnLoad 설정 완료 - 오브젝트: {gameObject.name}, 태그: {gameObject.tag}");
    }
    
    private void Update()
    {
        HandleInput();
        UpdateMovement();
        UpdateAnimation();
        CheckBounds();
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    private void HandleInput()
    {
        inputVector = Vector2.zero;
        
        // 키보드 입력
        if (enableKeyboardInput)
        {
            HandleKeyboardInput();
        }
        
        // 터치 입력
        if (enableTouchInput && Application.platform == RuntimePlatform.Android || 
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            HandleTouchInput();
        }
        
        // 입력 벡터 정규화
        if (inputVector.magnitude > 1f)
        {
            inputVector = inputVector.normalized;
        }
    }
    
    /// <summary>
    /// 키보드 입력 처리
    /// </summary>
    private void HandleKeyboardInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        inputVector = new Vector2(horizontal, vertical);
    }
    
    /// <summary>
    /// 터치 입력 처리
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touchPosition;
                    isTouching = true;
                    break;
                    
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isTouching)
                    {
                        Vector2 touchDelta = (touchPosition - touchStartPos) * touchSensitivity;
                        inputVector = Vector2.ClampMagnitude(touchDelta, 1f);
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isTouching = false;
                    inputVector = Vector2.zero;
                    break;
            }
        }
        else
        {
            isTouching = false;
        }
    }
    
    /// <summary>
    /// 이동 처리
    /// </summary>
    private void UpdateMovement()
    {
        Vector2 targetVelocity = inputVector * moveSpeed;
        
        if (smoothMovement)
        {
            // 부드러운 가속/감속
            float currentAcceleration = inputVector.magnitude > 0 ? acceleration : deceleration;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, 
                                                 currentAcceleration * Time.deltaTime);
        }
        else
        {
            // 즉시 속도 변경
            currentVelocity = targetVelocity;
        }
        
        // Rigidbody2D에 속도 적용
        rb.linearVelocity = currentVelocity;
        
        // 이동 상태 체크
        bool wasMoving = isMoving;
        isMoving = currentVelocity.magnitude > 0.1f;
        
        // 이동 상태 변화 이벤트
        if (isMoving && !wasMoving)
        {
            events?.OnStartMoving?.Invoke();
        }
        else if (!isMoving && wasMoving)
        {
            events?.OnStopMoving?.Invoke();
        }
        
        // 방향 변화 이벤트
        if (isMoving && Vector2.Angle(lastDirection, inputVector) > 1f)
        {
            lastDirection = inputVector;
            events?.OnDirectionChanged?.Invoke(inputVector);
        }
    }
    
    /// <summary>
    /// 애니메이션 업데이트
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // 애니메이터 파라미터 설정
        animator.SetFloat("Speed", currentVelocity.magnitude);
        animator.SetFloat("Horizontal", inputVector.x);
        animator.SetFloat("Vertical", inputVector.y);
        animator.SetBool("IsMoving", isMoving);
        
        // 마지막 방향 저장 (정지 시 방향 유지용)
        if (isMoving)
        {
            animator.SetFloat("LastHorizontal", inputVector.x);
            animator.SetFloat("LastVertical", inputVector.y);
        }
    }
    
    /// <summary>
    /// 경계 체크
    /// </summary>
    private void CheckBounds()
    {
        if (!limitToBounds) return;
        
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
        position.y = Mathf.Clamp(position.y, boundsMin.y, boundsMax.y);
        transform.position = position;
    }
    
    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }
    
    /// <summary>
    /// 일시적 속도 부스트
    /// </summary>
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }
    
    /// <summary>
    /// 속도 부스트 코루틴
    /// </summary>
    private System.Collections.IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        float originalSpeed = moveSpeed;
        moveSpeed *= multiplier;
        
        yield return new WaitForSeconds(duration);
        
        moveSpeed = originalSpeed;
    }
    
    /// <summary>
    /// 경계 설정
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
    }
    
    /// <summary>
    /// 특정 위치로 이동 (애니메이션)
    /// </summary>
    public void MoveToPosition(Vector2 targetPosition, float duration)
    {
        StartCoroutine(MoveToPositionCoroutine(targetPosition, duration));
    }
    
    /// <summary>
    /// 위치 이동 코루틴
    /// </summary>
    private System.Collections.IEnumerator MoveToPositionCoroutine(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = transform.position;
        float elapsedTime = 0f;
        
        // 입력 일시 비활성화
        bool wasKeyboardEnabled = enableKeyboardInput;
        bool wasTouchEnabled = enableTouchInput;
        enableKeyboardInput = false;
        enableTouchInput = false;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            Vector2 currentPosition = Vector2.Lerp(startPosition, targetPosition, t);
            transform.position = currentPosition;
            
            yield return null;
        }
        
        transform.position = targetPosition;
        
        // 입력 복원
        enableKeyboardInput = wasKeyboardEnabled;
        enableTouchInput = wasTouchEnabled;
    }
    
    /// <summary>
    /// 노크백 효과
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }
    
    /// <summary>
    /// 노크백 코루틴
    /// </summary>
    private System.Collections.IEnumerator KnockbackCoroutine(Vector2 direction, float force, float duration)
    {
        // 입력 일시 비활성화
        bool wasKeyboardEnabled = enableKeyboardInput;
        bool wasTouchEnabled = enableTouchInput;
        enableKeyboardInput = false;
        enableTouchInput = false;
        
        Vector2 knockbackVelocity = direction.normalized * force;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = 1f - (elapsedTime / duration);
            
            rb.linearVelocity = knockbackVelocity * t;
            
            yield return null;
        }
        
        rb.linearVelocity = Vector2.zero;
        
        // 입력 복원
        enableKeyboardInput = wasKeyboardEnabled;
        enableTouchInput = wasTouchEnabled;
    }
    
    /// <summary>
    /// 경계 시각화 (에디터용)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (limitToBounds)
        {
            Gizmos.color = Color.yellow;
            Vector2 center = (boundsMin + boundsMax) * 0.5f;
            Vector2 size = boundsMax - boundsMin;
            Gizmos.DrawWireCube(center, size);
        }
    }
}