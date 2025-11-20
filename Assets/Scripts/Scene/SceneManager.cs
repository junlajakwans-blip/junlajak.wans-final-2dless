using UnityEngine;

/// <summary>
/// SceneManager — pulls data from MapGeneratorBase and coordinates other systems.
/// Clean, WebGL-friendly (no async), and follows the DUFFDUCK UML.
/// </summary>
public sealed class SceneManager : MonoBehaviour
{
    [Header("Assigned by DI")]
    private MapGeneratorBase _mapGenerator;
    private Player _player;

    private string _sceneName = string.Empty;
    private MapType _mapType = MapType.None;
    private bool _initialized;

    // Called by GameManager AFTER scene is loaded
    public void Inject(MapGeneratorBase gen, Player player)
    {
        _mapGenerator = gen;
        _player = player;
    }

    private void Start()
    {

        // AUTO DI fallback (สำหรับกรณีกด Play Scene ตรง ๆ)
        if (_mapGenerator == null)
            _mapGenerator = FindAnyObjectByType<MapGeneratorBase>();

        if (_player == null)
            _player = FindAnyObjectByType<Player>();


        // ถ้าไม่ได้ Inject → อย่าเริ่ม
        if (_mapGenerator == null || _player == null)
        {
            Debug.LogWarning("[SceneManager] Not injected yet — waiting for DI.");
            return;
        }

        InitializeScene();

        if (_mapGenerator == null) Debug.LogError("SceneManager waiting DI: missing MapGenerator");
        if (_player == null) Debug.LogError("SceneManager waiting DI: missing Player");

    }


    private void InitializeScene()
    {
        if (_initialized) return;

        _sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        _mapType = MapTypeExtensions.FromSceneName(_sceneName);

        _mapGenerator.InitializeGenerators();
        _mapGenerator.GenerateMap(); 

        SpawnPlayerAtStart();

        _initialized = true;
        Debug.Log($"[SceneManager] Initialized → {_sceneName}, MapType={_mapType}");
    }

    private void SpawnPlayerAtStart()
    {
        var spawnObj = GameObject.FindWithTag("PlayerSpawn") 
                   ?? GameObject.Find("PlayerSpawn") 
                   ?? GameObject.Find("SpawnPoint");

        if (spawnObj != null)
            _player.transform.position = spawnObj.transform.position;
    }

    #region Accessors
    public string GetCurrentSceneName() => _sceneName;
    public MapType GetCurrentMapType() => _mapType;
    public bool IsInitialized => _initialized;
    public MapGeneratorBase GetMapGenerator() => _mapGenerator;
    #endregion

    #region Maintenance
    /// <summary>
    /// Clears scene bindings (used when unloading scene)
    /// </summary>
    public void ClearSceneObjects()
    {
        _mapGenerator = null;
        _player = null;
        _initialized = false;
        _sceneName = string.Empty;
        _mapType = MapType.None;
    }
    #endregion

    
}
