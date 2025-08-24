using UnityEngine;

/// <summary>
/// 플레이어 중심으로 얼음 폭발을 일으켜 적을 느리게 함
/// </summary>
public class FrostNova : WeaponBase
{
    [Header("Frost Nova Settings")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private bool useMultiplier = true;
    [SerializeField] private float slowMultiplier = 0.7f;
    [SerializeField] private float slowFlat = 0.3f;
    [SerializeField] private float ticksPerSec = 0f;
    [SerializeField] private float fieldDuration = 2f;
    [SerializeField] private GameObject fieldPrefab;
    [Header("Status Effect")]
    [SerializeField] private float statusDuration = 2f;
    [SerializeField] private float statusTickInterval = 1f;
    [SerializeField] private int statusStacks = 1;
    
    [Header("Range Indicator")]
    [SerializeField] private bool showRangeIndicator = true;
    [SerializeField] private Color indicatorColor = new Color(0f, 1f, 1f, 0.25f); // 청록색

    protected override void InitializeWeapon()
    {
        base.InitializeWeapon();
        baseDamage = 5f;
        if (fieldPrefab == null)
        {
            fieldPrefab = new GameObject("FrostNovaField");
            fieldPrefab.AddComponent<FieldBase>();
            fieldPrefab.SetActive(false);
        }
        
    }
    

    protected override void ExecuteAttack()
    {
        Debug.Log($"[FrostNova] ❄️ ExecuteAttack 호출! Level: {Level}, Damage: {Damage}, Radius: {radius}");
        
        // 플레이어 위치에서 발동
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[FrostNova] 플레이어를 찾을 수 없습니다!");
            OnAttackComplete();
            return;
        }
        
        Vector3 castPosition = player.transform.position;
        Debug.Log($"[FrostNova] 시전 위치: {castPosition} (플레이어 중심)");
        
        // 범위 인디케이터 표시 (임시 오브젝트 생성)
        CreateRangeIndicator(castPosition);
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(castPosition, radius, LayerMask.GetMask("Enemy"));
        Debug.Log($"[FrostNova] 범위 내 적 탐지: {hits.Length}마리");
        
        var effect = new StatusEffect
        {
            type = StatusType.Ice,
            magnitude = useMultiplier ? slowMultiplier : slowFlat,
            duration = statusDuration,
            tickInterval = statusTickInterval,
            stacks = statusStacks
        };
        
        int hitCount = 0;
        foreach (var h in hits)
        {
            Debug.Log($"[FrostNova] 탐지된 오브젝트: {h.name} (태그: {h.tag})");
            
            var enemy = h.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                var sc = enemy.GetComponent<StatusController>();
                float finalDamage = Damage;
                if (sc != null)
                {
                    finalDamage *= sc.GetDamageTakenMultiplier(DamageTag.Ice);
                    sc.ApplyStatus(effect);
                    Debug.Log($"[FrostNova] {enemy.name}에게 슬로우 효과 적용");
                }
                enemy.TakeDamage(finalDamage);
                hitCount++;
                Debug.Log($"[FrostNova] {enemy.name}에게 {finalDamage} 얼음 데미지 적용");
            }
            else
            {
                Debug.LogWarning($"[FrostNova] {h.name}에 EnemyBase 컴포넌트가 없음");
            }
        }
        
        Debug.Log($"[FrostNova] 총 {hitCount}마리 적에게 피해 적용");

        if (ticksPerSec > 0f)
        {
            Debug.Log($"[FrostNova] 지속 필드 생성 - ticksPerSec: {ticksPerSec}");
            GameObject fieldObj = SimpleObjectPool.Instance != null ?
                SimpleObjectPool.Instance.Get(fieldPrefab, castPosition, Quaternion.identity) :
                Instantiate(fieldPrefab, castPosition, Quaternion.identity);
            var field = fieldObj.GetComponent<FieldBase>();
            if (field == null) field = fieldObj.AddComponent<FieldBase>();
            field.Setup(radius, fieldDuration, 1f / ticksPerSec, Damage);
            field.ConfigureEffect(DamageTag.Ice, effect);
        }

        OnAttackComplete();
        Debug.Log($"[FrostNova] ExecuteAttack 완료!");
    }
    
    /// <summary>
    /// 범위 인디케이터 임시 오브젝트 생성
    /// </summary>
    private void CreateRangeIndicator(Vector3 position)
    {
        if (!showRangeIndicator) return;
        
        // 임시 오브젝트 생성
        GameObject indicatorObj = new GameObject("FrostNova_RangeIndicator");
        indicatorObj.transform.position = position;
        
        // CircleIndicator 컴포넌트 추가 및 설정
        var indicator = indicatorObj.AddComponent<CircleIndicator>();
        indicator.ShowIndicator(radius, indicatorColor, 1f); // 1초간 표시
        
        // 1.1초 후 오브젝트 삭제 (여유시간 포함)
        Destroy(indicatorObj, 1.1f);
        
        Debug.Log($"[FrostNova] 임시 범위 인디케이터 생성: 위치={position}, 반지름={radius}");
    }
    

    public override string GetWeaponInfo()
    {
        return $"{weaponName} Lv.{Level}\nDamage: {Damage:F1}\nRadius: {radius:F1}\nSlow: {(useMultiplier ? slowMultiplier : slowFlat)}";
    }

    public void DebugFire() => TryAttack();

    protected override float GetAttackRange() => radius;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
