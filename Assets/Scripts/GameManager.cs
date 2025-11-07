using UnityEngine;

public class GameManager : MonoBehaviour
{
#region  Fields
    private static GameManager _instance;
    private string _currentScene;
    private bool _isPaused;

    private Player _player;
    private SaveSystem _saveSystem;
    private UIManager _uiManager;
    private SceneManager _sceneManager;

    private int _score;
    private float _playTime;

#endregion

#region Properties
    public static GameManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }
#endregion

#region Unity Methods
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
        {
            _playTime += Time.deltaTime;
        }
    }

#endregion

#region Initialization and Game Flow state methods

    public void InitializeGame()
    {
        _saveSystem = FindObjectOfType<SaveSystem>();
        _uiManager = FindObjectOfType<UIManager>();
        _sceneManager = FindObjectOfType<SceneManager>();
        _player = FindObjectOfType<Player>();

        _currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        _isPaused = false;
        _score = 0;
        _playTime = 0f;
    }

    public void StartGame()
    {
        _uiManager?.ShowGameplayUI();
        _isPaused = false;
        _playTime = 0f;
        _score = 0;
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        _uiManager?.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        _uiManager?.HidePauseMenu();
    }

    public void EndGame()
    {
        _isPaused = true;
        _uiManager?.ShowGameOverScreen();
        SaveProgress();
    }

    public void RestartGame()
    {
        _score = 0;
        _playTime = 0f;
        _isPaused = false;

        UnityEngine.SceneManagement.SceneManager.LoadScene(_currentScene);
    }

    public void LoadScene(string sceneName)
    {
        _sceneManager?.LoadScene(sceneName);
        _currentScene = sceneName;
    }

    public void SaveProgress()
    {
        _saveSystem?.SaveData(_player, _score, _playTime);
    }

    public void AddScore(int amount)
    {
        _score += amount;
        _uiManager?.UpdateScore(_score);
    }

    public float GetPlayTime()
    {
        return _playTime;
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

#endregion