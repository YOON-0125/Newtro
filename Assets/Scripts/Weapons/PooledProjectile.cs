 codex/implement-relic-system-for-rewards
using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    public float Speed { get; set; }
    public int Pierce { get; set; }

    private Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplySpeedMultiplier(float mul)
    {
        Speed *= mul;
        if (rb != null)
        {
            rb.linearVelocity *= mul;
        }
    }
=======
using System;
using UnityEngine;

/// <summary>
/// 풀에서 사용하는 기본 투사체
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PooledProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private TrailRenderer trail;

    private float damage;
    private float speed;
    private float lifetime;
    private Vector2 direction;
    private int pierce;
    private Action<EnemyBase> onHit;
    private DamageTag damageTag = DamageTag.Physical;
    private StatusEffect statusEffect;

    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        trail = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize(float dmg, float spd, float life, Vector2 dir, DamageTag tag, StatusEffect effect, int pierceCount = 1, Action<EnemyBase> onHit = null)
    {
        damage = dmg;
        speed = spd;
        lifetime = life;
        direction = dir.normalized;
        pierce = pierceCount;
        this.onHit = onHit;
        damageTag = tag;
        statusEffect = effect;
        spawnTime = Time.time;

        rb.linearVelocity = direction * speed;

        if (trail != null) trail.Clear();
    }

    private void Update()
    {
        if (Time.time >= spawnTime + lifetime)
        {
            onHit?.Invoke(null);
            Release();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                var sc = enemy.GetComponent<StatusController>();
                float finalDamage = damage;
                if (sc != null)
                {
                    finalDamage *= sc.GetDamageTakenMultiplier(damageTag);
                    sc.ApplyStatus(statusEffect);
                }
                enemy.TakeDamage(finalDamage);
                onHit?.Invoke(enemy);
            }

            pierce--;
            if (pierce <= 0)
            {
                Release();
            }
        }
    }

    private void Release()
    {
        rb.linearVelocity = Vector2.zero;
        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
main
}
