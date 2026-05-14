using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;


/// <summary>
/// Centralized Game Manager — controls scene flow, pause state, saving, and global references.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Fields
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    [Header("Runtime State")]
    [SerializeField] private string _currentScene;
    [SerializeField] private bool _isPaused;
    [SerializeField] private int _score;
    private float _playTime;
    private float _startPosX;
    private int _lastDistanceScore;
    
    // 🔥 Session-specific tracking (resets each run)
    private int _sessionScore;
    private int _sessionCoins;
    public int SessionScore => _sessionScore;
    public int SessionCoins => _sessionCoins;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private SaveSystem _saveSystem;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private CardManager _cardManager;
    public CardManager CardManager => _cardManager;

    [SerializeField] private DevCheat _devCheat;
    #endregion

    [Header("Optimization Settings")]
    [SerializeField] private int _targetFrameRate = 90;

    #region Properties
    public static event System.Action OnCurrencyReady;
    public bool IsPaused => _isPaused;
    private bool _isGameOver = false;
    private bool _isSelectingStarterCard = false;
    public int Score => _score;
    private ScoreUI _scoreUI;
    public static event System.Action OnGameReset;

    public float PlayTime => _playTime;
    private GameProgressData _persistentProgress;
    private Currency _currencyData;
    private int _coins;

    private StoreManager _storeManager;
    [SerializeField] private StoreExchange _exchangeStore;
    [SerializeField] private StoreUpgrade _upgradeStore;
    [SerializeField] private StoreMap _mapStore;
    #endregion

    public MapType CurrentMapType;
    public GameProgressData GetProgressData() => _persistentProgress;
    public Currency GetCurrency() => _currencyData;
    public StoreManager GetStoreManager() => _storeManager; 
    public List<StoreBase> GetStoreList() => _storeManager?.Stores;

    private bool _storesInitialized = false;

    public static event Action OnGameReady;



    #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // WebGL Optimization: Force specific framerate
            Application.targetFrameRate = _targetFrameRate;
            
            // Pre-cache global UI if available
            if (_uiManager == null) _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void Start()
        {
            // ตรวจสอบ Flow การเริ่มเกม
            //if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
            //{
            //    Debug.LogWarning("[GameManager] Not in MainMenu. Reloading to ensure correct flow.");
            //    LoadScene("MainMenu");
            //    return;
            //}

            InitializeGame();
            
        }

        private void Update()
        {
            if (_currentScene == "MainMenu" || _player == null)
                return;

            if (_isPaused || _isGameOver || _isSelectingStarterCard)
                return;

            _playTime += Time.deltaTime;

            // 🔥 NEW: Distance-based scoring
            UpdateDistanceScore();
        }

        private void UpdateDistanceScore()
        {
            if (_player == null) return;

            var players = PlayerManager.Instance != null ? PlayerManager.Instance.GetAllPlayers() : null;
            bool isCompetition = GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Competition;

            if (isCompetition && players != null)
            {
                // 🔥 In Competition: Track each player individually
                foreach (var p in players)
                {
                    if (p == null || p.Data == null) continue;
                    
                    float dist = p.transform.position.x - _startPosX; // Assuming same start for both
                    int scoreAtDist = Mathf.Max(0, Mathf.FloorToInt(dist * 10));
                    
                    // We need a way to track last distance per player
                    // For simplicity, let's use the delta compared to their current data score
                    // But better to check if it's purely distance. 
                    // Let's use a simple per-player check.
                    if (scoreAtDist > p.LastDistanceScore)
                    {
                        int inc = scoreAtDist - p.LastDistanceScore;
                        p.AddScore(inc); 
                        p.LastDistanceScore = scoreAtDist;
                    }
                }
            }
            else
            {
                // In Solo/Coop: Track the furthest player for shared score
                float currentX = _player.transform.position.x;
                if (players != null && players.Count > 0)
                {
                    currentX = players.Max(p => p.transform.position.x);
                }

                float distance = currentX - _startPosX;
                int distanceScore = Mathf.Max(0, Mathf.FloorToInt(distance * 10));

                if (distanceScore > _lastDistanceScore)
                {
                    int increment = distanceScore - _lastDistanceScore;
                    AddScore(increment);
                    _lastDistanceScore = distanceScore;
                }
            }
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion


#region Scene Management & Flow

    /// <summary>
    /// ใช้สำหรับปุ่มใน Main Menu เพื่อเลือกด่านและเริ่มเกม
    /// </summary>
    public void LoadGameLevel(string sceneName, MapType mapType)
    {
        CurrentMapType = mapType;
        _score = 0;
        _playTime = 0f;
        _isPaused = false;
        _isGameOver = false;
        Time.timeScale = 1f;

        Debug.Log($"[GameManager] Loading Map: {sceneName} with Type: {mapType}");
        LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        _currentScene = sceneName;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene Loaded: {scene.name}");

        // Auto detect map type
        switch (scene.name)
        {
            case "MainMenu":
                CurrentMapType = MapType.None;
                break;

            case "MapSchool":
                CurrentMapType = MapType.School;
                break;

            case "MapRoadTraffic":
                CurrentMapType = MapType.RoadTraffic;
                break;

            case "MapKitchen":
                CurrentMapType = MapType.Kitchen;
                break;

            default:
                CurrentMapType = MapType.None;
                break;
        }

        Debug.Log($"[GameManager] Current Map = {CurrentMapType}");

        var sceneManager = FindFirstObjectByType<SceneManager>();
        var mapGenerator = FindFirstObjectByType<MapGeneratorBase>();
        var player = FindFirstObjectByType<Player>();

        if (sceneManager == null || mapGenerator == null || player == null || UIManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] Waiting next frame — scene dependencies not ready yet.");
            StartCoroutine(DelayedSceneInit(scene.name));
            return;
        }

        if (scene.name == "MainMenu")
        {
            StartCoroutine(WaitMainMenuUIBindThenShow());
            return;
        }
        HandleHUDVisibility(scene.name);

    }

    private IEnumerator WaitMainMenuUIBindThenShow()
    {
        // รอให้ UIManager spawn แล้ว
        while (UIManager.Instance == null) yield return null;

        // รออีก 1–2 เฟรมให้ Canvas กับ Panels ถูก Instantiate
        yield return null;
        yield return null;

        // AutoBind ก่อน
        UIManager.Instance.AutoBindMainMenuUI();

        // Inject dependencies (สำคัญ!!)
        UIManager.Instance.SetDependencies(this, _currencyData, _storeManager, GetStoreList());

        // เปิดเมนู
        UIManager.Instance.ShowMainMenu();
    }

    #endregion

