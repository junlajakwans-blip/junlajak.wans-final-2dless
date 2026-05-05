// using System.Collections;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// /// <summary>
// /// SceneManager — ประสาน MapGeneratorBase, Player, ObjectPoolManager
// /// </summary>
// public sealed class SceneManager : MonoBehaviour
// {
//     [Header("Assigned by DI")]
//     private MapGeneratorBase _mapGenerator;
//     private Player _player;

//     private string _sceneName = string.Empty;
//     private MapType _mapType = MapType.None;
//     private bool _initialized;

//     public static event System.Action OnSceneInitialized;

//     private void OnEnable()
//     {
//         UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
//     }

//     private void OnDisable()
//     {
//         UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
//     }

//     private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//     {
//         if (_initialized) return;

//         StartCoroutine(InitRoutine());
//     }

//     private IEnumerator InitRoutine()
//     {
//         // รอ MapGenerator 
//         yield return new WaitUntil(() => 
//             FindAnyObjectByType<MapGeneratorBase>() != null
//         );

//         _mapGenerator = FindAnyObjectByType<MapGeneratorBase>();

//         // รอ Player spawn 
//         yield return new WaitUntil(() => 
//             FindAnyObjectByType<Player>() != null
//         );

//         _player = FindAnyObjectByType<Player>();

//         TryInitializeScene();
//     }

//     // GameManager เรียกหลังโหลด scene
//     public void Inject(MapGeneratorBase gen, Player player)
//     {
//         _mapGenerator = gen;
//         _player = player;
//     }

//     private void AutoFindIfNeeded()
//     {
//         if (_mapGenerator == null)
//             _mapGenerator = FindAnyObjectByType<MapGeneratorBase>();

//         if (_player == null)
//             _player = FindAnyObjectByType<Player>();
//     }

//     public void TryInitializeScene()
//     {
//         if (_initialized) return;
//         if (_mapGenerator == null) return;

//         // 1) ดึง Pool กลางจาก Singleton
//         var pool = ObjectPoolManager.Instance;
//         if (pool != null)
//         {
//             pool.InitializePool();  // มี IsInitialized กันซ้ำแล้ว
//             Debug.Log("[SceneManager] ObjectPool Initialized");
//         }
//         else
//         {
//             Debug.LogError("[SceneManager] ObjectPoolManager.Instance not found! Make sure Pool exists in MainMenu/Bootstrap scene.");
//             return;
//         }

//         // 2) MapType จากชื่อฉาก
//         _sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//         _mapType = MapTypeExtensions.FromSceneName(_sceneName);

//         // 3) ให้ MapGenerator เตรียมตัว + สร้างแมพ
//         _mapGenerator.InitializeGenerators(_player != null ? _player.transform : null);
//         _mapGenerator.GenerateMap();
//         Debug.Log($"[SceneManager] GenerateMap completed → {_sceneName}");

//         // 4) Spawn player
//         if (_player != null)
//             SpawnPlayerAtStart();
//         else
//             Debug.LogError("[SceneManager] Player not assigned!");

//         _initialized = true;
//         Debug.Log($"[SceneManager] Initialized → {_sceneName}, Map={_mapType}");

//         OnSceneInitialized?.Invoke();
//     }

//     private void Start()
//     {
//         Debug.Log("[SceneManager] Waiting for GameManager DI...");
//     }

//     private void SpawnPlayerAtStart()
//     {
//         var spawnObj = GameObject.FindWithTag("PlayerSpawn")
//                       ?? GameObject.Find("PlayerSpawn")
//                       ?? GameObject.Find("SpawnPoint");

//         if (spawnObj != null && _player != null)
//             _player.transform.position = spawnObj.transform.position;
//     }

//     #region Accessors
//     public string GetCurrentSceneName() => _sceneName;
//     public MapType GetCurrentMapType() => _mapType;
//     public bool IsInitialized => _initialized;
//     public MapGeneratorBase GetMapGenerator() => _mapGenerator;
//     #endregion

//     public void ClearSceneObjects()
//     {
//         _mapGenerator = null;
//         _player = null;
//         _initialized = false;
//         _sceneName = string.Empty;
//         _mapType = MapType.None;
//     }
// }

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// SceneManager — คุม Map เท่านั้น (ไม่ยุ่ง Player)
/// รองรับกี่ Player ก็ได้
/// </summary>
public sealed class SceneManager : MonoBehaviour
{
    private MapGeneratorBase _mapGenerator;

    private string _sceneName = string.Empty;
    private MapType _mapType = MapType.None;
    private bool _initialized;

    public static SceneManager Instance { get; private set; }

    public bool IsInitialized => _initialized;

    public MapType GetCurrentMapType() => _mapType;

    public string GetCurrentSceneName() => _sceneName;

    public static event System.Action OnSceneInitialized;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

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
        if (_initialized) return;

        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        //  รอ MapGenerator
        yield return new WaitUntil(() =>
            FindAnyObjectByType<MapGeneratorBase>() != null
        );

        _mapGenerator = FindAnyObjectByType<MapGeneratorBase>();

        //  รอ PlayerManager spawn เสร็จ (สำคัญสุด)
        yield return new WaitUntil(() => 
        PlayerManager.Instance != null && 
        PlayerManager.Instance.GetAllPlayers().Count >= GameModeManager.Instance.PlayerCount
    );

    yield return new WaitForSeconds(0.2f);
    TryInitializeScene();
    }

    public void TryInitializeScene()
    {
        if (_initialized) return;
        if (_mapGenerator == null) return;

        //  Object Pool
        var pool = ObjectPoolManager.Instance;
        if (pool == null)
        {
            Debug.LogError("[SceneManager] ObjectPoolManager missing!");
            return;
        }

        pool.InitializePool();

        //  Scene Info
        _sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        _mapType = MapTypeExtensions.FromSceneName(_sceneName);

        //  เลือก Player “ตัวแรก” เป็น reference (แต่ไม่ผูกระบบ)
        var players = PlayerManager.Instance.GetAllPlayers();
        Transform reference = players.FirstOrDefault()?.transform;

        //  Generate Map (รองรับ null ได้)
        _mapGenerator.InitializeGenerators(reference);
        _mapGenerator.GenerateMap();

        Debug.Log($"[SceneManager] Map Generated → {_sceneName}");

        _initialized = true;

        OnSceneInitialized?.Invoke();
    }

    public void ClearSceneObjects()
    {
        _mapGenerator = null;
        _initialized = false;
        _sceneName = string.Empty;
        _mapType = MapType.None;
    }
}