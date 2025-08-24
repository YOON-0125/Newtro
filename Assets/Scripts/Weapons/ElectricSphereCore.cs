using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전기 데미지 타입 열거형
/// </summary>
public enum ElectricDamageType
{
    Instant,    // 즉시 데미지 (적 진입 시)
    Tick        // 틱 데미지 (지속 데미지)
}

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
    
    // 범위 인디케이터
    private CircleIndicator rangeIndicator;
    
    // 이벤트 기반 효과를 위한 적별 데이터 저장
    private readonly Dictionary<EnemyBase, bool> subscribedEnemies = new Dictionary<EnemyBase, bool>();

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
        
        // 범위 인디케이터 초기화 (ElectricSphere에서 설정된 경우)
        rangeIndicator = GetComponent<CircleIndicator>();
        if (rangeIndicator != null)
        {
            Debug.Log($"[ElectricSphereCore] 범위 인디케이터 초기화 완료 - 반지름: {linkRadius}");
        }
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
            
            if (enemies.Count > 0)
            {
                Debug.Log($"[ElectricSphereCore] ⏰ 틱 데미지 발동! 대상 {enemies.Count}마리");
            }
            
            // Collection modification 에러 방지를 위해 배열로 복사
            var enemiesCopy = new EnemyBase[enemies.Count];
            enemies.CopyTo(enemiesCopy);
            
            foreach (var e in enemiesCopy)
            {
                if (e != null && enemies.Contains(e)) // 여전히 컬렉션에 있는지 확인
                    ApplyDamage(e, damage, ElectricDamageType.Tick);
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
                Debug.Log($"[ElectricSphereCore] 🎯 적 진입! 즉시 데미지 적용: {enemy.name}");
                ApplyDamage(enemy, damage, ElectricDamageType.Instant);
                enemies.Add(enemy);
                
                // 이벤트 기반 개별화된 효과 시스템 구독
                SubscribeToEnemyEvents(enemy);
            }
        }
        else
        {
            var field = other.GetComponent<ElectricField>();
            if (field != null)
            {
                Debug.Log($"[ElectricSphereCore] 🔗 전기장 발견! 펄스 전파 시작");
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
            {
                enemies.Remove(enemy);
                
                // 이벤트 구독 해제
                UnsubscribeFromEnemyEvents(enemy);
            }
        }
    }

    private void SpreadPulse(ElectricField start)
    {
        var visited = new HashSet<ElectricField>();
        var queue = new Queue<ElectricField>();
        queue.Enqueue(start);
        visited.Add(start);
        int fieldCount = 0;

        while (queue.Count > 0)
        {
            var f = queue.Dequeue();
            fieldCount++;
            Debug.Log($"[ElectricSphereCore] 🌊 전기장 #{fieldCount} 펄스! 데미지: {damage}, 틱간격: {tickInterval}");
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
        
        Debug.Log($"[ElectricSphereCore] 🔗 펄스 전파 완료! 총 {fieldCount}개 전기장 활성화");
    }

    private void Release()
    {
        // 모든 이벤트 구독 해제 (메모리 누수 방지)
        CleanupAllEventSubscriptions();
        
        enemies.Clear();
        rb.linearVelocity = Vector2.zero;
        
        // 범위 인디케이터 정리
        if (rangeIndicator != null)
        {
            rangeIndicator.HideIndicator();
            Debug.Log("[ElectricSphereCore] 범위 인디케이터 숨김");
        }
        
        if (SimpleObjectPool.Instance != null)
            SimpleObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 전기 데미지 시각 효과를 생성합니다.
    /// </summary>
    private void SpawnElectricDamageEffect(Vector3 position, ElectricDamageType damageType)
    {
        if (damageType == ElectricDamageType.Instant)
        {
            // 즉시 데미지: 큰 전기 폭발 이펙트
            SpawnInstantDamageEffect(position);
        }
        else
        {
            // 틱 데미지: 작은 스파크 효과
            SpawnTickDamageEffect(position);
        }
    }
    
    /// <summary>
    /// 즉시 데미지용 큰 전기 폭발 이펙트
    /// </summary>
    private void SpawnInstantDamageEffect(Vector3 position)
    {
        // 전기 이펙트 프리팹 경로에서 로드 (큰 효과)
        GameObject effectPrefab = Resources.Load<GameObject>("Electric_SphereEffectPrefab_Large");
        
        // Large 버전이 없으면 기본 버전 사용
        if (effectPrefab == null)
        {
            effectPrefab = Resources.Load<GameObject>("Electric_SphereEffectPrefab");
        }
        
        // Resources 폴더에서 찾지 못한 경우 대체 경로 시도
        if (effectPrefab == null)
        {
            effectPrefab = Resources.Load<GameObject>("Prefab/Skills/Electric_SphereEffectPrefab");
        }
        
        if (effectPrefab != null)
        {
            GameObject effect = SimpleObjectPool.Instance != null ?
                SimpleObjectPool.Instance.Get(effectPrefab, position, Quaternion.identity) :
                Instantiate(effectPrefab, position, Quaternion.identity);
            
            // 1초 후 자동 제거
            if (SimpleObjectPool.Instance != null)
            {
                SimpleObjectPool.Instance.Release(effect, 1f);
            }
            else
            {
                Destroy(effect, 1f);
            }
            
            Debug.Log($"[ElectricSphereCore] 💥 즉시 데미지 이펙트 생성! 위치: {position}");
        }
        else
        {
            Debug.LogWarning("[ElectricSphereCore] Electric_SphereEffectPrefab을 찾을 수 없습니다. 대체 효과를 사용합니다.");
            CreateFallbackElectricEffect(position, 0.8f); // 큰 크기
        }
    }
    
    /// <summary>
    /// 틱 데미지용 작은 스파크 효과
    /// </summary>
    private void SpawnTickDamageEffect(Vector3 position)
    {
        // 작은 스파크 효과는 LineRenderer 기반으로 직접 생성
        CreateElectricSparks(position);
        
        Debug.Log($"[ElectricSphereCore] ⚡ 틱 데미지 스파크 생성! 위치: {position}");
    }
    
    /// <summary>
    /// 작은 전기 스파크들을 생성 (틱 데미지용)
    /// </summary>
    private void CreateElectricSparks(Vector3 position)
    {
        int sparkCount = Random.Range(2, 4); // 2-3개의 작은 스파크
        
        for (int i = 0; i < sparkCount; i++)
        {
            GameObject spark = new GameObject($"ElectricSpark_Tick_{i}");
            spark.transform.position = position;
            
            LineRenderer lr = spark.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = new Color(1f, 1f, 0.3f, 0.8f); // 약간 투명한 노란색
            lr.startWidth = 0.03f;
            lr.endWidth = 0.01f;
            lr.positionCount = 2;
            lr.sortingOrder = 10;
            
            // 랜덤한 방향으로 짧은 스파크
            Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(0.2f, 0.4f);
            lr.SetPosition(0, position);
            lr.SetPosition(1, position + (Vector3)randomDir);
            
            // 0.15초 후 제거 (틱 데미지는 빠르게 사라짐)
            Destroy(spark, 0.15f);
        }
    }
    
    /// <summary>
    /// 이펙트 프리팹이 없을 때 사용할 간단한 대체 효과
    /// </summary>
    private void CreateFallbackElectricEffect(Vector3 position, float size = 0.5f)
    {
        // 간단한 노란색 스프라이트로 전기 효과 시뮬레이션
        GameObject fallbackEffect = new GameObject("ElectricSpark_Fallback");
        fallbackEffect.transform.position = position;
        
        SpriteRenderer sr = fallbackEffect.AddComponent<SpriteRenderer>();
        // 기본 2D 스프라이트 생성 (더 안전한 방법)
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.color = Color.yellow;
        sr.size = Vector2.one * size;
        
        // 간단한 스케일 애니메이션
        StartCoroutine(AnimateFallbackEffect(fallbackEffect));
        
        Debug.Log($"[ElectricSphereCore] ⚡ 대체 전기 이펙트 생성 (크기: {size})");
    }
    
    /// <summary>
    /// 대체 효과 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator AnimateFallbackEffect(GameObject effect)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = effect.transform.localScale;
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 크기는 커지고 투명도는 감소
            effect.transform.localScale = originalScale * (1f + progress * 2f);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - progress);
            
            yield return null;
        }
        
        Destroy(effect);
    }

    private void ApplyDamage(EnemyBase enemy, float baseDamage, ElectricDamageType damageType = ElectricDamageType.Instant)
    {
        var sc = enemy.GetComponent<StatusController>();
        float finalDamage = baseDamage;
        float multiplier = 1f;
        
        if (sc != null)
        {
            multiplier = sc.GetDamageTakenMultiplier(damageTag);
            finalDamage *= multiplier;
            sc.ApplyStatus(statusEffect);
        }
        
        Debug.Log($"[ElectricSphereCore] ⚡ 데미지 적용! 타입: {damageType}, 대상: {enemy.name}, 기본데미지: {baseDamage:F1}, 배율: {multiplier:F2}, 최종데미지: {finalDamage:F1}");
        enemy.TakeDamage(finalDamage, damageTag);
        
        // 💥 데미지 타입에 따른 차별화된 시각 효과
        SpawnElectricDamageEffect(enemy.transform.position, damageType);
    }
    
    /// <summary>
    /// 적의 OnDamage 이벤트에 구독하여 개별화된 효과 적용
    /// </summary>
    private void SubscribeToEnemyEvents(EnemyBase enemy)
    {
        if (enemy == null || subscribedEnemies.ContainsKey(enemy))
            return;
            
        // 이벤트 구독
        if (enemy.events != null && enemy.events.OnDamage != null)
        {
            enemy.events.OnDamage.AddListener(damage => OnEnemyDamaged(enemy, damage));
            subscribedEnemies[enemy] = true;
            
            Debug.Log($"[ElectricSphereCore] 🎯 {enemy.name}의 OnDamage 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning($"[ElectricSphereCore] {enemy.name}의 events가 null이거나 OnDamage 이벤트가 없습니다.");
        }
    }
    
    /// <summary>
    /// 적의 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEnemyEvents(EnemyBase enemy)
    {
        if (enemy == null || !subscribedEnemies.ContainsKey(enemy))
            return;
            
        // 이벤트 구독 해제
        if (enemy.events != null && enemy.events.OnDamage != null)
        {
            enemy.events.OnDamage.RemoveListener(damage => OnEnemyDamaged(enemy, damage));
        }
        
        subscribedEnemies.Remove(enemy);
        Debug.Log($"[ElectricSphereCore] 🎯 {enemy.name}의 OnDamage 이벤트 구독 해제 완료");
    }
    
    /// <summary>
    /// 모든 이벤트 구독 정리 (메모리 누수 방지)
    /// </summary>
    private void CleanupAllEventSubscriptions()
    {
        var enemyList = new List<EnemyBase>(subscribedEnemies.Keys);
        foreach (var enemy in enemyList)
        {
            UnsubscribeFromEnemyEvents(enemy);
        }
        subscribedEnemies.Clear();
        
        Debug.Log("[ElectricSphereCore] 🧹 모든 이벤트 구독 정리 완료");
    }
    
    /// <summary>
    /// 적이 데미지를 받았을 때 호출되는 개별화된 효과 콜백
    /// </summary>
    private void OnEnemyDamaged(EnemyBase enemy, float damageAmount)
    {
        if (enemy == null) return;
        
        // 적 타입별 개별화된 효과 적용
        ApplyIndividualizedEffect(enemy, damageAmount);
    }
    
    /// <summary>
    /// 적 타입별 개별화된 시각 효과 적용
    /// </summary>
    private void ApplyIndividualizedEffect(EnemyBase enemy, float damageAmount)
    {
        // 적의 이름이나 태그에 따른 개별화된 효과
        string enemyTypeLower = enemy.name.ToLower();
        
        if (enemyTypeLower.Contains("boss"))
        {
            // 보스: 더 큰 효과와 화면 진동
            SpawnBossElectricEffect(enemy.transform.position, damageAmount);
        }
        else if (enemyTypeLower.Contains("shield"))
        {
            // 방패 적: 스파크가 방패에 튕기는 효과
            SpawnShieldSparkEffect(enemy.transform.position);
        }
        else if (enemyTypeLower.Contains("mage"))
        {
            // 마법사: 마나가 흩어지는 효과
            SpawnMageElectricEffect(enemy.transform.position);
        }
        else
        {
            // 일반 적: 기본 추가 스파크 효과
            SpawnBasicIndividualEffect(enemy.transform.position, damageAmount);
        }
        
        Debug.Log($"[ElectricSphereCore] 🎨 개별화된 효과 적용: {enemy.name} (데미지: {damageAmount:F1})");
    }
    
    /// <summary>
    /// 보스용 강화된 전기 효과
    /// </summary>
    private void SpawnBossElectricEffect(Vector3 position, float damage)
    {
        // 더 많은 스파크와 더 큰 효과
        int sparkCount = Mathf.RoundToInt(damage / 10f) + 5; // 데미지에 비례한 스파크 개수
        
        for (int i = 0; i < sparkCount; i++)
        {
            CreateEnhancedElectricSpark(position, 0.8f, 0.4f);
        }
        
        // 추가: 화면 진동 효과 (Camera Shake)
        // Camera.main?.GetComponent<CameraShake>()?.Shake(0.1f, 0.2f);
    }
    
    /// <summary>
    /// 방패 적용 튕기는 스파크 효과
    /// </summary>
    private void SpawnShieldSparkEffect(Vector3 position)
    {
        // 방패에서 튕기는 듯한 효과 (위쪽으로 스파크)
        for (int i = 0; i < 3; i++)
        {
            GameObject spark = new GameObject($"ShieldSpark_{i}");
            spark.transform.position = position;
            
            LineRenderer lr = spark.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = new Color(0.8f, 0.8f, 1f, 0.9f); // 약간 파란 빛
            lr.startWidth = 0.04f;
            lr.endWidth = 0.01f;
            lr.positionCount = 2;
            lr.sortingOrder = 10;
            
            // 위쪽 방향으로 스파크 (방패에서 튕김)
            Vector3 upwardDir = (Vector3.up + Random.insideUnitSphere * 0.3f).normalized * Random.Range(0.3f, 0.6f);
            lr.SetPosition(0, position);
            lr.SetPosition(1, position + upwardDir);
            
            Destroy(spark, 0.25f);
        }
    }
    
    /// <summary>
    /// 마법사용 마나 흩어짐 효과
    /// </summary>
    private void SpawnMageElectricEffect(Vector3 position)
    {
        // 마나 파티클이 흩어지는 효과
        int particleCount = Random.Range(4, 7);
        
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject($"MageMana_{i}");
            particle.transform.position = position;
            
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            // 기본 2D 스프라이트 생성 (더 안전한 방법)
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            sr.color = new Color(0.6f, 0.3f, 1f, 0.8f); // 마법사스러운 보라색
            sr.size = Vector2.one * 0.1f;
            
            // 랜덤 방향으로 흩어짐
            Vector3 scatterDir = Random.insideUnitSphere * Random.Range(0.5f, 1f);
            StartCoroutine(AnimateScatterParticle(particle, position, position + scatterDir));
        }
    }
    
    /// <summary>
    /// 일반 적용 기본 추가 효과
    /// </summary>
    private void SpawnBasicIndividualEffect(Vector3 position, float damage)
    {
        // 데미지에 비례한 작은 추가 스파크
        if (damage > 15f) // 높은 데미지일 때만 추가 효과
        {
            CreateEnhancedElectricSpark(position, 0.4f, 0.25f);
        }
    }
    
    /// <summary>
    /// 강화된 전기 스파크 생성
    /// </summary>
    private void CreateEnhancedElectricSpark(Vector3 position, float length, float duration)
    {
        GameObject spark = new GameObject("EnhancedElectricSpark");
        spark.transform.position = position;
        
        LineRenderer lr = spark.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = new Color(1f, 1f, 0.2f, 1f); // 강렬한 노란색
        lr.startWidth = 0.05f;
        lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.sortingOrder = 15;
        
        Vector2 randomDir = Random.insideUnitCircle.normalized * length;
        lr.SetPosition(0, position);
        lr.SetPosition(1, position + (Vector3)randomDir);
        
        Destroy(spark, duration);
    }
    
    /// <summary>
    /// 흩어지는 파티클 애니메이션
    /// </summary>
    private IEnumerator AnimateScatterParticle(GameObject particle, Vector3 start, Vector3 end)
    {
        float duration = 0.8f;
        float elapsed = 0f;
        SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 위치와 알파 애니메이션
            particle.transform.position = Vector3.Lerp(start, end, progress);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * (1f - progress));
            
            yield return null;
        }
        
        Destroy(particle);
    }
}
