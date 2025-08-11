using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모든 무기의 기본이 되는 추상 클래스
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("무기 기본 정보")]
    [SerializeField] protected string weaponName;
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float cooldown = 1f;
    [SerializeField] protected float range = 10f;
    [SerializeField] protected int level = 1;
    [SerializeField] protected int maxLevel = 10;
    [SerializeField] protected DamageTag damageTag = DamageTag.Physical;
    [SerializeField] protected StatusEffect statusEffect;
    
    [Header("사운드")]
    [SerializeField] protected AudioClip attackSound;
    
    // 이벤트
    [System.Serializable]
    public class WeaponEvents
    {
        public UnityEvent OnAttack;
        public UnityEvent OnLevelUp;
        public UnityEvent OnMaxLevel;
    }
    
    [Header("이벤트")]
    [SerializeField] protected WeaponEvents events;
    
    // 내부 변수
    protected float lastAttackTime;
    protected bool isAttacking;
    protected AudioSource audioSource;
    
    // 프로퍼티
    public string WeaponName => weaponName;
    public float Damage => damage;
    public float Cooldown => cooldown;
    public float Range { get => range; set => range = value; }
    public int Level => level;
    public int MaxLevel => maxLevel;
    public bool CanAttack => Time.time >= lastAttackTime + cooldown;
    public bool IsMaxLevel => level >= maxLevel;
    
    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        audioSource.playOnAwake = false;
    }
    
    protected virtual void Start()
    {
        InitializeWeapon();
    }
    
    /// <summary>
    /// 무기 초기화
    /// </summary>
    protected virtual void InitializeWeapon()
    {
        // 각 무기별 초기화 로직
    }
    
    /// <summary>
    /// 공격 실행
    /// </summary>
    public virtual bool TryAttack()
    {
        if (!CanAttack || isAttacking)
            return false;

        var relicManager = FindObjectOfType<RelicManager>();
        if (relicManager != null)
            relicManager.OnBeforeWeaponFired(this);

        lastAttackTime = Time.time;
        isAttacking = true;
        
        // 사운드 재생
        PlayAttackSound();
        
        // 실제 공격 로직
        ExecuteAttack();
        
        // 이벤트 발생
        events?.OnAttack?.Invoke();
        
        return true;
    }

    /// <summary>
    /// 실제 공격 로직 (각 무기별로 구현)
    /// </summary>
    protected abstract void ExecuteAttack();
    
    /// <summary>
    /// 무기 레벨업
    /// </summary>
    public virtual bool LevelUp()
    {
        if (IsMaxLevel)
            return false;
            
        level++;
        OnLevelUp();
        
        events?.OnLevelUp?.Invoke();
        
        if (IsMaxLevel)
        {
            events?.OnMaxLevel?.Invoke();
        }
        
        return true;
    }
    
    /// <summary>
    /// 레벨업 시 처리 로직 (각 무기별로 구현)
    /// </summary>
    protected virtual void OnLevelUp()
    {
        // 기본적으로 데미지 10% 증가
        damage *= 1.1f;
    }

    /// <summary>
    /// 데미지 배수 적용
    /// </summary>
    public virtual void ApplyDamageMultiplier(float m)
    {
        damage *= m;
    }

    /// <summary>
    /// 쿨다운 배수 적용
    /// </summary>
    public virtual void ApplyCooldownMultiplier(float m)
    {
        cooldown *= m;
    }
    
    /// <summary>
    /// 공격 사운드 재생
    /// </summary>
    protected virtual void PlayAttackSound()
    {
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    
    /// <summary>
    /// 공격 완료 처리
    /// </summary>
    protected virtual void OnAttackComplete()
    {
        isAttacking = false;
    }
    
    /// <summary>
    /// 적에게 상태효과 적용
    /// </summary>
    protected virtual void ApplyStatusToTarget(GameObject target)
    {
        if (target == null) return;
        
        // StatusController 컴포넌트 확인
        var statusController = target.GetComponent<StatusController>();
        if (statusController != null)
        {
            // DamageTag에 따른 자동 상태효과 적용
            StatusEffect autoEffect = GetAutoStatusEffect();
            
            // 설정된 상태효과가 있으면 우선 적용
            if (statusEffect.type != default(StatusType) && statusEffect.duration > 0)
            {
                statusController.ApplyStatus(statusEffect);
                Debug.Log($"[{weaponName}] {target.name}에게 설정된 상태효과 적용: {statusEffect.type}");
            }
            // 자동 상태효과 적용
            else if (autoEffect.type != default(StatusType))
            {
                statusController.ApplyStatus(autoEffect);
                Debug.Log($"[{weaponName}] {target.name}에게 자동 상태효과 적용: {autoEffect.type} (DamageTag: {damageTag})");
            }
            else
            {
                Debug.Log($"[{weaponName}] {target.name}에게 적용할 상태효과가 없음 (DamageTag: {damageTag})");
            }
        }
        else
        {
            Debug.LogWarning($"[{weaponName}] {target.name}에 StatusController가 없습니다!");
        }
    }
    
    /// <summary>
    /// DamageTag에 따른 자동 상태효과 생성
    /// </summary>
    protected virtual StatusEffect GetAutoStatusEffect()
    {
        StatusEffect effect = new StatusEffect();
        
        switch (damageTag)
        {
            case DamageTag.Fire:
                effect.type = StatusType.Fire;
                effect.magnitude = damage * 0.2f; // 데미지의 20%
                effect.duration = 3f;
                effect.tickInterval = 0.5f;
                effect.stacks = 1;
                break;
                
            case DamageTag.Ice:
                effect.type = StatusType.Ice;
                effect.magnitude = 0.3f; // 30% 슬로우
                effect.duration = 2f;
                effect.stacks = 1;
                break;
                
            case DamageTag.Lightning:
                effect.type = StatusType.Lightning;
                effect.duration = 4f;
                effect.tickInterval = 1f;
                effect.stacks = 1;
                break;
        }
        
        return effect;
    }
    
    /// <summary>
    /// 무기 정보 텍스트 반환
    /// </summary>
    public virtual string GetWeaponInfo()
    {
        return $"{weaponName} Lv.{level}\nDamage: {damage:F1}\nCooldown: {cooldown:F1}s";
    }
    
    /// <summary>
    /// 타겟 찾기 (가장 가까운 적)
    /// </summary>
    protected virtual Transform FindNearestTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, GetAttackRange(), LayerMask.GetMask("Enemy"));
        
        if (enemies.Length == 0)
            return null;
            
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }
        
        return nearestEnemy;
    }
    
    /// <summary>
    /// 공격 범위 반환 (각 무기별로 재정의)
    /// </summary>
    protected virtual float GetAttackRange()
    {
        return range; // 기본 공격 범위
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetAttackRange());
    }
}