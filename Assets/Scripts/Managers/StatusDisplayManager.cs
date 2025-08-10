using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 일시정지 메뉴에서 게임 상태 정보를 표시하는 매니저
/// </summary>
public class StatusDisplayManager : MonoBehaviour
{
    [Header("플레이어 상태 UI")]
    [SerializeField] private Text playerHealthText;
    [SerializeField] private Text playerStatsText;
    
    [Header("무기 상태 UI")]
    [SerializeField] private Text weaponListText;
    [SerializeField] private Text weaponStatsText;
    
    [Header("게임 진행도 UI")]
    [SerializeField] private Text gameProgressText;
    [SerializeField] private Text gameTimeText;
    
    // 참조할 게임 시스템들
    private WeaponManager weaponManager;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    
    void Awake()
    {
        // 게임 시스템 참조 찾기
        FindGameSystems();
    }
    
    /// <summary>
    /// 게임 시스템들 찾기
    /// </summary>
    private void FindGameSystems()
    {
        // WeaponManager 찾기
        weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogWarning("WeaponManager를 찾을 수 없습니다!");
        }
        
        // PlayerHealth 찾기
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth를 찾을 수 없습니다!");
        }
        
        // GameManager 찾기
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 상태 정보 UI 업데이트
    /// </summary>
    public void UpdateDisplay()
    {
        UpdatePlayerInfo();
        UpdateWeaponInfo();
        UpdateGameProgress();
    }
    
    /// <summary>
    /// 플레이어 정보 업데이트
    /// </summary>
    private void UpdatePlayerInfo()
    {
        if (playerHealth != null)
        {
            // 체력 정보
            if (playerHealthText != null)
            {
                int currentHealth = Mathf.FloorToInt(playerHealth.Health);
                int maxHealth = Mathf.FloorToInt(playerHealth.MaxHealth);
                int healthPercentage = Mathf.FloorToInt(playerHealth.HealthPercentage * 100);
                
                playerHealthText.text = $"체력: {currentHealth}/{maxHealth} ({healthPercentage}%)";
            }
            
            // 기타 플레이어 스탯
            if (playerStatsText != null)
            {
                string stats = "";
                stats += $"방어력: {playerHealth.Armor:F0}\\n";
                stats += $"상태: {(playerHealth.IsDead ? "사망" : playerHealth.IsInvincible ? "무적" : "정상")}\\n";
                
                // PlayerObj에서 이동속도 정보 가져오기 (있다면)
                PlayerObj playerObj = FindObjectOfType<PlayerObj>();
                if (playerObj != null)
                {
                    stats += $"이동속도: {playerObj._charMS:F1}\\n";
                }
                
                playerStatsText.text = stats;
            }
        }
        else
        {
            // PlayerHealth가 없을 때 기본 텍스트
            if (playerHealthText != null)
                playerHealthText.text = "플레이어 정보를 찾을 수 없습니다.";
            if (playerStatsText != null)
                playerStatsText.text = "";
        }
    }
    
    /// <summary>
    /// 무기 정보 업데이트
    /// </summary>
    private void UpdateWeaponInfo()
    {
        if (weaponManager != null)
        {
            // 무기 목록
            if (weaponListText != null)
            {
                string weaponList = $"장착된 무기 ({weaponManager.EquippedWeaponCount}/{weaponManager.MaxWeapons}):\\n\\n";
                
                var equippedWeapons = weaponManager.EquippedWeapons;
                if (equippedWeapons.Count > 0)
                {
                    for (int i = 0; i < equippedWeapons.Count; i++)
                    {
                        var weapon = equippedWeapons[i];
                        if (weapon != null)
                        {
                            weaponList += $"{i + 1}. {weapon.WeaponName} (Lv.{weapon.Level})\\n";
                        }
                    }
                }
                else
                {
                    weaponList += "장착된 무기가 없습니다.";
                }
                
                weaponListText.text = weaponList;
            }
            
            // 무기 상세 스탯
            if (weaponStatsText != null)
            {
                string weaponStats = "";
                
                var equippedWeapons = weaponManager.EquippedWeapons;
                if (equippedWeapons.Count > 0)
                {
                    weaponStats = "무기 상세 정보:\\n\\n";
                    
                    foreach (var weapon in equippedWeapons)
                    {
                        if (weapon != null)
                        {
                            weaponStats += $"[{weapon.WeaponName}]\\n";
                            weaponStats += $"레벨: {weapon.Level}/{weapon.MaxLevel}\\n";
                            weaponStats += $"데미지: {weapon.Damage:F1}\\n";
                            weaponStats += $"쿨다운: {weapon.Cooldown:F1}초\\n";
                            // weaponStats += $"공격 속도: {weapon.AttackSpeed:F1}\\n"; // AttackSpeed 프로퍼티 없음
                            // weaponStats += $"범위: {weapon.Range:F1}\\n"; // Range 프로퍼티 없음
                            
                            // 다음 레벨 정보 (최대 레벨이 아닌 경우)
                            if (!weapon.IsMaxLevel)
                            {
                                weaponStats += $"다음 레벨: 레벨업 시 개선됨\\n";
                                // weaponStats += $"다음 레벨: 데미지 +{weapon.DamagePerLevel:F1}\\n"; // DamagePerLevel 프로퍼티 없음
                            }
                            else
                            {
                                weaponStats += "최대 레벨 달성!\\n";
                            }
                            
                            weaponStats += "\\n";
                        }
                    }
                }
                else
                {
                    weaponStats = "장착된 무기가 없어 상세 정보를 표시할 수 없습니다.";
                }
                
                weaponStatsText.text = weaponStats;
            }
        }
        else
        {
            // WeaponManager가 없을 때 기본 텍스트
            if (weaponListText != null)
                weaponListText.text = "무기 정보를 찾을 수 없습니다.";
            if (weaponStatsText != null)
                weaponStatsText.text = "";
        }
    }
    
    /// <summary>
    /// 게임 진행도 업데이트
    /// </summary>
    private void UpdateGameProgress()
    {
        // 게임 진행 시간
        if (gameTimeText != null)
        {
            float gameTime = Time.timeSinceLevelLoad;
            int minutes = Mathf.FloorToInt(gameTime / 60);
            int seconds = Mathf.FloorToInt(gameTime % 60);
            
            gameTimeText.text = $"플레이 시간: {minutes:00}:{seconds:00}";
        }
        
        // 기타 게임 진행도 정보
        if (gameProgressText != null)
        {
            string progress = "";
            
            // GameManager가 있다면 추가 정보 표시
            if (gameManager != null)
            {
                // GameManager에 적 처치 수나 점수 등의 정보가 있다면 여기에 추가
                progress += "게임 통계:\\n";
                progress += "- 현재 웨이브: 진행 중\\n";
                progress += "- 적 처치수: 집계 중\\n";
                progress += "- 획득 점수: 집계 중\\n";
            }
            else
            {
                progress = "게임 진행 정보를 가져오는 중...";
            }
            
            gameProgressText.text = progress;
        }
    }
    
    /// <summary>
    /// UI 텍스트 컴포넌트들이 제대로 설정되었는지 확인
    /// </summary>
    void Start()
    {
        ValidateUIComponents();
    }
    
    /// <summary>
    /// UI 컴포넌트 유효성 검사
    /// </summary>
    private void ValidateUIComponents()
    {
        if (playerHealthText == null)
            Debug.LogWarning("StatusDisplayManager: playerHealthText가 설정되지 않았습니다!");
            
        if (playerStatsText == null)
            Debug.LogWarning("StatusDisplayManager: playerStatsText가 설정되지 않았습니다!");
            
        if (weaponListText == null)
            Debug.LogWarning("StatusDisplayManager: weaponListText가 설정되지 않았습니다!");
            
        if (weaponStatsText == null)
            Debug.LogWarning("StatusDisplayManager: weaponStatsText가 설정되지 않았습니다!");
            
        if (gameProgressText == null)
            Debug.LogWarning("StatusDisplayManager: gameProgressText가 설정되지 않았습니다!");
            
        if (gameTimeText == null)
            Debug.LogWarning("StatusDisplayManager: gameTimeText가 설정되지 않았습니다!");
    }
}