using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전기 구체의 코어 처리
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class ElectricSphereCore : MonoBehaviour
{
    private float damage;
    private float tickInterval;
    private float radius;
    private float speed;
    private float lifetime;
    private float linkRadius;
    private DamageTag damageTag = DamageTag.Lightning;
    private StatusEffect statusEffect;

    private float lifeTimer;
    private float tickTimer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private readonly HashSet<EnemyBase> enemies = new HashSet<EnemyBase>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    public void Initialize(Vector2 dir, float damage, float tickInterval, float radius, float speed, float lifetime, float linkRadius, StatusEffect effect)
    {
        this.damage = damage;
        this.tickInterval = tickInterval;
        this.radius = radius;
        this.speed = speed;
        this.lifetime = lifetime;
        this.linkRadius = linkRadius;
        statusEffect = effect;

        lifeTimer = 0f;
        tickTimer = 0f;
        enemies.Clear();

        col.radius = radius;
        rb.linearVelocity = dir.normalized * speed;
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Release();
            return;
        }
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            foreach (var e in enemies)
            {
                if (e != null)
                    ApplyDamage(e, damage);
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
                ApplyDamage(enemy, damage);
                enemies.Add(enemy);
            }
        }
        else
        {
            var field = other.GetComponent<ElectricField>();
            if (field != null)
            {
                SpreadPulse(field);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
                enemies.Remove(enemy);
        }
    }

    private void SpreadPulse(ElectricField start)
    {
        var visited = new HashSet<ElectricField>();
        var queue = new Queue<ElectricField>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var f = queue.Dequeue();
            f.ConfigureEffect(DamageTag.Lightning, statusEffect);
            f.Pulse(damage, tickInterval);
            Collider2D[] cols = Physics2D.OverlapCircleAll(f.transform.position, linkRadius);
            foreach (var c in cols)
            {
                var nf = c.GetComponent<ElectricField>();
                if (nf != null && !visited.Contains(nf))
                {
                    visited.Add(nf);
                    queue.Enqueue(nf);
                }
            }
        }
    }

    private void Release()
    {
        enemies.Clear();
        rb.linearVelocity = Vector2.zero;
        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    private void ApplyDamage(EnemyBase enemy, float baseDamage)
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
