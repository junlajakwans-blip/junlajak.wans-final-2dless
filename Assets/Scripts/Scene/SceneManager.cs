using UnityEngine;

/// <summary>
/// SceneManager — pulls data from MapGeneratorBase and coordinates other systems.
/// Clean, WebGL-friendly (no async), and follows the DUFFDUCK UML.
/// </summary>
public sealed class SceneManager : MonoBehaviour
{
    #region Fields
    [Header("Scene References")]
    [SerializeField] private MapGeneratorBase _mapGenerator;
    [SerializeField] private Player _player;

    private string _sceneName = string.Empty;
    private MapType _mapType = MapType.None;
    private bool _isInitialized;
    #endregion

    #region Scene Setup
    /// <summary>
    /// Initializes once after all scene objects are ready.
    /// </summary>
    private void Start()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneManager] Scene detected: {scene}");

        if (scene == "MainMenu")
        {
            Debug.Log("[SceneManager] Main Menu detected → no map generation required.");
            return;
        }

        // ดำเนินงานเฉพาะฉาก Gameplay
        _mapGenerator = FindFirstObjectByType<MapGeneratorBase>();

        if (_mapGenerator == null)
        {
            Debug.LogError("[SceneManager] MapGeneratorBase not found in scene.");
            return;
        }

        if (_isInitialized) return;

        _sceneName   = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        _mapGenerator = FindFirstObjectByType<MapGeneratorBase>();
        _player       = FindFirstObjectByType<Player>();

        if (_mapGenerator == null)
        {
            Debug.Log("SceneManager searching MapGenerator in scene: " +
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            return;
        }

        // Derive MapType from scene name (per MapTypeExtensions)
        _mapType = MapTypeExtensions.FromSceneName(_sceneName);

        InitializeFromMap();
        _isInitialized = true;
    }

    /// <summary>
    /// Delegates map initialization and positions the player at a spawn marker.
    /// </summary>
    private void InitializeFromMap()
    {
        // Let the map generator do its own internal setup
        _mapGenerator.InitializeGenerators();

        // Place player at a spawn marker provided by the map (no direct access to protected fields)
        SpawnPlayerAtStart();

        Debug.Log($"[SceneManager] Scene '{_sceneName}' ready. MapType={_mapType}");
    }
    #endregion

    #region Player Placement
    /// <summary>
    /// Positions the player using an existing scene marker (e.g., tag 'PlayerSpawn').
    /// </summary>
    public void SpawnPlayerAtStart()
    {
        if (_player == null) return;

        // 1) Preferred: tag-based spawn point (placed by the map)
        var spawnObj = GameObject.FindWithTag("PlayerSpawn");

        // 2) Fallbacks by common names (kept minimal and non-spaghetti)
        if (spawnObj == null) spawnObj = GameObject.Find("PlayerSpawn");
        if (spawnObj == null) spawnObj = GameObject.Find("SpawnPoint");

        if (spawnObj != null)
        {
            _player.transform.position = spawnObj.transform.position;
            Debug.Log($"[SceneManager] Player spawned at {spawnObj.transform.position}");
        }
        else
        {
            // If no marker exists, do nothing — Map/Level should provide it.
            Debug.LogWarning("[SceneManager] No spawn marker found (tag 'PlayerSpawn' or 'SpawnPoint').");
        }
    }
    #endregion

    #region Accessors
    /// <summary>Returns current active scene name.</summary>
    public string GetCurrentSceneName() => _sceneName;

    /// <summary>Returns current map type resolved from the scene name.</summary>
    public MapType GetCurrentMapType() => _mapType;

    /// <summary>Indicates whether initialization completed successfully.</summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>Returns the active MapGeneratorBase instance.</summary>
    public MapGeneratorBase GetMapGenerator() => _mapGenerator;
    #endregion

    #region Maintenance
    /// <summary>
    /// Clears local references (no allocations, no leaks).
    /// </summary>
    public void ClearSceneObjects()
    {
        _mapGenerator = null;
        _player = null;
        _isInitialized = false;
        _sceneName = string.Empty;
        _mapType = MapType.None;
    }
    #endregion
}