#region Coroutine GM
/// <summary>
/// Coroutine รอให้ Dependencies (Player, Map, UI) พร้อมทำงานก่อนเริ่มเกม
/// </summary>
    private IEnumerator DelayedSceneInit(string sceneName)
    {
        // 1. Clear Cache
        _player = null;
        _uiManager = null;
        SceneManager sceneLogic = null;
        MapGeneratorBase mapGen = null;

        // 2. Wait dependencies
        while (_player == null || _uiManager == null || sceneLogic == null || mapGen == null)
        {
            if (_player == null) _player = FindFirstObjectByType<Player>();
            if (_uiManager == null) _uiManager = FindFirstObjectByType<UIManager>();
            if (sceneLogic == null) sceneLogic = FindFirstObjectByType<SceneManager>();
            if (mapGen == null) mapGen = FindFirstObjectByType<MapGeneratorBase>();
            yield return null;
        }

        // 3. Bind UI Dependencies
        _uiManager.SetDependencies(this, _currencyData, _storeManager, GetStoreList());
        if (!_player.HasHealthBar)
            _player.SetHealthBarUI(_uiManager.GetPlayerHealthBarUI());

        // 4. Score system + UI
        _scoreUI = _uiManager.GetScoreUI();   // ดึง ScoreUI ตัวใหม่ทุกครั้ง
        _score = 0;                           // reset ตัวแปร score
        _playTime = 0f;                       // reset ตัวแปรเวลา
        _startPosX = _player != null ? _player.transform.position.x : 0f;
        _lastDistanceScore = 0;
            if (_scoreUI != null)
        {
            _scoreUI.DisplaySavedHighScore(_persistentProgress.BestScore);
            _scoreUI.InitializeScore(0);
            _scoreUI.UpdateCoins(0);
            _uiManager?.UpdateScore(0);
            //_scoreUI.UpdateScore(0);  
            Debug.Log($"[GM] ScoreUI forced refresh — Score=0");
            // Pass baseline (saved) total coins so ScoreUI will show only session-collected coins
            // If a PlayerManager exists, let it bind ScoreUI per-player to avoid single-player-only binding
            if (PlayerManager.Instance == null)
            {
                if (_player != null)
                {
                    _player.HookScoreUI(_scoreUI, _currencyData != null ? _currencyData.Coin : 0);
                    Debug.Log("[GM] ScoreUI initialized and linked (single-player fallback).");
                }
            }
            else
            {
                Debug.Log("[GM] PlayerManager present — skipping direct ScoreUI hook. PlayerManager will bind per-player UI.");
            }
        }

        // 5. Scene inject to Map + Player system
        sceneLogic.TryInitializeScene();

        // 6. Career + CardManager Dependencies
        var cardManager = FindFirstObjectByType<CardManager>();
        var careerSwitcher = FindFirstObjectByType<CareerSwitcher>();
        FindFirstObjectByType<CardSlotUI>()?.SetManager(cardManager);
        if (cardManager != null && careerSwitcher != null)
            cardManager.SetCareerSwitcher(careerSwitcher);

        // 7. StarterCard dependency
        var randomStarter = FindFirstObjectByType<RandomStarterCard>();
        randomStarter?.SetDependencies(cardManager, this);

        // 8. Player full Initialize
        PlayerData data = new PlayerData(_currencyData, _persistentProgress);
        int hpBonus = _upgradeStore != null ? _upgradeStore.GetTotalHPBonus() : 0;
        data.UpgradeStat("MaxHealth", hpBonus);
        _player.Initialize(data, cardManager, careerSwitcher);

        Debug.Log($"[GM DEBUG] HP Upgrade Bonus: {hpBonus}");
        Debug.Log($"[GM DEBUG] PlayerData.MaxHealth (after upgrade stat): {data.MaxHealth}");  
      
        if (_player != null && _uiManager != null)
        {
            if (!_player.HasHealthBar)
                _player.SetHealthBarUI(_uiManager.GetPlayerHealthBarUI());
            Debug.Log($"[GM DEBUG] Player's Max Health AFTER Initialize (Final Value): {_player.MaxHealth}");
        }


        // 9. Comic FX pools
        var fxManager = FindFirstObjectByType<ComicEffectManager>();
        fxManager?.Initialize(_player);

        // 🔥 10. Notify gameplay systems READY (DevCheat, Input, Card, Attack, Interact)
        OnGameReady?.Invoke();

        // 11. Optional systems
        FindFirstObjectByType<BuffManager>()?.Initialize(this);

        var throwableSpawner = FindFirstObjectByType<ThrowableSpawner>();
        if (throwableSpawner != null && _player != null)
        {
            // Ensure spawner is initialized (pool created) before spawning
            if (!throwableSpawner.CanInteract) 
            {
                var enemySpawner = FindFirstObjectByType<EnemySpawner>();
                throwableSpawner.Initialize(_player.transform, enemySpawner);
            }

            Vector3 pos = _player.transform.position;
            pos.x += 1.8f;
            throwableSpawner.SpawnAtPosition(pos);
            Debug.Log("[GM] 🔥 TEST THROWABLE SPAWNED AT START");
        }

        Debug.Log("[GameManager] All Systems Ready");

        // 12. Starter card initial selection
        randomStarter?.ResetForNewGame();
        _uiManager.ShowGameplayHUD();
        _uiManager.ShowCardSelectionPanel(true);
        randomStarter?.OpenPanel();

        // 13. Freeze gameplay until player selects starter card
        Time.timeScale = 0f;
    }


    public Player PlayerRef => _player;

    private void HandleHUDVisibility(string sceneName)
    {
        if (_uiManager == null) return;

        if (sceneName == "MainMenu")
        {
            _uiManager.ShowMainMenu();
        }
        else
        {
            _uiManager.ShowGameplayHUD();
            
        }
    }
    #endregion


    #region Initialization
