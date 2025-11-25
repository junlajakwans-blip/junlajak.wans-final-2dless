using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;


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
    [SerializeField] private float _playTime;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private SaveSystem _saveSystem;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private CardManager _cardManager;

    [SerializeField] private DevCheat _devCheat;
    #endregion

    [Header("Optimization Settings")]
    [SerializeField] private int _targetFrameRate = 90;

    #region Properties
    public static event System.Action OnCurrencyReady;
    public bool IsPaused => _isPaused;
    public int Score => _score;
    public float PlayTime => _playTime;
    private GameProgressData _persistentProgress;
    private Currency _currencyData;
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
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
            {
                Debug.LogWarning("[GameManager] Not in MainMenu. Reloading to ensure correct flow.");
                LoadScene("MainMenu");
                return;
            }

            InitializeGame();
        }

        private void Update()
        {
            if (!_isPaused && _currentScene != "MainMenu")
                _playTime += Time.deltaTime;

            // Shortcut Keys
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (_currentScene != "MainMenu") 
                    TogglePause();
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
        CurrentMapType = mapType; // Set map type BEFORE loading scene
        
        // Reset Gameplay Values immediately
        _score = 0;
        _playTime = 0f;
        _isPaused = false;
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

        HandleHUDVisibility(scene.name);

    }
    #endregion


#region Corotine GM
    /// <summary>
    /// Coroutine รอให้ Dependencies (Player, Map, UI) พร้อมทำงานก่อนเริ่มเกม
    /// </summary>
private IEnumerator DelayedSceneInit(string sceneName)
{
    // ⬇ Ensure Currency already initialized
    if (_currencyData == null)
        SetupStores(_persistentProgress);

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

    // 3. UI Dependencies
    _uiManager.SetDependencies(this, _currencyData, _storeManager, GetStoreList());
    _player.SetHealthBarUI(_uiManager.GetPlayerHealthBarUI());

    // 4. Scene Logic injection
    sceneLogic.Inject(mapGen, _player);
    sceneLogic.TryInitializeScene();

    var cardManager = FindFirstObjectByType<CardManager>();
    var careerSwitcher = FindFirstObjectByType<CareerSwitcher>();
    FindFirstObjectByType<CardSlotUI>()?.SetManager(cardManager);

    // 5. StarterCard dependency MUST happen BEFORE UI opens
    var randomStarter = FindFirstObjectByType<RandomStarterCard>();
    if (randomStarter != null)
        randomStarter.SetDependencies(cardManager, this);

    // 6. Player stats
    PlayerData data = new PlayerData(_currencyData, _persistentProgress);
    int hpBonus = _upgradeStore != null ? _upgradeStore.GetTotalHPBonus() : 0;
    data.UpgradeStat("MaxHealth", hpBonus);
    _player.Initialize(data, cardManager, careerSwitcher);

    // 7. Buffs
    FindFirstObjectByType<BuffManager>()?.Initialize(this);

    Debug.Log("[GameManager] All Systems Ready. Firing OnGameReady.");

    // 8. RESET StarterCard — must do BEFORE panel opens
    randomStarter?.ResetForNewGame();

    // 9. HUD & Panel flow
    _uiManager.ShowGameplayHUD();            // เปิด HUD
    _uiManager.ShowCardSelectionPanel(true); // เปิด container ของ Panel
    randomStarter?.OpenPanel();              // เปิด panel สุ่มการ์ดจริง

    // 10. Pause game while selecting
    Time.timeScale = 0f;

    // 11. Trigger Gameplay Ready Event
    OnGameReady?.Invoke();
}


    private IEnumerator ShowStarterPanelNextFrame()
    {
        yield return null; // รอ 1 เฟรมให้ UI และ currency refresh ก่อน
        var starterPanel = FindFirstObjectByType<RandomStarterCard>();
        starterPanel?.ResetForNewGame();
        starterPanel?.OpenPanel();

        UIManager.Instance.ShowCardSelectionPanel(true);
        Time.timeScale = 0f;
    }

    private void HandleMainMenuState()
    {
        if (_uiManager == null) _uiManager = FindFirstObjectByType<UIManager>();
        
        if (_uiManager != null)
        {
            _uiManager.ShowMainMenu();
            _uiManager.SetDependencies(this, _currencyData, _storeManager, GetStoreList());
        }
        
        // Setup Map Selector if exists
        var mapSelect = FindFirstObjectByType<MapSelectController>();
        if (mapSelect != null) mapSelect.SetDependencies(this, _currencyData);

        OnCurrencyReady?.Invoke();
    }

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

    OnGameReady?.Invoke(); 
}
    #endregion


    #region Game Flow
    public void StartGame() //TODO: Implement start game logic
    {
        _isPaused = false;
        _playTime = 0f;
        _score = 0;
        Time.timeScale = 1f;
        _uiManager?.panelHUDMain.SetActive(true);
    }

    public void TogglePause()
    {
        if (IsPaused && _uiManager != null && _uiManager.IsAnyMenuOpen()) 
                {
                    return;
                }

                if (_isPaused) ResumeGame();
                else PauseGame();
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
        Time.timeScale = 0f; // หยุดเกม
        
        SaveProgress(); // บันทึกความคืบหน้า

        int finalScore = _score;
        int finalCoins = _currencyData?.Coin ?? 0; // ต้องมี _currencyData

        Debug.Log($"[GameManager] Game Over. Final Score: {finalScore}");
        
        // เปิด Panel_Result
        _uiManager?.ShowResultMenu(finalScore, finalCoins);
    }
    
    /// <summary>
    /// NEW: เมธอดสำหรับปุ่ม 'ตกลง' จาก Panel_Result (กลับ MainMenu)
    /// </summary>
    public void ExitToMainMenuFromResults()
    {
        Debug.Log("[GameManager] Exiting to Main Menu from Result Screen.");
        _uiManager?.CloseAllMenus(); // ปิด Panel Result และ Menu อื่นๆ
        Time.timeScale = 1f; // คืนค่าเวลา
        LoadScene("MainMenu"); 
    }


    public void RestartGame()
    {
        Debug.Log("[GameManager] Reloading current Scene...");
        Time.timeScale = 1f;
        _score = 0;
        _playTime = 0f;
        _isPaused = false;
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
        _uiManager?.UpdateScore(_score);

        Debug.Log($"[GameManager] Score +{amount} (Total: {_score})");
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

        // สำหรับ Map Store event unlock
        _mapStore.OnMapUnlockedEvent += HandleMapUnlocked;

        Debug.Log("[GameManager] All store systems initialized successfully.");
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
        
        // 1. สั่งให้ SaveSystem ล้างไฟล์เซฟและสร้าง GameProgressData ใหม่
        _saveSystem.ResetData();
        
        // 2. โหลดข้อมูลใหม่ (0) กลับเข้าสู่ GameManager และระบบอื่นๆ
        // (A) อัปเดต _persistentProgress ของ GameManager
        _persistentProgress = _saveSystem.GetProgressData();

        // (B) โหลดเกมใหม่เพื่อเริ่มต้นระบบทั้งหมดด้วยค่าใหม่
        LoadScene("MainMenu");
        
        Debug.Log("[GameManager] Full game progress reset and restarted.");
    }
    
    public void DeleteSaveAndRestart()
    {
        if (_saveSystem == null)
        {
            Debug.LogWarning("[GameManager] SaveSystem missing — cannot delete save.");
            return;
        }

        _saveSystem.DeleteSave();
        LoadScene("MainMenu");
        Debug.Log("[GameManager] Save deleted and game restarted.");
    }

}

