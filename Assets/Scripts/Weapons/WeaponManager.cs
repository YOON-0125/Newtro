using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어의 무기를 관리하는 매니저
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("무기 관리 설정")]
    [SerializeField] private int maxWeapons = 6;
    [SerializeField] private bool autoAttack = true;
    [SerializeField] private Transform weaponContainer;
    
    [Header("무기 프리팹")]
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
    
    // 이벤트
    [System.Serializable]
    public class WeaponManagerEvents
    {
        public UnityEvent<WeaponBase> OnWeaponAdded;
        public UnityEvent<WeaponBase> OnWeaponRemoved;
        public UnityEvent<WeaponBase> OnWeaponLevelUp;
        public UnityEvent OnWeaponLimitReached;
    }
    
    [Header("이벤트")]
    [SerializeField] private WeaponManagerEvents events;
    
    // 내부 변수
    private List<WeaponBase> equippedWeapons = new List<WeaponBase>();
    private Dictionary<string, WeaponBase> weaponsByName = new Dictionary<string, WeaponBase>();
    private Coroutine autoAttackCoroutine;
    
    // 프로퍼티
    public int EquippedWeaponCount => equippedWeapons.Count;
    public int MaxWeapons => maxWeapons;
    public bool HasSpaceForNewWeapon => equippedWeapons.Count < maxWeapons;
    public IReadOnlyList<WeaponBase> EquippedWeapons => equippedWeapons.AsReadOnly();
    public IReadOnlyList<WeaponBase> AllWeapons => equippedWeapons.AsReadOnly();
    
    /// <summary>
    /// 무기 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class WeaponData
    {
        public string weaponName;
        public GameObject weaponPrefab;
        public Sprite weaponIcon;
        [TextArea(2, 4)]
        public string description;
        public int unlockLevel = 1;
    }
    
    private void Awake()
    {
        // 무기 컨테이너 설정
        if (weaponContainer == null)
        {
            GameObject container = new GameObject("WeaponContainer");
            container.transform.SetParent(transform);
            weaponContainer = container.transform;
        }
    }
    
    private void Start()
    {
        // availableWeapons가 설정된 WeaponManager만 무기를 추가
        if (availableWeapons.Count > 0)
        {
            // 테스트를 위해 체인 라이트닝 무기 자동 장착
            AddWeapon("ChainLightning");

            // 자동 공격 시작
            if (autoAttack)
                StartAutoAttack();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}의 WeaponManager에 무기가 설정되지 않았습니다.");
        }
    }
    
    private void OnDestroy()
    {
        StopAutoAttack();
    }
    
    /// <summary>
    /// 자동 공격 시작
    /// </summary>
    public void StartAutoAttack()
    {
        if (autoAttackCoroutine == null)
            autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
    }
    
    /// <summary>
    /// 자동 공격 중지
    /// </summary>
    public void StopAutoAttack()
    {
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
    }
    
    /// <summary>
    /// 자동 공격 코루틴
    /// </summary>
    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            foreach (var weapon in equippedWeapons)
            {
                if (weapon != null && weapon.CanAttack)
                {
                    weapon.TryAttack();
                }
            }
            
            yield return new WaitForSeconds(0.1f); // 공격 체크 주기
        }
    }
    
    /// <summary>
    /// 무기 추가
    /// </summary>
    public bool AddWeapon(string weaponName)
    {
        // 이미 있는 무기인지 확인
        if (weaponsByName.ContainsKey(weaponName))
        {
            return LevelUpWeapon(weaponName);
        }
        
        // 무기 슬롯 확인
        if (!HasSpaceForNewWeapon)
        {
            events?.OnWeaponLimitReached?.Invoke();
            return false;
        }
        
        // 무기 데이터 찾기 (디버깅 추가)
        Debug.Log($"찾는 무기: '{weaponName}'");
        Debug.Log($"등록된 무기 개수: {availableWeapons.Count}");
        
        for (int i = 0; i < availableWeapons.Count; i++)
        {
            var weaponInfo = availableWeapons[i];
            Debug.Log($"무기 {i}: 이름='{weaponInfo.weaponName}', 프리팹={weaponInfo.weaponPrefab != null}");
        }
        
        WeaponData weaponData = availableWeapons.Find(w => w.weaponName == weaponName);
        if (weaponData == null || weaponData.weaponPrefab == null)
        {
            Debug.LogError($"무기를 찾을 수 없습니다: {weaponName}");
            return false;
        }
        
        // 무기 생성
        GameObject weaponObject = Instantiate(weaponData.weaponPrefab, weaponContainer);
        WeaponBase weapon = weaponObject.GetComponent<WeaponBase>();
        
        if (weapon == null)
        {
            Debug.LogError($"WeaponBase 컴포넌트가 없습니다: {weaponName}");
            Destroy(weaponObject);
            return false;
        }
        
        // 무기 등록
        equippedWeapons.Add(weapon);
        weaponsByName[weaponName] = weapon;
        
        var relicManager = FindObjectOfType<RelicManager>();
        if (relicManager != null)
        {
            float d = weapon.Damage;
            float c = weapon.Cooldown;
            float r = weapon.Range;
            relicManager.ModifyWeaponStats(weapon, ref d, ref c, ref r);
            if (d != weapon.Damage)
                weapon.ApplyDamageMultiplier(Mathf.Max(0.0001f, d / Mathf.Max(0.0001f, weapon.Damage)));
            if (c != weapon.Cooldown)
                weapon.ApplyCooldownMultiplier(Mathf.Max(0.0001f, c / Mathf.Max(0.0001f, weapon.Cooldown)));
            weapon.Range = r;
        }

        events?.OnWeaponAdded?.Invoke(weapon);

        Debug.Log($"무기 추가됨: {weaponName}");
        return true;
    }
    
    /// <summary>
    /// 무기 제거
    /// </summary>
    public bool RemoveWeapon(string weaponName)
    {
        if (!weaponsByName.TryGetValue(weaponName, out WeaponBase weapon))
            return false;
        
        equippedWeapons.Remove(weapon);
        weaponsByName.Remove(weaponName);
        
        events?.OnWeaponRemoved?.Invoke(weapon);
        
        if (weapon != null)
            Destroy(weapon.gameObject);
        
        Debug.Log($"무기 제거됨: {weaponName}");
        return true;
    }
    
    /// <summary>
    /// 무기 레벨업
    /// </summary>
    public bool LevelUpWeapon(string weaponName)
    {
        if (!weaponsByName.TryGetValue(weaponName, out WeaponBase weapon))
            return false;
        
        if (weapon.LevelUp())
        {
            var relicManager = FindObjectOfType<RelicManager>();
            if (relicManager != null)
            {
                float d = weapon.Damage;
                float c = weapon.Cooldown;
                float r = weapon.Range;
                relicManager.ModifyWeaponStats(weapon, ref d, ref c, ref r);
                if (d != weapon.Damage)
                    weapon.ApplyDamageMultiplier(Mathf.Max(0.0001f, d / Mathf.Max(0.0001f, weapon.Damage)));
                if (c != weapon.Cooldown)
                    weapon.ApplyCooldownMultiplier(Mathf.Max(0.0001f, c / Mathf.Max(0.0001f, weapon.Cooldown)));
                weapon.Range = r;
            }

            events?.OnWeaponLevelUp?.Invoke(weapon);
            Debug.Log($"무기 레벨업: {weaponName} -> Lv.{weapon.Level}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 특정 무기 가져오기
    /// </summary>
    public WeaponBase GetWeapon(string weaponName)
    {
        weaponsByName.TryGetValue(weaponName, out WeaponBase weapon);
        return weapon;
    }
    
    /// <summary>
    /// 무기 보유 여부 확인
    /// </summary>
    public bool HasWeapon(string weaponName)
    {
        return weaponsByName.ContainsKey(weaponName);
    }
    
    /// <summary>
    /// 무기 정보 가져오기
    /// </summary>
    public WeaponData GetWeaponData(string weaponName)
    {
        return availableWeapons.Find(w => w.weaponName == weaponName);
    }
    
    /// <summary>
    /// 사용 가능한 무기 목록
    /// </summary>
    public List<WeaponData> GetAvailableWeapons(int playerLevel)
    {
        return availableWeapons.FindAll(w => w.unlockLevel <= playerLevel);
    }
    
    /// <summary>
    /// 레벨업 가능한 무기 목록
    /// </summary>
    public List<WeaponBase> GetLevelUpableWeapons()
    {
        return equippedWeapons.FindAll(w => w != null && !w.IsMaxLevel);
    }
    
    /// <summary>
    /// 랜덤 무기 추가
    /// </summary>
    public bool AddRandomWeapon(int playerLevel)
    {
        List<WeaponData> availableList = GetAvailableWeapons(playerLevel);
        
        // 이미 보유한 무기 제외
        availableList.RemoveAll(w => HasWeapon(w.weaponName));
        
        if (availableList.Count == 0)
            return false;
        
        WeaponData randomWeapon = availableList[Random.Range(0, availableList.Count)];
        return AddWeapon(randomWeapon.weaponName);
    }
    
    /// <summary>
    /// 랜덤 무기 레벨업
    /// </summary>
    public bool LevelUpRandomWeapon()
    {
        List<WeaponBase> levelUpable = GetLevelUpableWeapons();
        
        if (levelUpable.Count == 0)
            return false;
        
        WeaponBase randomWeapon = levelUpable[Random.Range(0, levelUpable.Count)];
        return LevelUpWeapon(randomWeapon.WeaponName);
    }
    
    /// <summary>
    /// 모든 무기 공격
    /// </summary>
    public void AttackWithAllWeapons()
    {
        foreach (var weapon in equippedWeapons)
        {
            if (weapon != null && weapon.CanAttack)
            {
                weapon.TryAttack();
            }
        }
    }
    
    /// <summary>
    /// 무기 매니저 정보 가져오기
    /// </summary>
    public string GetManagerInfo()
    {
        string info = $"무기 매니저 정보\n";
        info += $"장착된 무기: {EquippedWeaponCount}/{MaxWeapons}\n";
        info += $"자동 공격: {(autoAttack ? "활성" : "비활성")}\n\n";
        
        info += "장착된 무기 목록:\n";
        foreach (var weapon in equippedWeapons)
        {
            if (weapon != null)
            {
                info += $"- {weapon.GetWeaponInfo()}\n\n";
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (equippedWeapons != null)
        {
            foreach (var weapon in equippedWeapons)
            {
                if (weapon != null)
                {
                    // 무기별 공격 범위 표시는 각 무기에서 처리
                }
            }
        }
    }
}