public void InitializeGame()
{
    

    Debug.Log(">> OPEN MAIN MENU");
    _persistentProgress = _saveSystem != null ? _saveSystem.GetProgressData() : new GameProgressData();

    _currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

    SetupStores(_persistentProgress); 
    OnCurrencyReady?.Invoke();

    // ==========================================================
    //TODO : ลบส่วนนี้และไฟล์ Devcheat.CS ในภายหลังจากเทสเสร็จสิ้น
    // ✅ FIX 2: INJECT DEPENDENCIES INTO DEVCHEAT (ใช้ FindFirstObjectByType ภายใน GameManager)
    // สำหรับ DevCheat เท่านั้น เนื่องจากเป็นเครื่องมือชั่วคราว
    // ==========================================================
    _devCheat ??= FindFirstObjectByType<DevCheat>(); 
    if (_devCheat != null)
    {

        var mapSelect = FindFirstObjectByType<MapSelectController>();
        var storeUI = FindFirstObjectByType<StoreUI>();

        _devCheat.InitializeCheat(this, _player, _currencyData, mapSelect, storeUI, _uiManager );
    }
    // ==========================================================


    if (_uiManager != null)
    {
        // UIManager.cs ต้องมีเมธอด SetDependencies(GameManager, Currency, StoreManager, List<StoreBase>)
        _uiManager.SetDependencies(this, _currencyData, _storeManager, GetStoreList());
    }

    var mapSelectController = FindFirstObjectByType<MapSelectController>();
    if (mapSelectController != null)
    {
        mapSelectController.SetDependencies(this, _currencyData);
    }

    // Inject ตัวเองเข้า BuffManager ก่อน
    BuffManager buffManager = FindFirstObjectByType<BuffManager>(); // TEMP FIND FOR SINGLETON
    if (buffManager != null)
    {
        // BuffManager.cs ต้องมี Initialize(GameManager gm)
        buffManager.Initialize(this); 
    }

    if (_currentScene == "MainMenu")
    {
        Debug.Log("[GameManager] Main Menu scene detected → skipping gameplay initialization.");
        OnCurrencyReady?.Invoke();
        return;
    }

    
    PlayerData playerData = new PlayerData(_currencyData, _persistentProgress);
    int hpBonus = _upgradeStore != null ? _upgradeStore.GetTotalHPBonus() : 0;
    playerData.UpgradeStat("MaxHealth", hpBonus);


    _isPaused = false;
    _score = 0;
    _playTime = 0f;
}
    #endregion


    #region Game Flow
    public void SetGameOver()
    {
        _isPaused = true;
        _isGameOver = true;
    }

    public void StartGame()
    {
        _isPaused = false;
        _playTime = 0f;
        _score = 0;
        Time.timeScale = 1f;
        _uiManager?.panelHUDMain.SetActive(true);
    }

    public void TogglePause()
    {
        Debug.Log($"[GameManager] TogglePause called. Current State: _isPaused={_isPaused}, TimeScale={Time.timeScale}");

        if (_isPaused) 
        {
            ResumeGame();
        }
        else 
        {
            // Only block pausing if a "hard" menu (Store/Settings) is open
            if (_uiManager != null && _uiManager.IsAnyMenuOpen())
            {
                Debug.Log("[GameManager] Pause blocked because a Persistent Menu (Store/Settings) is open.");
                return;
            }
            PauseGame();
        }
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        _uiManager?.ShowPauseMenu(true);
         Debug.Log("[GameManager] Game Paused.");
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        _uiManager?.ShowPauseMenu(false);
        Debug.Log("[GameManager] Game Resumed.");
    }


    /// <summary>
    /// NEW: เมธอดสำหรับปุ่ม Resume ใน Pause Menu
    /// </summary>
    public void ResumeGameFromPauseButton()
    {
        ResumeGame();
    }
    
    /// <summary>
    /// NEW: เมธอดสำหรับปุ่ม Restart ใน Pause Menu (สั่ง Player Die)
    /// </summary>
    public void RestartGameFromPause()
    {
        Debug.Log("[GameManager] Restart via Pause Menu → Forcing Player Die.");
        PlayerDieHandler(); // สั่งให้ Game Over ทันที
    }

    public void EndGame()
    {
        // EndGame ถูกเรียกเมื่อ Player ตาย (จาก Player.cs/Wall_Kill.cs)
        // FIX: โยนไปให้ PlayerDieHandler จัดการ UI/Save/TimeScale
        PlayerDieHandler();
    }

    /// <summary>
    /// NEW: เมธอดจัดการ Game Over (Player Die) และเปิด Result Panel
    /// </summary>
    public void PlayerDieHandler()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        SaveProgress();

        // ไม่ต้องส่งคะแนน ให้ UIManager ใช้ ScoreUI จาก prefab เอง
        _uiManager.ShowResultMenu();
    }


    /// <summary>
    /// NEW: เมธอดสำหรับปุ่ม 'ตกลง' จาก Panel_Result (กลับ MainMenu)
    /// </summary>
    public void ExitToMainMenuFromResults()
    {
        Debug.Log("[GameManager] Exiting to Main Menu from Result Screen.");
        _uiManager?.ShowMainMenu(); // ปิด Panel Result และ Menu อื่นๆ
        Time.timeScale = 1f; // คืนค่าเวลา
        _isPaused = false;   
        LoadScene("MainMenu"); 
        
        //Reset Card Summon
        OnGameReset?.Invoke();
    }


    public void RestartGame()
    {
        Debug.Log("[GameManager] Reloading current Scene...");
        Time.timeScale = 1f;
        _score = 0;
        _playTime = 0f;
        _isPaused = false;
        _isGameOver = false;
        LoadScene(_currentScene);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("[GameManager] Quit application.");
    }
    #endregion

    #region Score & Progress
    public void AddScore(int amount)
    {
        if (amount <= 0) return;

        _score += amount;
        _sessionScore += amount; // Track session-specific score

        bool isCompetition = GameModeManager.Instance != null && GameModeManager.Instance.CurrentMode == GameModeManager.GameMode.Competition;

        // 1. Update central UI (for Solo or general display)
        if (GameModeManager.Instance == null || GameModeManager.Instance.PlayerCount == 1)
            _uiManager?.UpdateScore(_sessionScore); // Show session score on UI

        // 2. Broadcast to all players' bound UIs only in Solo/Coop
        if (!isCompetition && PlayerManager.Instance != null)
        {
            var players = PlayerManager.Instance.GetAllPlayers();
            foreach (var p in players)
            {
                if (p != null) p.UpdateBoundScoreUI(_sessionScore);
            }
        }

        Debug.Log($"[GM] Score Added: {amount} | Session: {_sessionScore} | Total (Saved): {_score} | Mode: {(isCompetition ? "Competition" : "Shared")}");
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        _sessionCoins += amount; // Track session-specific coins
        
        if (_currencyData != null)
            _currencyData.Coin += amount; // Add to global currency

        // Only update central coin UI in single-player mode
        if (GameModeManager.Instance == null || GameModeManager.Instance.PlayerCount == 1)
            _uiManager?.GetScoreUI()?.UpdateCoins(_sessionCoins);

        Debug.Log($"[GameManager] Coins +{amount} (Session: {_sessionCoins})");
    }

    public PlayerData GetPlayer1Data()
    {
        PlayerData data = new PlayerData(_currencyData, _persistentProgress);
        int hpBonus = _upgradeStore != null ? _upgradeStore.GetTotalHPBonus() : 0;
        data.UpgradeStat("MaxHealth", hpBonus);
        return data;
    }

    public PlayerData GetPlayer2Data()
    {
        // For now, P2 uses the same base data (shared currency/progress in non-persistent modes)
        // Or you could create a fresh one if you want P2 to always start fresh
        PlayerData data = new PlayerData(new Currency(), new GameProgressData());
        // Still give them the HP bonus if applicable
        int hpBonus = _upgradeStore != null ? _upgradeStore.GetTotalHPBonus() : 0;
        data.UpgradeStat("MaxHealth", hpBonus);
        return data;
    }

    public float GetPlayTime() => _playTime;

    public void SaveProgress() 
    {
        if (_saveSystem == null)
        {
            Debug.LogWarning("[GameManager] SaveSystem not found!");
            return;
        }

        _persistentProgress.UpdateBestScore(_score);
        _persistentProgress.PlayTime += _playTime;
        
        _persistentProgress.TotalCoins = _currencyData.Coin;
        _persistentProgress.TotalTokens = _currencyData.Token;
        _persistentProgress.TotalKeyMaps = _currencyData.KeyMap;
        
        _saveSystem.SaveData();
        Debug.Log("[GameManager] Game progress saved.");
    }
    #endregion

