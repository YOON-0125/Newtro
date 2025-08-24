using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 카메라가 따라갈 대상 (플레이어)
    public Transform target;

    // 카메라가 따라가는 속도 (값이 낮을수록 부드럽게 따라감)
    public float smoothSpeed = 0.125f;

    // 대상과의 거리 오프셋
    public Vector3 offset;

    private void Awake()
    {
        // 중복 카메라 방지
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            if (cam != GetComponent<Camera>() && cam.CompareTag("MainCamera"))
            {
                Debug.Log("[Camera] 중복 메인카메라 제거");
                Destroy(cam.gameObject);
            }
        }
        
        // Audio Listener 중복 해결
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            for (int i = 1; i < listeners.Length; i++)
            {
                Debug.Log("[Camera] 중복 Audio Listener 제거");
                Destroy(listeners[i]);
            }
        }
        
        // 씬 전환 시 카메라 유지
        DontDestroyOnLoad(gameObject);
        Debug.Log("[Camera] DontDestroyOnLoad 설정 완료");
    }
    
    // LateUpdate는 모든 Update 호출이 끝난 후에 호출됩니다.
    // 플레이어가 움직인 후 카메라가 따라가게 하므로, 카메라 움직임이 끊기는 현상을 방지합니다.
    void LateUpdate()
    {
        if (target != null)
        { 
            // 목표 위치 = 플레이어 위치 + 오프셋
            Vector3 desiredPosition = target.position + offset;
            
            // Lerp를 사용하여 현재 위치에서 목표 위치로 부드럽게 이동
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            
            // 카메라 위치 업데이트
            transform.position = smoothedPosition;
        }
    }
}
