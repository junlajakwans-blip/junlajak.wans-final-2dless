using UnityEngine;
using System;
using System.Collections.Generic;


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
    //[SerializeField] private SceneLoader _sceneLoader; // Handles scene transitions

    #endregion


    #region Properties
    public static event System.Action OnCurrencyReady;

    public static event System.Action OnMainMenuUIReady;
    public static GameManager Instance => _instance;
    public bool IsPaused => _isPaused;
    public int Score => _score;
    public float PlayTime => _playTime;
    public Player Player => _player;
    private GameProgressData _persistentProgress;
    private Currency _currencyData;
    private StoreManager _storeManager;
    private StoreRandomCard _cardStore;
    private StoreUpgrade _upgradeStore;
    private StoreMap _mapStore;
    #endregion

    public GameProgressData GetProgressData() => _persistentProgress;
    public Currency GetCurrency() => _currencyData;
    public StoreManager GetStoreManager() => _storeManager; 
    public List<StoreBase> GetStoreList() => _storeManager?.Stores;

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
    
    _saveSystem ??= FindFirstObjectByType<SaveSystem>();

    Debug.Log(">> OPEN MAIN MENU");
    _persistentProgress = _saveSystem != null ? _saveSystem.GetProgressData() : new GameProgressData();

    _currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;


    if (_currentScene == "MainMenu")
    {
        Debug.Log("[GameManager] Main Menu scene detected → skipping gameplay initialization.");
        SetupStores(_persistentProgress);
        OnCurrencyReady?.Invoke();
        return;
    }

    
    PlayerData playerData = new PlayerData(_currencyData, _persistentProgress);
    int hpBonus = _persistentProgress.PermanentHPUpgradeLevel * 10;
    playerData.UpgradeStat("MaxHealth", hpBonus);


    _player ??= FindFirstObjectByType<Player>();
    if (_player != null)
        _player.Initialize(playerData);

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
        //_sceneLoader?.LoadScene(sceneName);
        _currentScene = sceneName;
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
        _storeManager = new StoreManager(_currencyData, progressData);

        CardManager cardManager = FindFirstObjectByType<CardManager>();
        if (cardManager != null)
            _storeManager.SetCardManager(cardManager);

        _cardStore = new StoreRandomCard();
        _cardStore.Initialize(_storeManager);

        _upgradeStore = new StoreUpgrade();
        _upgradeStore.Initialize(_storeManager);

        _mapStore = new StoreMap();
        _mapStore.Initialize(_storeManager);
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

}
