using UnityEngine;

/// <summary>
/// 통합 데미지 계산 시스템
/// 공식: 최종 데미지 = (기본값 + 고정 보너스들) × (1 + 속성% + 전역%)
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 기본 데미지 계산
    /// </summary>
    /// <param name="baseDamage">무기 기본 데미지</param>
    /// <param name="flatBonus">고정 보너스 (레벨업, 유물 등)</param>
    /// <param name="percentBonus">퍼센트 보너스 (전역 데미지 증가)</param>
    /// <returns>계산된 최종 데미지</returns>
    public static float Calculate(float baseDamage, float flatBonus = 0f, float percentBonus = 0f)
    {
        float finalDamage = (baseDamage + flatBonus) * (1f + percentBonus);
        return Mathf.Max(0f, finalDamage); // 음수 방지
    }
    
    /// <summary>
    /// 속성 효과를 포함한 데미지 계산
    /// </summary>
    /// <param name="baseDamage">무기 기본 데미지</param>
    /// <param name="flatBonus">고정 보너스</param>
    /// <param name="globalPercent">전역 퍼센트 보너스</param>
    /// <param name="elementalPercent">속성 퍼센트 보너스 (저항/약점)</param>
    /// <returns>속성 효과가 적용된 최종 데미지</returns>
    public static float CalculateWithElemental(float baseDamage, float flatBonus = 0f, float globalPercent = 0f, float elementalPercent = 0f)
    {
        float finalDamage = (baseDamage + flatBonus) * (1f + globalPercent + elementalPercent);
        return Mathf.Max(0f, finalDamage);
    }
    
    /// <summary>
    /// 데미지 타입에 따른 속성 배율 가져오기
    /// </summary>
    /// <param name="damageTag">데미지 타입</param>
    /// <param name="targetStatusController">대상의 StatusController</param>
    /// <returns>속성 배율 (-1.0 ~ +1.0, 0이 기본)</returns>
    public static float GetElementalMultiplier(DamageTag damageTag, StatusController targetStatusController)
    {
        if (targetStatusController == null) return 0f;
        
        // StatusController의 기존 GetDamageTakenMultiplier를 활용
        float multiplier = targetStatusController.GetDamageTakenMultiplier(damageTag);
        
        // 1.0 기준에서 퍼센트로 변환 (1.0 = 0%, 1.5 = +50%, 0.5 = -50%)
        return multiplier - 1f;
    }
    
    /// <summary>
    /// 디버그용 데미지 계산 정보 출력
    /// </summary>
    /// <param name="weaponName">무기 이름</param>
    /// <param name="baseDamage">기본 데미지</param>
    /// <param name="flatBonus">고정 보너스</param>
    /// <param name="percentBonus">퍼센트 보너스</param>
    /// <param name="elementalBonus">속성 보너스</param>
    /// <returns>계산된 데미지와 함께 디버그 정보 출력</returns>
    public static float CalculateWithDebug(string weaponName, float baseDamage, float flatBonus = 0f, float percentBonus = 0f, float elementalBonus = 0f)
    {
        float baseTotal = baseDamage + flatBonus;
        float totalPercent = percentBonus + elementalBonus;
        float finalDamage = baseTotal * (1f + totalPercent);
        
        Debug.Log($"[DamageCalculator] {weaponName} 데미지 계산:");
        Debug.Log($"  기본: {baseDamage:F1} + 고정보너스: {flatBonus:F1} = {baseTotal:F1}");
        Debug.Log($"  배율: 전역 {percentBonus:P1} + 속성 {elementalBonus:P1} = {totalPercent:P1}");
        Debug.Log($"  최종: {baseTotal:F1} × {(1f + totalPercent):F2} = {finalDamage:F1}");
        
        return Mathf.Max(0f, finalDamage);
    }
    
    /// <summary>
    /// 수확 체감 계산 (같은 업그레이드 반복 시 효과 감소)
    /// </summary>
    /// <param name="baseValue">기본 증가값</param>
    /// <param name="currentCount">현재 획득 횟수</param>
    /// <param name="diminishingRate">감소율 (0.8 = 20% 감소)</param>
    /// <returns>수확 체감이 적용된 증가값</returns>
    public static float CalculateDiminishingReturns(float baseValue, int currentCount, float diminishingRate = 0.8f)
    {
        if (currentCount <= 0) return baseValue;
        
        float diminishedValue = baseValue * Mathf.Pow(diminishingRate, currentCount);
        return Mathf.Max(baseValue * 0.1f, diminishedValue); // 최소 10%는 보장
    }
}