#region  store
    public void SetupStores(GameProgressData progressData)
    {
        if (_storesInitialized)
        return; 

        _currencyData = new Currency
        {
            Coin = progressData.TotalCoins,
            Token = progressData.TotalTokens,
            KeyMap = progressData.TotalKeyMaps
        };

        _storeManager = new StoreManager(_currencyData, progressData);

        // --- ให้ StoreManager เป็นคน Initialize + Inject items ---
        if (_exchangeStore != null)
            _storeManager.RegisterStore(_exchangeStore);
        
        if (_upgradeStore != null)
            _storeManager.RegisterStore(_upgradeStore);
        
        if (_mapStore != null)
        {
            _storeManager.RegisterStore(_mapStore);
            _mapStore.OnMapUnlockedEvent += HandleMapUnlocked;
        }

        Debug.Log("[GameManager] All store systems initialized successfully.");
        _storesInitialized = true; 
    }

    private void HandleMapUnlocked(string mapName)
    {
        _persistentProgress.AddUnlockedMap(mapName);
        SaveProgress(); 
        Debug.Log($"[GameManager] New map unlocked and saved: {mapName}");
    }
    #endregion


    public void ResetGameProgress()
    {
        if (_saveSystem == null)
        {
            Debug.LogWarning("[GameManager] SaveSystem not found! Cannot reset.");
            return;
        }

        // 1. Reset SaveSystem file to fresh state
        _saveSystem.ResetData(); // reset file

        // 2. Get new empty GameProgressData from SaveSystem
        _persistentProgress = _saveSystem.GetProgressData();
        _persistentProgress.ResetProgress(); // clear values to be 100% safe
        _saveSystem.SaveData(); // save cleared version

        // 3. Reset runtime data
        _currencyData = new Currency();
        _storesInitialized = false;
        _storeManager = null;

        // 4. Reinitialize stores with fresh progress data
        SetupStores(_persistentProgress);

        // 5. Reset Player's cached PlayerData
        if (_player != null)
            _player.ResetPlayerDataCache();

        // 5b. Force Editor refresh so Inspector shows updated runtime values
        #if UNITY_EDITOR
        _player?.ForceEditorRefresh();
        #endif

        // 6. Broadcast reset event for any listeners
        OnGameReset?.Invoke();

        LoadScene("MainMenu");
        Debug.Log("[GameManager] Full game progress reset → stores reinitialized → restarted.");
    }

    public void DeleteSaveAndRestart()
    {
        if (_saveSystem == null)
        {
            Debug.LogWarning("[GameManager] SaveSystem missing — cannot delete save.");
            return;
        }

        // 1. Delete save file and recreate empty version in SaveSystem
        _saveSystem.DeleteSave();

        // 2. Get fresh GameProgressData from SaveSystem
        _persistentProgress = _saveSystem.GetProgressData();

        // 3. Reset runtime data
        _currencyData = new Currency();
        _storesInitialized = false;
        _storeManager = null;

        // 4. Reinitialize stores with fresh progress data
        SetupStores(_persistentProgress);

        // 5. Reset Player's cached PlayerData
        if (_player != null)
            _player.ResetPlayerDataCache();

        // 5b. Force Editor refresh so Inspector shows updated runtime values
        #if UNITY_EDITOR
        _player?.ForceEditorRefresh();
        #endif

        // 6. Broadcast reset event for any listeners
        OnGameReset?.Invoke();

        LoadScene("MainMenu");

        Debug.Log("[GameManager] 🗑 Save deleted → fresh data loaded → stores reinitialized → PlayerData reset → game restarted.");
    }


}

