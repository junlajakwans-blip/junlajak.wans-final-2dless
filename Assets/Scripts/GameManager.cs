using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


/// <summary>
/// Centralized Game Manager — controls scene flow, pause state, saving, and global references.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Fields
    private static GameManager _instance;

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
    //[SerializeField] private SceneLoader _sceneLoader; // Handles scene transitions

    [SerializeField] private DevCheat _devCheat;
    #endregion


    #region Properties
    public static event System.Action OnCurrencyReady;

    public static GameManager Instance => _instance;
    public bool IsPaused => _isPaused;
    public int Score => _score;
    public float PlayTime => _playTime;
    public Player Player => _player;
    private GameProgressData _persistentProgress;
    private Currency _currencyData;
    private StoreManager _storeManager;
    [SerializeField] private StoreExchange _exchangeStore;
    [SerializeField] private StoreUpgrade _upgradeStore;
    [SerializeField] private StoreMap _mapStore;
    private Player _playerInstance;
    #endregion

    public GameProgressData GetProgressData() => _persistentProgress;
    public Currency GetCurrency() => _currencyData;
    public StoreManager GetStoreManager() => _storeManager; 
    public List<StoreBase> GetStoreList() => _storeManager?.Stores;


    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene Loaded: {scene.name}");

        var sceneManager = FindFirstObjectByType<SceneManager>();
        var mapGenerator = FindFirstObjectByType<MapGeneratorBase>();
        var player = FindFirstObjectByType<Player>();

        if (sceneManager != null && mapGenerator != null && player != null)
        {
            sceneManager.Inject(mapGenerator, player);

            PlayerData data = new PlayerData(_currencyData, _persistentProgress);
            int hpBonus = _upgradeStore != null ? _upgradeStore.GetTotalHPBonus() : 0;
            data.UpgradeStat("MaxHealth", hpBonus);
            player.Initialize(data);

            sceneManager.TryInitializeScene();
        }


        // แก้ CardManager Initialize
        _playerInstance = player;
        if (_cardManager != null)
            _cardManager.Initialize(_playerInstance);
            if (_cardManager != null)
            {
                _cardManager.Initialize(_playerInstance);

                // FIX การเชื่อมกลับ
                if (_cardManager.TryGetComponent<CardSlotUI>(out var slotUI))
                {
                    slotUI.SetManager(_cardManager);
                }
                else
                {
                    // ถ้า CardSlotUI อยู่เป็น GameObject แยกใน Canvas
                    var ui = FindFirstObjectByType<CardSlotUI>();
                    if (ui != null)
                        ui.SetManager(_cardManager);
                }
            }

        OnCurrencyReady?.Invoke();
    }


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

    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (!_isPaused)
            _playTime += Time.deltaTime;

        // Pause / Resume (shortcut key)
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
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


    if (_player != null)
        _player.Initialize(playerData);


    // ตรวจสอบว่า CardManager และ Player มีอยู่จริง
        if (_cardManager != null && _playerInstance != null)
        {
            //  ใช้เมธอด Initialize ที่ถูกปรับปรุงแล้ว
            _cardManager.Initialize(_playerInstance); 
        }
        else
        {
            Debug.LogError("[GameManager] CardManager or Player instance is missing! Cannot initialize card system.");
        }

    _isPaused = false;
    _score = 0;
    _playTime = 0f;
}


    #endregion


    #region Game Flow
    public void StartGame() //TODO: Implement start game logic
    {
        //_uiManager?.ShowGameplayUI();
        _isPaused = false;
        _playTime = 0f;
        _score = 0;
        Time.timeScale = 1f;
    }

    public void PauseGame() //TODO: Implement pause logic
    {
        _isPaused = true;
        Time.timeScale = 0f;
        //_uiManager?.ShowPauseMenu();
        Debug.Log("[GameManager] Game paused.");
    }

    public void ResumeGame() //TODO: Implement resume logic
    {
        _isPaused = false;
        Time.timeScale = 1f;
        //_uiManager?.HidePauseMenu();
        Debug.Log("[GameManager] Game resumed.");
    }

    public void EndGame() //TODO: Implement end game logic
    {
        _isPaused = true;
        Time.timeScale = 0f;
        //_uiManager?.ShowGameOverScreen();
        SaveProgress();
        Debug.Log("[GameManager] Game over — progress saved.");
    }

    public void RestartGame()
    {
        _score = 0;
        _playTime = 0f;
        _isPaused = false;
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(_currentScene);
        Debug.Log("[GameManager] Restarted current scene.");
    }

    public void LoadScene(string sceneName) //TODO: Implement scene loading logic
    {
        _currentScene = sceneName;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

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
        _currencyData = new Currency();
        _currencyData.Coin = progressData.TotalCoins;
        _currencyData.Token = progressData.TotalTokens;
        _currencyData.KeyMap = progressData.TotalKeyMaps;

        _storeManager = new StoreManager(_currencyData, progressData);

        // --- ให้ StoreManager เป็นคน Initialize + Inject items ---
        if (_exchangeStore == null) Debug.LogError(" Missing Exchange Store");
        if (_upgradeStore == null) Debug.LogError(" Missing Upgrade Store");
        if (_mapStore == null) Debug.LogError("Missing Map Store");

        _storeManager.RegisterStore(_exchangeStore);
        _storeManager.RegisterStore(_upgradeStore);
        _storeManager.RegisterStore(_mapStore);

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
        RestartGame(); // หรือ LoadScene("MainMenu") ถ้าคุณต้องการกลับไปหน้าเมนูหลัก
        
        Debug.Log("[GameManager] Full game progress reset and restarted.");
    }
}
