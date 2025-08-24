using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 보스 방 진입을 감지하고 보스 전투를 시작하는 트리거
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossRoomTrigger : MonoBehaviour
{
    [Header("보스 방 설정")]
    [SerializeField] private BossType bossType = BossType.Fire;
    [SerializeField] private string roomName = "보스 방";
    [SerializeField] private Transform bossSpawnPoint;
    
    [Header("방 상태")]
    [SerializeField] private bool isCleared = false;
    [SerializeField] private bool allowReentry = false; // 클리어 후 재진입 허용
    [SerializeField] private bool debugMode = true;
    
    [Header("방 문 시스템 (선택사항)")]
    [SerializeField] private GameObject[] roomDoors; // 방 문들
    [SerializeField] private bool closeDoorOnEntry = true;
    [SerializeField] private bool openDoorOnClear = true;
    
    [Header("시각적 효과")]
    [SerializeField] private GameObject entryEffect; // 진입 시 이펙트
    [SerializeField] private GameObject clearEffect; // 클리어 시 이펙트
    [SerializeField] private Color roomGizmoColor = Color.red;

    [Header("텔레포트 비석 (선택사항)")]
    [SerializeField] private GameObject deactivatedTeleportStone; // 비활성화 상태의 비석 오브젝트
    [SerializeField] private GameObject activatedTeleportStonePrefab; // 활성화 상태의 비석 프리팹
    [SerializeField] private Transform teleportStoneSpawnPoint; // 비석 생성 위치
    
    // 이벤트
    [System.Serializable]
    public class BossRoomEvents
    {
        public UnityEvent OnPlayerEntered;
        public UnityEvent OnPlayerExited;
        public UnityEvent OnRoomCleared;
        public UnityEvent OnBossSpawned;
    }
    
    [Header("이벤트")]
    [SerializeField] private BossRoomEvents events;
    
    // 내부 상태
    private bool hasBeenTriggered = false;
    private bool playerInRoom = false;
    private Collider2D roomCollider;
    private BossManager bossManager;
    
    // 프로퍼티
    public BossType BossType => bossType;
    public Transform BossSpawnPoint => bossSpawnPoint;
    public bool IsCleared => isCleared;
    public bool PlayerInRoom => playerInRoom;
    public string RoomName => roomName;
    
    private void Awake()
    {
        roomCollider = GetComponent<Collider2D>();
        roomCollider.isTrigger = true;
        
        bossManager = BossManager.Instance;
        if (bossManager == null)
        {
            bossManager = FindObjectOfType<BossManager>();
        }
    }
    
    private void Start()
    {
        ValidateSetup();
        SetupRoom();
    }
    
    /// <summary>
    /// 플레이어 진입 감지
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
            
        if (debugMode)
        {
            Debug.Log($"[BossRoomTrigger] 플레이어가 {roomName}에 진입했습니다.");
        }
        
        playerInRoom = true;
        events?.OnPlayerEntered?.Invoke();
        
        // 이미 클리어된 방이고 재진입이 허용되지 않는 경우
        if (isCleared && !allowReentry)
        {
            if (debugMode)
            {
                Debug.Log($"[BossRoomTrigger] {roomName}은 이미 클리어되었습니다.");
            }
            return;
        }
        
        // 이미 트리거된 경우
        if (hasBeenTriggered && !allowReentry)
        {
            if (debugMode)
            {
                Debug.Log($"[BossRoomTrigger] {roomName}은 이미 활성화되었습니다.");
            }
            return;
        }
        
        // 다른 보스 전투 진행 중인 경우
        if (bossManager != null && bossManager.IsBossEncounterActive)
        {
            Debug.LogWarning($"[BossRoomTrigger] 다른 보스 전투가 진행 중입니다!");
            return;
        }
        
        // 보스 전투 시작
        StartBossEncounter();
    }
    
    /// <summary>
    /// 플레이어 퇴장 감지
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
            
        if (debugMode)
        {
            Debug.Log($"[BossRoomTrigger] 플레이어가 {roomName}에서 퇴장했습니다.");
        }
        
        playerInRoom = false;
        events?.OnPlayerExited?.Invoke();
        
        // 보스 전투 중 플레이어가 방을 나간 경우 (선택적 처리)
        if (bossManager != null && bossManager.IsBossEncounterActive && bossManager.CurrentBoss != null)
        {
            // 보스 전투 중단 또는 계속 진행 결정
            // 현재는 계속 진행하도록 설정 (필요시 수정)
            if (debugMode)
            {
                Debug.Log($"[BossRoomTrigger] 보스 전투 중 플레이어 퇴장. 전투는 계속 진행됩니다.");
            }
        }
    }
    
    /// <summary>
    /// 보스 전투 시작
    /// </summary>
    private void StartBossEncounter()
    {
        if (bossManager == null)
        {
            Debug.LogError($"[BossRoomTrigger] BossManager를 찾을 수 없습니다!");
            return;
        }
        
        hasBeenTriggered = true;
        
        if (debugMode)
        {
            Debug.Log($"[BossRoomTrigger] 보스 전투 시작: {bossType} in {roomName}");
        }
        
        // 방 문 닫기
        if (closeDoorOnEntry)
        {
            SetDoorState(false);
        }
        
        // 진입 이펙트
        if (entryEffect != null)
        {
            GameObject effect = Instantiate(entryEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // BossManager에 보스 전투 시작 요청
        bossManager.StartBossEncounter(bossType, this);
        
        events?.OnBossSpawned?.Invoke();
    }
    
    /// <summary>
    /// 방 클리어 처리 (BossManager에서 호출)
    /// </summary>
    public void MarkAsCleared()
    {
        if (isCleared)
            return;
            
        isCleared = true;
        
        if (debugMode)
        {
            Debug.Log($"[BossRoomTrigger] {roomName} 클리어 완료!");
        }
        
        // 방 문 열기
        if (openDoorOnClear)
        {
            SetDoorState(true);
        }
        
        // 클리어 이펙트
        if (clearEffect != null)
        {
            GameObject effect = Instantiate(clearEffect, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        events?.OnRoomCleared?.Invoke();
        
        // 클리어 후 재진입 허용하는 경우 트리거 리셋
        if (allowReentry)
        {
            hasBeenTriggered = false;
        }

        // 텔레포트 비석 활성화
        if (deactivatedTeleportStone != null)
        {
            deactivatedTeleportStone.SetActive(false);
        }
        if (activatedTeleportStonePrefab != null && teleportStoneSpawnPoint != null)
        {
            Instantiate(activatedTeleportStonePrefab, teleportStoneSpawnPoint.position, teleportStoneSpawnPoint.rotation);
            if(debugMode) Debug.Log($"[BossRoomTrigger] 텔레포트 비석 활성화: {roomName}");
        }
    }
    
    /// <summary>
    /// 방 문 상태 설정
    /// </summary>
    private void SetDoorState(bool open)
    {
        if (roomDoors == null || roomDoors.Length == 0)
            return;
            
        foreach (GameObject door in roomDoors)
        {
            if (door != null)
            {
                door.SetActive(!open); // 문이 열리면 비활성화, 닫히면 활성화
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[BossRoomTrigger] 방 문 상태 변경: {(open ? "열림" : "닫힘")}");
        }
    }
    
    /// <summary>
    /// 방 초기 설정
    /// </summary>
    private void SetupRoom()
    {
        // 스폰 포인트가 없으면 자동 생성
        if (bossSpawnPoint == null)
        {
            GameObject spawnPointObj = new GameObject("BossSpawnPoint");
            spawnPointObj.transform.SetParent(transform);
            spawnPointObj.transform.localPosition = Vector3.zero;
            bossSpawnPoint = spawnPointObj.transform;
            
            if (debugMode)
            {
                Debug.Log($"[BossRoomTrigger] {roomName}에 보스 스폰 포인트를 자동 생성했습니다.");
            }
        }
        
        // 초기 방 문 상태 설정 (클리어되지 않은 방은 문 열림)
        if (!isCleared)
        {
            SetDoorState(true);
        }
    }
    
    /// <summary>
    /// 설정 검증
    /// </summary>
    private void ValidateSetup()
    {
        if (bossSpawnPoint == null)
        {
            Debug.LogWarning($"[BossRoomTrigger] {roomName}: 보스 스폰 포인트가 설정되지 않았습니다!");
        }
        
        if (bossManager == null)
        {
            Debug.LogWarning($"[BossRoomTrigger] {roomName}: BossManager를 찾을 수 없습니다!");
        }
        
        if (roomCollider == null)
        {
            Debug.LogError($"[BossRoomTrigger] {roomName}: Collider2D가 필요합니다!");
        }
        else if (!roomCollider.isTrigger)
        {
            Debug.LogWarning($"[BossRoomTrigger] {roomName}: Collider2D가 Trigger로 설정되지 않았습니다!");
            roomCollider.isTrigger = true;
        }
    }
    
    /// <summary>
    /// 방 리셋 (개발/테스트용)
    /// </summary>
    [ContextMenu("Reset Room")]
    public void ResetRoom()
    {
        isCleared = false;
        hasBeenTriggered = false;
        playerInRoom = false;
        SetDoorState(true);
        
        if (debugMode)
        {
            Debug.Log($"[BossRoomTrigger] {roomName} 리셋 완료");
        }
    }
    
    /// <summary>
    /// 강제 클리어 (개발/테스트용)
    /// </summary>
    [ContextMenu("Force Clear")]
    public void ForceClear()
    {
        MarkAsCleared();
    }
    
    /// <summary>
    /// 방 정보 반환
    /// </summary>
    public string GetRoomInfo()
    {
        return $"보스 방 정보\n" +
               $"이름: {roomName}\n" +
               $"보스 타입: {bossType}\n" +
               $"클리어 상태: {(isCleared ? "완료" : "미완료")}\n" +
               $"플레이어 존재: {(playerInRoom ? "있음" : "없음")}\n" +
               $"트리거됨: {(hasBeenTriggered ? "예" : "아니오")}";
    }
    
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        // 방 영역 표시
        Gizmos.color = roomGizmoColor;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // 클리어 상태에 따른 색상 변경
        if (isCleared)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, transform.localScale * 0.9f);
        }
        
        // 보스 스폰 포인트 표시
        if (bossSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bossSpawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, bossSpawnPoint.position);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 선택 시 더 자세한 정보 표시
        Gizmos.color = Color.yellow;
        
        if (roomCollider != null)
        {
            // 트리거 영역 표시
            if (roomCollider is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (roomCollider is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
        }
    }
}