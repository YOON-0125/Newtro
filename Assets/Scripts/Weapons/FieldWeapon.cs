using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필드 무기 (아이스 필드, 독 필드 등)
/// </summary>
public class FieldWeapon : WeaponBase
{
    [Header("필드 설정")]
    [SerializeField] private GameObject fieldPrefab;
    [SerializeField] private float fieldRadius = 5f;
    [SerializeField] private float fieldDuration = 3f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private int maxFields = 1;
    [SerializeField] private FieldPlacementType placementType = FieldPlacementType.NearPlayer;
    
    [Header("필드 효과")]
    [SerializeField] private FieldEffectType effectType = FieldEffectType.Damage;
    [SerializeField] private float slowEffect = 0.5f; // 슬로우 효과 (0~1)
    
    // 활성 필드 리스트
    private List<FieldInstance> activeFields = new List<FieldInstance>();
    
    public enum FieldPlacementType
    {
        NearPlayer,     // 플레이어 근처
        AtTarget,       // 타겟 위치
        RandomNear      // 근처 랜덤 위치
    }
    
    public enum FieldEffectType
    {
        Damage,         // 데미지만
        Slow,           // 슬로우만
        DamageAndSlow   // 데미지 + 슬로우
    }
    
    protected override void ExecuteAttack()
    {
        Vector3 fieldPosition = DetermineFieldPosition();
        CreateField(fieldPosition);
        OnAttackComplete();
    }
    
    /// <summary>
    /// 필드 위치 결정
    /// </summary>
    private Vector3 DetermineFieldPosition()
    {
        switch (placementType)
        {
            case FieldPlacementType.NearPlayer:
                return transform.position + (Vector3)Random.insideUnitCircle * 2f;
                
            case FieldPlacementType.AtTarget:
                Transform target = FindNearestTarget();
                return target != null ? target.position : transform.position;
                
            case FieldPlacementType.RandomNear:
                return transform.position + (Vector3)Random.insideUnitCircle * fieldRadius;
                
            default:
                return transform.position;
        }
    }
    
    /// <summary>
    /// 필드 생성
    /// </summary>
    private void CreateField(Vector3 position)
    {
        // 최대 필드 수 체크
        if (activeFields.Count >= maxFields)
        {
            // 가장 오래된 필드 제거
            RemoveOldestField();
        }
        
        GameObject fieldObject;
        
        if (fieldPrefab != null)
        {
            fieldObject = Instantiate(fieldPrefab, position, Quaternion.identity);
        }
        else
        {
            // 기본 필드 생성
            fieldObject = CreateDefaultField(position);
        }
        
        // 필드 인스턴스 설정
        FieldInstance fieldInstance = fieldObject.GetComponent<FieldInstance>();
        if (fieldInstance == null)
            fieldInstance = fieldObject.AddComponent<FieldInstance>();
            
          fieldInstance.Initialize(this, damage, fieldRadius, fieldDuration, tickInterval, effectType, slowEffect, damageTag, statusEffect);
        activeFields.Add(fieldInstance);
    }
    
    /// <summary>
    /// 기본 필드 생성 (프리팹이 없을 경우)
    /// </summary>
    private GameObject CreateDefaultField(Vector3 position)
    {
        GameObject field = new GameObject($"{weaponName}_Field");
        field.transform.position = position;
        
        // 원형 콜라이더 추가
        CircleCollider2D collider = field.AddComponent<CircleCollider2D>();
        collider.radius = fieldRadius;
        collider.isTrigger = true;
        
        // 시각적 효과를 위한 스프라이트 렌더러 (나중에 교체 가능)
        SpriteRenderer renderer = field.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = GetFieldColor();
        
        return field;
    }
    
