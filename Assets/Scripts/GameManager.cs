using UnityEngine;
using System;

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
    public static GameManager Instance => _instance;
    public bool IsPaused => _isPaused;
    public int Score => _score;
    public float PlayTime => _playTime;
    public Player Player => _player;
    #endregion


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
    public void InitializeGame() //TODO: Implement initialization logic
    {
        _saveSystem ??= FindFirstObjectOfType<SaveSystem>();
        _uiManager ??= FindFirstObjectOfType<UIManager>();
        //_sceneLoader ??= FindFirstObjectOfType<SceneLoader>();
        _player ??= FindFirstObjectOfType<Player>();

        _currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        _isPaused = false;
        _score = 0;
        _playTime = 0f;

        Debug.Log($"[GameManager] Initialized. Scene: {_currentScene}");
    }

    private T FindFirstObjectOfType<T>()
    {
        throw new NotImplementedException();
    }

    private T FindfirstObjectOfType<T>()
    {
        throw new NotImplementedException();
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

    public void SaveProgress() //TODO: Implement save system
    {
        if (_saveSystem == null)
        {
            Debug.LogWarning("[GameManager] SaveSystem not found!");
            return;
        }

        //_saveSystem.SaveData(_player, _score, _playTime);
        Debug.Log("[GameManager] Game progress saved.");
    }
    #endregion
}
