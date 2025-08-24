using UnityEngine;

/// <summary>
/// 플레이어를 지정된 위치로 순간이동시키는 비석.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TeleportStone : MonoBehaviour
{
    [Header("텔레포트 설정")]
    [Tooltip("플레이어가 이동할 목표 위치")]
    [SerializeField] private Transform teleportDestination;

    [Tooltip("순간이동 시 재생할 파티클 효과 (선택사항)")]
    [SerializeField] private GameObject teleportEffectPrefab;

    [Tooltip("순간이동 쿨다운 (초)")]
    [SerializeField] private float teleportCooldown = 1f;

    private bool isReady = true;

    private void Awake()
    {
        // 이 스크립트가 있는 오브젝트의 콜라이더는 반드시 트리거여야 합니다.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 텔레포트가 준비되지 않았거나, 충돌한 것이 플레이어가 아니면 무시
        if (!isReady || !other.CompareTag("Player"))
        {
            return;
        }

        if (teleportDestination == null)
        {
            Debug.LogError("[TeleportStone] 목표 위치(teleportDestination)가 설정되지 않았습니다!", gameObject);
            return;
        }

        // 텔레포트 실행
        Teleport(other.gameObject);
    }

    private void Teleport(GameObject player)
    {
        Debug.Log($"[TeleportStone] {player.name}을(를) {teleportDestination.name}(으)로 순간이동 시킵니다.");

        // 플레이어 위치 변경
        player.transform.position = teleportDestination.position;

        // 순간이동 효과 재생
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, player.transform.position, Quaternion.identity);
        }

        // 쿨다운 시작
        isReady = false;
        Invoke(nameof(ResetCooldown), teleportCooldown);
    }

    private void ResetCooldown()
    {
        isReady = true;
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 목표 위치를 시각적으로 표시
        if (teleportDestination != null)
        {            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(teleportDestination.position, 1f);
            Gizmos.DrawLine(transform.position, teleportDestination.position);
        }
    }
}
