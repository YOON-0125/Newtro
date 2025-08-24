using UnityEngine;

/// <summary>
/// 보스가 발사하는 투사체
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f; // 투사체 수명 (초)
    [SerializeField] private bool destroyOnHit = true; // 충돌 시 파괴 여부
    
    [Header("이펙트")]
    [SerializeField] private GameObject hitEffect; // 충돌 시 이펙트
    [SerializeField] private GameObject trailEffect; // 꼬리 이펙트 (선택사항)
    
    private float timer = 0f;
    
    private void Start()
    {
        // 수명 타이머 시작
        timer = 0f;
        
        // 꼬리 이펙트 활성화
        if (trailEffect != null)
        {
            trailEffect.SetActive(true);
        }
    }
    
    private void Update()
    {
        // 수명 체크
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            DestroyProjectile();
        }
    }
    
    /// <summary>
    /// 충돌 감지 (플레이어와 충돌)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌
        if (other.CompareTag("Player"))
        {
            // 플레이어에게 데미지 적용
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, DamageTag.Physical);
            }
            
            Debug.Log($"[BossProjectile] 플레이어에게 {damage} 데미지!");
            
            // 충돌 시 파괴
            if (destroyOnHit)
            {
                CreateHitEffect();
                DestroyProjectile();
            }
        }
        
        // 벽과 충돌 (Wall 태그 또는 레이어)
        if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            CreateHitEffect();
            DestroyProjectile();
        }
    }
    
    /// <summary>
    /// 충돌 이펙트 생성
    /// </summary>
    private void CreateHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // 2초 후 이펙트 제거
        }
    }
    
    /// <summary>
    /// 투사체 파괴
    /// </summary>
    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 투사체 데미지 설정 (BossBase에서 호출)
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    /// <summary>
    /// 투사체 수명 설정
    /// </summary>
    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
    }
}