using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범위형 필드 기본 클래스
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class FieldBase : MonoBehaviour
{
    [SerializeField] protected float radius = 1f;
    [SerializeField] protected float duration = 3f;
    [SerializeField] protected float tickInterval = 1f;
    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float slowMultiplier = 1f;
    [SerializeField] protected LayerMask targetLayer = 0;
    [SerializeField] protected DamageTag damageTag = DamageTag.Physical;
    [SerializeField] protected StatusEffect statusEffect;

    protected readonly HashSet<EnemyBase> targets = new HashSet<EnemyBase>();
    private float lifeTimer;
    private float tickTimer;
    private CircleCollider2D circle;

    protected virtual void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = radius;
    }

    public virtual void Setup(float radius, float duration, float tickInterval, float damage, float slowMultiplier = 1f)
    {
        this.radius = radius;
        this.duration = duration;
        this.tickInterval = tickInterval;
        this.damage = damage;
        this.slowMultiplier = slowMultiplier;
        if (circle != null) circle.radius = radius;
    }

    public void ConfigureEffect(DamageTag tag, StatusEffect effect)
    {
        damageTag = tag;
        statusEffect = effect;
    }

    protected virtual void OnEnable()
    {
        lifeTimer = 0f;
        tickTimer = 0f;
        targets.Clear();
    }

    protected virtual void Update()
    {
        lifeTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;
        if (lifeTimer >= duration)
        {
            DestroyField();
            return;
        }

        if (tickInterval > 0f && tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            Tick();
        }
    }

    protected virtual void Tick()
    {
        foreach (var enemy in targets)
        {
            if (enemy != null)
            {
                ApplyTick(enemy);
            }
        }
    }

    protected virtual void ApplyTick(EnemyBase enemy)
    {
        ApplyDamage(enemy, damage);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                ApplyDamage(enemy, damage);
                if (slowMultiplier != 1f)
                    enemy.ApplySlow(slowMultiplier);
                targets.Add(enemy);
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                if (slowMultiplier != 1f)
                    enemy.ApplySlow(1f);
                targets.Remove(enemy);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        foreach (var enemy in targets)
        {
            if (enemy != null && slowMultiplier != 1f)
                enemy.ApplySlow(1f);
        }
        targets.Clear();
    }

    protected virtual void DestroyField()
    {
        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    protected void ApplyDamage(EnemyBase enemy, float baseDamage)
    {
        var sc = enemy.GetComponent<StatusController>();
        float finalDamage = baseDamage;
        if (sc != null)
        {
            finalDamage *= sc.GetDamageTakenMultiplier(damageTag);
            sc.ApplyStatus(statusEffect);
        }
        enemy.TakeDamage(finalDamage);
    }
}
