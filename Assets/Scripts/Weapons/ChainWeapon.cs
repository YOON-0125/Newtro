using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.LightningBolt;

/// <summary>
/// 체인 무기 (라이트닝 체인)
/// </summary>
public class ChainWeapon : WeaponBase
{
    [Header("체인 설정")]
    [SerializeField] private int maxChainTargets = 3;
    [SerializeField] private float chainRange = 8f;
    [SerializeField] private float chainDelay = 0.05f; // 체인 간격을 더 빠르게
    [SerializeField] private GameObject chainEffectPrefab;
    [SerializeField] private float effectDuration = 0.5f; // 이펙트가 사라지기까지의 시간 (빠르게)
    
    private HashSet<Transform> hitTargets = new HashSet<Transform>();
    
    protected override void ExecuteAttack()
    {
        Transform firstTarget = FindNearestTargetFromPlayer();
        if (firstTarget == null)
        {
            OnAttackComplete();
            return;
        }
        
        StartCoroutine(ChainAttackCoroutine(firstTarget));
    }
    
    /// <summary>
    /// 플레이어 위치에서 가장 가까운 적 찾기
    /// </summary>
    private Transform FindNearestTargetFromPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("플레이어를 찾을 수 없습니다!");
            return null;
        }
        
        Vector3 playerPosition = player.transform.position;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerPosition, chainRange, LayerMask.GetMask("Enemy"));
        
        if (enemies.Length == 0)
        {
            Debug.Log($"ChainWeapon: 플레이어({playerPosition}) 주변 범위({chainRange})에 적이 없습니다.");
            return null;
        }
            
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector2.Distance(playerPosition, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }
        
        Debug.Log($"ChainWeapon: 플레이어 주변에서 가장 가까운 적 발견 - 거리: {nearestDistance:F1}");
        return nearestEnemy;
    }
    
    private IEnumerator ChainAttackCoroutine(Transform firstTarget)
    {
        hitTargets.Clear();
        Transform currentTarget = firstTarget;
        
        // 실제 플레이어 위치 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 previousPosition = player != null ? player.transform.position : transform.position;
        
        Debug.Log($"체인 공격 시작: 플레이어 위치={previousPosition}");
        
        for (int i = 0; i < maxChainTargets && currentTarget != null; i++)
        {
            AttackTarget(currentTarget, previousPosition);
            hitTargets.Add(currentTarget);
            
            previousPosition = currentTarget.position;
            Transform nextTarget = FindNextChainTarget(currentTarget.position);
            
            if (nextTarget != null)
            {
                yield return new WaitForSeconds(chainDelay);
            }
            
            currentTarget = nextTarget;
        }
        
        OnAttackComplete();
    }
    
    private void AttackTarget(Transform target, Vector3 fromPosition)
    {
        var enemy = target.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        
        CreateChainEffect(fromPosition, target.position);
    }
    
    private Transform FindNextChainTarget(Vector3 currentPosition)
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(currentPosition, chainRange, LayerMask.GetMask("Enemy"));
        
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            if (hitTargets.Contains(enemy.transform))
                continue;
            
            float distance = Vector2.Distance(currentPosition, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = enemy.transform;
            }
        }
        
        return nearestTarget;
    }
    
    /// <summary>
    /// LightningBolt 에셋을 사용한 체인 이펙트 생성
    /// </summary>
    private void CreateChainEffect(Vector3 startPos, Vector3 endPos)
    {
        if (chainEffectPrefab == null)
        {
            Debug.LogError("ChainEffectPrefab이 비어있습니다! 인스펙터에서 설정해주세요.");
            return;
        }
        
        // 1. 이펙트 생성 (위치는 중요하지 않음 - Start/End 오브젝트로 제어)
        GameObject effect = Instantiate(chainEffectPrefab, Vector3.zero, Quaternion.identity);
        
        // 2. LightningBoltScript 컴포넌트 가져오기
        LightningBoltScript lightningScript = effect.GetComponent<LightningBoltScript>();
        if (lightningScript == null)
        {
            Debug.LogError("LightningBoltScript 컴포넌트를 찾을 수 없습니다!");
            Destroy(effect);
            return;
        }
        
        // 3. Start와 End 오브젝트의 위치를 정확히 설정
        if (lightningScript.StartObject != null)
        {
            lightningScript.StartObject.transform.position = startPos;
        }
        
        if (lightningScript.EndObject != null)
        {
            lightningScript.EndObject.transform.position = endPos;
        }
        
        Debug.Log($"번개 체인 이펙트: {startPos} → {endPos}, 거리={Vector3.Distance(startPos, endPos):F2}");
        
        // 4. 일정 시간 후 이펙트 제거
        Destroy(effect, effectDuration);
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        switch (level)
        {
            case 2: maxChainTargets = 4; break;
            case 3: chainRange *= 1.2f; break;
            case 4: maxChainTargets = 5; break;
            case 5: cooldown *= 0.8f; break;
            case 6: chainRange *= 1.3f; break;
            case 7: maxChainTargets = 6; break;
            case 8: chainDelay *= 0.7f; break;
            case 9: cooldown *= 0.7f; break;
            case 10: maxChainTargets = 8; chainRange *= 1.5f; damage *= 1.5f; break;
        }
    }
    
    protected override float GetAttackRange()
    {
        return chainRange;
    }
    
    public override string GetWeaponInfo()
    {
        return base.GetWeaponInfo() + 
               "\nChain Targets: " + maxChainTargets +
               "\nChain Range: " + chainRange.ToString("F1") +
               "\nChain Delay: " + chainDelay.ToString("F2") + "s";
    }
    
    protected override void OnDrawGizmosSelected()
    {
        // 플레이어 위치에서 공격 범위 시각화
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.transform.position, chainRange);
        }
        else
        {
            // 플레이어가 없으면 기본 위치에서 표시
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, chainRange);
        }
    }
}
