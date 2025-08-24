using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모든 무기의 기본이 되는 추상 클래스
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("무기 기본 정보")]
    [SerializeField] protected string weaponName;
    [SerializeField] protected float baseDamage = 10f; // 기본 데미지 (Inspector 설정)
    [SerializeField] protected float cooldown = 1f;
    [SerializeField] protected float range = 10f;
    [SerializeField] protected int level = 1;
    [SerializeField] protected int maxLevel = 10;
    [SerializeField] protected DamageTag damageTag = DamageTag.Physical;
    [SerializeField] protected StatusEffect statusEffect;
    
    [Header("데미지 보너스 (런타임)")]
    [SerializeField] protected float flatDamageBonus = 0f; // 고정 데미지 보너스 (레벨업, 유물)
    [SerializeField] protected float percentDamageBonus = 0f; // 퍼센트 데미지 보너스 (전역 배율)
    
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
    public float BaseDamage => baseDamage;
    public float FlatDamageBonus => flatDamageBonus;
    public float PercentDamageBonus => percentDamageBonus;
    public float Damage => DamageCalculator.Calculate(baseDamage, flatDamageBonus, percentDamageBonus);
    public float Cooldown => cooldown;
    public float Range { get => range; set => range = value; }
    public int Level => level;
    public int MaxLevel => maxLevel;
    public bool CanAttack => Time.time >= lastAttackTime + cooldown;
    public bool IsMaxLevel => level >= maxLevel;
    public DamageTag DamageTag => damageTag;
    
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
        if (weaponName == "Fireball")
        {
            Debug.Log($"[WeaponBase] 🔥 Fireball TryAttack - CanAttack: {CanAttack}, isAttacking: {isAttacking}, Time: {Time.time}, lastAttackTime: {lastAttackTime}, cooldown: {cooldown}");
        }
        
        if (!CanAttack || isAttacking)
        {
            if (weaponName == "Fireball")
            {
                Debug.Log($"[WeaponBase] ❌ Fireball 공격 조건 실패 - CanAttack: {CanAttack}, isAttacking: {isAttacking}");
            }
            return false;
        }

        if (weaponName == "Fireball")
        {
            Debug.Log($"[WeaponBase] ✅ Fireball 공격 시작!");
        }

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
        // 무기별 레벨업 로직은 각 무기에서 구현
        // UpgradeSystem의 value 설정값만 사용
    }

    /// <summary>
    /// 고정 데미지 보너스 추가 (무기별 레벨업, 유물 등)
    /// </summary>
    public virtual void AddFlatDamageBonus(float amount)
    {
        flatDamageBonus += amount;
        Debug.Log($"[WeaponBase] {weaponName} 고정 데미지 보너스 추가: +{amount} (총 {flatDamageBonus})");
    }
    
    /// <summary>
    /// 퍼센트 데미지 보너스 추가 (전역 데미지 증가)
    /// </summary>
    public virtual void AddPercentDamageBonus(float percent)
    {
        percentDamageBonus += percent;
        Debug.Log($"[WeaponBase] {weaponName} 퍼센트 데미지 보너스 추가: +{percent:P1} (총 {percentDamageBonus:P1})");
    }
    
    /// <summary>
    /// 기존 호환성을 위한 데미지 배수 적용 (Deprecated)
    /// </summary>
    [System.Obsolete("Use AddPercentDamageBonus instead")]
    public virtual void ApplyDamageMultiplier(float multiplier)
    {
        // 기존 시스템 호환성: 1.2배 = +20% 보너스
        float percentBonus = multiplier - 1f;
        AddPercentDamageBonus(percentBonus);
        Debug.LogWarning($"[WeaponBase] {weaponName} ApplyDamageMultiplier is deprecated. Use AddPercentDamageBonus instead.");
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
        if (weaponName == "Fireball")
        {
            Debug.Log($"[WeaponBase] 🔥 Fireball 공격 완료! isAttacking 상태 해제");
        }
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
                effect.magnitude = Damage * 0.2f; // 데미지의 20%
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
        float finalDamage = Damage;
        return $"{weaponName} Lv.{level}\n" +
               $"데미지: {finalDamage:F1} ({baseDamage:F1}+{flatDamageBonus:F1}×{(1f+percentDamageBonus):F2})\n" +
               $"쿨다운: {cooldown:F1}s";
    }
    
    /// <summary>
    /// 상세 데미지 정보 반환 (디버그용)
    /// </summary>
    public virtual string GetDetailedDamageInfo()
    {
        return $"[{weaponName}] 기본: {baseDamage:F1}, 고정보너스: {flatDamageBonus:F1}, " +
               $"퍼센트보너스: {percentDamageBonus:P1}, 최종: {Damage:F1}";
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