    /// <summary>
    /// 원형 스프라이트 생성
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                Color color = Color.clear;
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius) * 0.5f;
                    color = new Color(1f, 1f, 1f, alpha);
                }
                
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 필드 색상 결정
    /// </summary>
    private Color GetFieldColor()
    {
        switch (effectType)
        {
            case FieldEffectType.Damage:
                return new Color(1f, 0.2f, 0.2f, 0.3f); // 빨간색 (화염)
            case FieldEffectType.Slow:
                return new Color(0.2f, 0.5f, 1f, 0.3f); // 파란색 (얼음)
            case FieldEffectType.DamageAndSlow:
                return new Color(0.8f, 0.2f, 0.8f, 0.3f); // 보라색
            default:
                return new Color(1f, 1f, 1f, 0.3f);
        }
    }
    
    /// <summary>
    /// 가장 오래된 필드 제거
    /// </summary>
    private void RemoveOldestField()
    {
        if (activeFields.Count > 0)
        {
            FieldInstance oldestField = activeFields[0];
            activeFields.RemoveAt(0);
            
            if (oldestField != null)
                oldestField.DestroyField();
        }
    }
    
    /// <summary>
    /// 활성 필드에서 제거
    /// </summary>
    public void RemoveField(FieldInstance field)
    {
        activeFields.Remove(field);
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // 레벨에 따른 개선
        switch (level)
        {
            case 2:
                fieldRadius *= 1.2f;
                break;
            case 3:
                maxFields = 2;
                break;
            case 4:
                fieldDuration *= 1.3f;
                break;
            case 5:
                tickInterval *= 0.8f;
                break;
            case 6:
                fieldRadius *= 1.3f;
                break;
            case 7:
                maxFields = 3;
                break;
            case 8:
                cooldown *= 0.7f;
                break;
            case 9:
                fieldDuration *= 1.5f;
                break;
            case 10:
                maxFields = 4;
                fieldRadius *= 1.5f;
                damage *= 1.5f;
                break;
        }
    }
    
    protected override float GetAttackRange()
    {
        return fieldRadius * 2f; // 필드 생성 범위
    }
    
    public override string GetWeaponInfo()
    {
        return base.GetWeaponInfo() + 
               $"\nField Radius: {fieldRadius:F1}" +
               $"\nField Duration: {fieldDuration:F1}s" +
               $"\nMax Fields: {maxFields}" +
               $"\nTick Interval: {tickInterval:F1}s";
    }
}

/// <summary>
/// 필드 인스턴스 클래스
/// </summary>
public class FieldInstance : MonoBehaviour
{
        private FieldWeapon parentWeapon;
        private float damage;
        private float radius;
        private float duration;
        private float tickInterval;
        private FieldWeapon.FieldEffectType effectType;
        private float slowEffect;
        private DamageTag damageTag;
        private StatusEffect statusEffect;
    
    private float startTime;
    private float lastTickTime;
    private HashSet<EnemyBase> affectedEnemies = new HashSet<EnemyBase>();
    
        public void Initialize(FieldWeapon parent, float damage, float radius, float duration,
                               float tickInterval, FieldWeapon.FieldEffectType effectType, float slowEffect,
                               DamageTag tag, StatusEffect statusEffect)
        {
            this.parentWeapon = parent;
            this.damage = damage;
            this.radius = radius;
            this.duration = duration;
            this.tickInterval = tickInterval;
            this.effectType = effectType;
            this.slowEffect = slowEffect;
            this.damageTag = tag;
            this.statusEffect = statusEffect;
        
        startTime = Time.time;
        lastTickTime = startTime;
        
        // 콜라이더 크기 설정
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
            collider.radius = radius;
    }
    
    private void Update()
    {
        // 지속 시간 체크
        if (Time.time >= startTime + duration)
        {
            DestroyField();
            return;
        }
        
        // 틱 데미지 처리
        if (Time.time >= lastTickTime + tickInterval)
        {
            ProcessTick();
            lastTickTime = Time.time;
        }
    }
    
    /// <summary>
    /// 틱 처리 (주기적 효과)
    /// </summary>
    private void ProcessTick()
    {
        foreach (var enemy in affectedEnemies)
        {
            if (enemy != null)
            {
                // 데미지 처리
                if (effectType == FieldWeapon.FieldEffectType.Damage || 
                    effectType == FieldWeapon.FieldEffectType.DamageAndSlow)
                {
                  enemy.TakeDamage(damage, damageTag);
                  var status = enemy.GetComponent<IStatusReceiver>();
                  status?.ApplyStatus(statusEffect);
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                affectedEnemies.Add(enemy);
                ApplyFieldEffect(enemy, true);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                affectedEnemies.Remove(enemy);
                ApplyFieldEffect(enemy, false);
            }
        }
    }
    
    /// <summary>
    /// 필드 효과 적용/해제
    /// </summary>
        private void ApplyFieldEffect(EnemyBase enemy, bool apply)
        {
          if (effectType == FieldWeapon.FieldEffectType.Slow ||
              effectType == FieldWeapon.FieldEffectType.DamageAndSlow)
          {
              var status = enemy.GetComponent<IStatusReceiver>();
              if (apply)
                  status?.ApplyStatus(statusEffect);
              else
                  status?.RemoveStatus(statusEffect.type);
          }
        }
    
    /// <summary>
    /// 필드 파괴
    /// </summary>
    public void DestroyField()
    {
        // 영향을 받던 적들의 효과 해제
        foreach (var enemy in affectedEnemies)
        {
            if (enemy != null)
                ApplyFieldEffect(enemy, false);
        }
        
        // 부모 무기에서 제거
        if (parentWeapon != null)
            parentWeapon.RemoveField(this);
            
        Destroy(gameObject);
    }
}