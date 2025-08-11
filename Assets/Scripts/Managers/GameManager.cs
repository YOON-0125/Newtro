using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전반을 관리하는 메인 매니저
/// </summary>
public class GameManager : MonoBehaviour
    {
        [Header("게임 설정")]
        [SerializeField] private float gameDuration = 1800f; // 30분 (1800초)
        [SerializeField] private bool pauseOnStart = false;
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 60f;
        
        [Header("플레이어 설정")]
        [SerializeField] private Transform player;
        [SerializeField] private int playerLevel = 1;
        [SerializeField] private int playerExperience = 0;
        [SerializeField] private int expToNextLevel = 100;
        [SerializeField] private float expMultiplier = 1.2f;
        
        [Header("게임 상태")]
        [SerializeField] private GameState currentState = GameState.Menu;
        [SerializeField] private float gameTime = 0f;
        [SerializeField] private int score = 0;
        [SerializeField] private int enemiesKilled = 0;
        
        // 이벤트
        [System.Serializable]
        public class GameManagerEvents
        {
            public UnityEvent OnGameStart;
            public UnityEvent OnGamePause;
            public UnityEvent OnGameResume;
            public UnityEvent OnGameWin;
            public UnityEvent OnGameLose;
            public UnityEvent<int> OnLevelUp;
            public UnityEvent<int> OnExperienceGained;
            public UnityEvent<int> OnScoreChanged;
            public UnityEvent<float> OnTimeUpdate;
        }
        
        [Header("이벤트")]
        [SerializeField] private GameManagerEvents events;
        
        // 외부 접근용 프로퍼티
        public GameManagerEvents Events => events;
        
        // 게임 상태 열거형
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver,
            Victory
        }
        
        // 싱글톤
        public static GameManager Instance { get; private set; }
        
        // 다른 매니저들
        private EnemyManager enemyManager;
        private WeaponManager weaponManager;
        private PlayerHealth playerHealth;
        private RelicManager relicManager;
        private UpgradeSystem upgradeSystem;
        private SimpleObjectPool objectPool;
        
        // 내부 변수
        private bool isGameRunning = false;
        private float lastAutoSaveTime;
        private int baseExpRequirement = 100;
        
        // 프로퍼티
        public GameState CurrentState => currentState;
        public float GameTime => gameTime;
        public float RemainingTime => Mathf.Max(0, gameDuration - gameTime);
        public float GameDuration => gameDuration;
        public int PlayerLevel => playerLevel;
        public int PlayerExperience => playerExperience;
        public int ExpToNextLevel => expToNextLevel;
        public int Score => score;
        public int EnemiesKilled => enemiesKilled;
        public bool IsGameRunning => isGameRunning;
        public bool IsPaused => currentState == GameState.Paused;
        public Transform Player => player;
        public RelicManager RelicManager => relicManager;
        public UpgradeSystem UpgradeSystem => upgradeSystem;
        public WeaponManager WeaponManager => weaponManager;
        public SimpleObjectPool ObjectPool => objectPool;
        
        private void Awake()
        {
            // 싱글톤 설정
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad는 씬 전환이 필요한 경우에만 사용
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeManagers();
        }
        
        private void Start()
        {
            if (pauseOnStart)
            {
                SetGameState(GameState.Paused);
            }
            else
            {
                StartGame();
            }
        }
        
        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                UpdateGameTime();
                CheckWinCondition();
                HandleAutoSave();
            }
            
            HandleInput();
        }
        
        /// <summary>
        /// 매니저들 초기화
        /// </summary>
        private void InitializeManagers()
        {
            // 플레이어 찾기
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
            
            // 다른 매니저들 찾기
            enemyManager = FindObjectOfType<EnemyManager>();
            weaponManager = FindObjectOfType<WeaponManager>();
            relicManager = FindObjectOfType<RelicManager>();
            upgradeSystem = FindObjectOfType<UpgradeSystem>();
            
            // ObjectPool 초기화 (없으면 생성)
            objectPool = SimpleObjectPool.Instance;
            if (objectPool == null)
            {
                GameObject poolObj = new GameObject("SimpleObjectPool");
                objectPool = poolObj.AddComponent<SimpleObjectPool>();
                Debug.Log("[GameManager] SimpleObjectPool 자동 생성 완료");
            }
            
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
            
            // 디버그 로그
            Debug.Log($"[GameManager] 매니저 초기화 완료 - RelicManager: {relicManager != null}, UpgradeSystem: {upgradeSystem != null}, ObjectPool: {objectPool != null}");
        }
        
        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            SetGameState(GameState.Playing);
            isGameRunning = true;
            gameTime = 0f;
            
            // 초기 설정
            ResetGameStats();
            
            events?.OnGameStart?.Invoke();
            
            Debug.Log("게임이 시작되었습니다!");
        }
        
        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
                Time.timeScale = 0f;
                events?.OnGamePause?.Invoke();
            }
        }
        
        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
                Time.timeScale = 1f;
                events?.OnGameResume?.Invoke();
            }
        }
        
        /// <summary>
        /// 게임 승리
        /// </summary>
        public void WinGame()
        {
            SetGameState(GameState.Victory);
            isGameRunning = false;
            
            // 승리 보너스 점수
            int timeBonus = Mathf.RoundToInt(RemainingTime * 10);
            AddScore(timeBonus);
            
            events?.OnGameWin?.Invoke();
            
            Debug.Log("게임에서 승리했습니다!");
        }
        
        /// <summary>
        /// 게임 패배
        /// </summary>
        public void LoseGame()
        {
            SetGameState(GameState.GameOver);
            isGameRunning = false;
            
            events?.OnGameLose?.Invoke();
            
            Debug.Log("게임에서 패배했습니다!");
        }
        
        /// <summary>
        /// 플레이어 사망 처리
        /// </summary>
        public void OnPlayerDied()
        {
            LoseGame();
        }
        
        /// <summary>
        /// 게임 시간 업데이트
        /// </summary>
        private void UpdateGameTime()
        {
            gameTime += Time.deltaTime;
            events?.OnTimeUpdate?.Invoke(gameTime);
        }
        
        /// <summary>
        /// 승리 조건 체크
        /// </summary>
        private void CheckWinCondition()
        {
            if (gameTime >= gameDuration)
            {
                WinGame();
            }
        }
        
        /// <summary>
        /// 경험치 추가
        /// </summary>
        public void AddExperience(int amount)
        {
            playerExperience += amount;
            events?.OnExperienceGained?.Invoke(amount);
            
            // 레벨업 체크
            while (playerExperience >= expToNextLevel)
            {
                LevelUp();
            }
        }
        
        /// <summary>
        /// 레벨업
        /// </summary>
        private void LevelUp()
        {
            playerExperience -= expToNextLevel;
            playerLevel++;
            
            // 다음 레벨 경험치 요구량 증가
            expToNextLevel = Mathf.RoundToInt(baseExpRequirement * Mathf.Pow(expMultiplier, playerLevel - 1));
            
            events?.OnLevelUp?.Invoke(playerLevel);
            
            Debug.Log($"레벨업! 현재 레벨: {playerLevel}");
            
            // 업그레이드 옵션 생성 및 UI 표시
            ShowLevelUpUI();
        }
        
        /// <summary>
        /// 업그레이드 적용 (UI에서 호출)
        /// </summary>
        public void ApplyUpgrade(string upgradeId)
        {
            if (upgradeSystem != null)
            {
                bool success = upgradeSystem.ApplyUpgrade(upgradeId);
                Debug.Log($"[GameManager] 업그레이드 적용: {upgradeId} - {(success ? "성공" : "실패")}");
            }
            else
            {
                Debug.LogError("[GameManager] UpgradeSystem이 초기화되지 않았습니다!");
            }
        }
        
        /// <summary>
        /// 무기 추가 (업그레이드에서 호출)
        /// </summary>
        public bool AddWeapon(string weaponName)
        {
            if (weaponManager != null)
            {
                bool success = weaponManager.AddWeapon(weaponName);
                Debug.Log($"[GameManager] 무기 추가: {weaponName} - {(success ? "성공" : "실패")}");
                return success;
            }
            else
            {
                Debug.LogError("[GameManager] WeaponManager가 초기화되지 않았습니다!");
                return false;
            }
        }
        
        /// <summary>
        /// 무기 레벨업 (업그레이드에서 호출)
        /// </summary>
        public bool LevelUpWeapon(string weaponName)
        {
            if (weaponManager != null)
            {
                bool success = weaponManager.LevelUpWeapon(weaponName);
                Debug.Log($"[GameManager] 무기 레벨업: {weaponName} - {(success ? "성공" : "실패")}");
                return success;
            }
            else
            {
                Debug.LogError("[GameManager] WeaponManager가 초기화되지 않았습니다!");
                return false;
            }
        }
        
        /// <summary>
        /// 업그레이드 옵션 가져오기 (UI에서 호출)
        /// </summary>
        public List<UpgradeOption> GetUpgradeOptions()
        {
            if (upgradeSystem != null)
            {
                return upgradeSystem.GenerateUpgradeOptions(playerLevel);
            }
            return new List<UpgradeOption>();
        }
        
        /// <summary>
        /// 레벨업 UI 표시
        /// </summary>
        private void ShowLevelUpUI()
        {
            LevelUpManager levelUpManager = FindObjectOfType<LevelUpManager>();
            if (levelUpManager != null)
            {
                levelUpManager.StartLevelUp(playerLevel);
            }
            else
            {
                Debug.LogWarning("GameManager: LevelUpManager를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 점수 추가
        /// </summary>
        public void AddScore(int points)
        {
            score += points;
            events?.OnScoreChanged?.Invoke(score);
        }
        
        /// <summary>
        /// 적 처치 수 증가
        /// </summary>
        public void AddEnemyKill()
        {
            enemiesKilled++;
            AddScore(10); // 적 처치당 10점
            // 경험치는 EXP 오브를 통해서만 획득
        }
        
        /// <summary>
        /// 게임 상태 설정
        /// </summary>
        private void SetGameState(GameState newState)
        {
            currentState = newState;
        }
        
        /// <summary>
        /// 게임 통계 리셋
        /// </summary>
        private void ResetGameStats()
        {
            playerLevel = 1;
            playerExperience = 0;
            expToNextLevel = baseExpRequirement;
            score = 0;
            enemiesKilled = 0;
        }
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            // ESC 키로 일시정지/재개
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }
        
        /// <summary>
        /// 자동 저장 처리
        /// </summary>
        private void HandleAutoSave()
        {
            if (autoSave && Time.time >= lastAutoSaveTime + autoSaveInterval)
            {
                SaveGame();
                lastAutoSaveTime = Time.time;
            }
        }
        
        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame()
        {
            // PlayerPrefs를 사용한 간단한 저장
            PlayerPrefs.SetInt("PlayerLevel", playerLevel);
            PlayerPrefs.SetInt("PlayerExperience", playerExperience);
            PlayerPrefs.SetInt("Score", score);
            PlayerPrefs.SetFloat("GameTime", gameTime);
            PlayerPrefs.SetInt("EnemiesKilled", enemiesKilled);
            
            PlayerPrefs.Save();
            Debug.Log("게임이 저장되었습니다.");
        }
        
        /// <summary>
        /// 게임 불러오기
        /// </summary>
        public void LoadGame()
        {
            if (PlayerPrefs.HasKey("PlayerLevel"))
            {
                playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
                playerExperience = PlayerPrefs.GetInt("PlayerExperience", 0);
                score = PlayerPrefs.GetInt("Score", 0);
                gameTime = PlayerPrefs.GetFloat("GameTime", 0f);
                enemiesKilled = PlayerPrefs.GetInt("EnemiesKilled", 0);
                
                // 다음 레벨 경험치 계산
                expToNextLevel = Mathf.RoundToInt(baseExpRequirement * Mathf.Pow(expMultiplier, playerLevel - 1));
                
                Debug.Log("게임이 불러와졌습니다.");
            }
        }
        
        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        /// <summary>
        /// 메인 메뉴로 이동
        /// </summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            // 메인 메뉴 씬으로 이동 (씬 이름에 따라 수정 필요)
            SceneManager.LoadScene("MainMenu");
        }
        
        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            SaveGame();
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        /// <summary>
        /// 현재 게임 정보 반환
        /// </summary>
        public string GetGameInfo()
        {
            return $"게임 정보\n" +
                   $"상태: {currentState}\n" +
                   $"시간: {FormatTime(gameTime)} / {FormatTime(gameDuration)}\n" +
                   $"레벨: {playerLevel} (EXP: {playerExperience}/{expToNextLevel})\n" +
                   $"점수: {score:N0}\n" +
                   $"처치한 적: {enemiesKilled}";
        }
        
        /// <summary>
        /// 시간 포맷팅 (MM:SS)
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            return $"{minutes:D2}:{seconds:D2}";
        }
        
        /// <summary>
        /// 게임 속도 설정 (디버그용)
        /// </summary>
        public void SetGameSpeed(float speed)
        {
            Time.timeScale = speed;
        }
        
        private void OnDestroy()
        {
            Debug.LogError("[GameManager] OnDestroy()가 호출되었습니다! GameManager가 파괴됩니다.");
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentState == GameState.Playing)
            {
                PauseGame();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && currentState == GameState.Playing)
            {
                PauseGame();
            }
        }
    }