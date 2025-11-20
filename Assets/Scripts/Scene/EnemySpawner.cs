using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles spawning of enemies according to the current <see cref="MapType"/>.
/// Integrates with <see cref="IObjectPool"/> for performance and reuse.
/// </summary>
public class EnemySpawner : MonoBehaviour, ISpawn
{
    #region Fields
    [Header("Spawner Settings")]
    [SerializeField] private MapType _mapType = MapType.None;
    [SerializeField] private List<GameObject> _enemyPrefabs = new();
    [SerializeField] private List<Transform> _spawnPoints = new();
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxEnemies = 5;

    [Header("Runtime Data")]
    [SerializeField] private int _currentWaveCount = 0;
    [SerializeField] private List<GameObject> _activeEnemies = new();

    // SingerDuck BuffMap Field
    [SerializeField] private float _goldenMonBonusChance = 0.02f; 

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    [SerializeField] private DistanceCulling _cullingManager;


    [Header("Injected Managers")]
    [SerializeField] private Player _player; 
    [SerializeField] private CollectibleSpawner _collectibleSpawner; 
    [SerializeField] private CardManager _cardManager;
    protected BuffManager _buffManagerRef;

    private List<GameObject> _validPrefabsCache = new();

    /// <summary>
    /// Event triggered every time a new enemy is spawned from the pool.
    /// Payload: The Enemy component of the newly spawned object.
    /// </summary>
    public event System.Action<Enemy> OnEnemySpawned;

    #endregion


    #region Platform Settings
    /// <summary>
    /// Determines the recommended maximum number of enemies based on the current platform.
    /// </summary>
    private int GetRecommendedEnemyCount()
    {
#if UNITY_WEBGL
            return 4;
#elif UNITY_ANDROID || UNITY_IOS
            return 5;
#else
        return 8; // PC / Console
#endif
    }
    #endregion


    #region Initialization
    /// <summary>
    /// Initializes the spawner with a given object pool and map context.
    /// </summary>
    public void InitializeSpawner(IObjectPool pool, MapType mapType, Player player, CollectibleSpawner collectibleSpawner, CardManager cardManager)
    {
        _objectPool = pool;
        _mapType = mapType;
        this._player = player;
        this._collectibleSpawner = collectibleSpawner;
        this._cardManager = cardManager;
        
        Debug.Log("[EnemySpawner] Initialized with dependencies (Player, CollectibleSpawner, CardManager).");

        _maxEnemies = GetRecommendedEnemyCount();

        CacheValidEnemies();

        Debug.Log($"[EnemySpawner] Initialized for map: {_mapType.ToFriendlyString()} | " + $"Platform: {Application.platform} | MaxEnemies={_maxEnemies}");
    }

    /// <summary>
    /// Filters the main prefab list based on the current MapType and stores the result in a cache.
    /// This prevents repeated GC allocation from LINQ in the Spawn methods.
    /// </summary>
    private void CacheValidEnemies()
    {
         _validPrefabsCache = _enemyPrefabs
            .Where(prefab =>
            {
                var enemyComp = prefab.GetComponent<Enemy>();
                // Assuming EnemyTypeExtensions has CanAppearInMap() method
                return enemyComp != null && enemyComp.EnemyType.CanAppearInMap(_mapType); 
            })
            .ToList();
        
        if (_validPrefabsCache.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] Caching resulted in 0 valid enemies for map {_mapType}. Check CanAppearInMap logic.");
        }
    }
    #endregion


    #region Enemy Handling & BuffMap Logic
    
    /// <summary>
    /// Event handler for enemy death, used to trigger BuffMap effects.
    /// </summary>
    private void HandleEnemyDied(Enemy enemy)
    {
        // 1. Clean up from active list and event
        _activeEnemies.Remove(enemy.gameObject);
        enemy.OnEnemyDied -= HandleEnemyDied; // Unsubscribe from the instance event

        // 2. SingerDuck BuffMap Logic: +2% GoldenMon Chance
        if (_player != null && _player.TryGetComponent<SingerDuck>(out var singerDuck))
        {
            // Check the public hook on SingerDuck
            if (singerDuck.IsMapBuffActive())
            {
                // The "Stacking" is passive, meaning the buff is always active (no decay)
                // If we want actual stacking, we'd need to increase _goldenMonBonusChance
                // based on the number of enemies killed or time. For now, use the base 2%.
                
                if (Random.value < _goldenMonBonusChance)
                {
                    // The bonus GoldenMon should spawn near the player or the dead enemy.
                    SpawnSpecificEnemy(EnemyType.GoldenMon, enemy.transform.position);
                    Debug.Log("<color=yellow>[SingerDuck BuffMap]</color> SUCCESS! GoldenMon spawned from +2% passive chance.");
                }
            }
        }
    }
    
    #endregion



    #region ISpawn Implementation
    /// <summary>
    /// Spawns a random valid enemy for the current map.
    /// </summary>
    public void Spawn()
    {
        if (_spawnPoints.Count == 0 || _enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Missing spawn points or enemy prefabs.");
            return;
        }

        if (_activeEnemies.Count >= _maxEnemies || _validPrefabsCache.Count == 0)
            return;

        // 1. Select enemy and spawn from cache
        int randomEnemy = Random.Range(0, _validPrefabsCache.Count); 
        int randomPoint = Random.Range(0, _spawnPoints.Count);

        Vector3 spawnPos = _spawnPoints[randomPoint].position;
        Quaternion spawnRot = _spawnPoints[randomPoint].rotation;

        // Use the prefab's name as the tag for the object pool
        string objectTag = _validPrefabsCache[randomEnemy].name;
        
        // 2. Spawn
        var enemyGO = _objectPool.SpawnFromPool(objectTag, spawnPos, spawnRot); 

        if (enemyGO != null)
        {
            _activeEnemies.Add(enemyGO);
            _cullingManager?.RegisterObject(enemyGO);
            
            // [NEW FIX] INJECT DEPENDENCIES & SUBSCRIBE
            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent))
            {
                // 1. INJECT DEPENDENCIES (DI)
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);

                // 2. SUBSCRIBE TO DEATH EVENT
                enemyComponent.OnEnemyDied += HandleEnemyDied;
                
                // 3. INVOKE SPAWN EVENT 
                OnEnemySpawned?.Invoke(enemyComponent);
            }
            
            Debug.Log($"[EnemySpawner] Spawned {enemyGO.name} at {spawnPos}");
        }
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        if (_validPrefabsCache.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] No valid enemies for map {_mapType}.");
            return null;
        }

        int randomEnemy = Random.Range(0, _validPrefabsCache.Count);
        var enemyGO = _objectPool.SpawnFromPool(_validPrefabsCache[randomEnemy].name, position, Quaternion.identity);
        
        if (enemyGO != null)
        {
            _activeEnemies.Add(enemyGO);
            _cullingManager?.RegisterObject(enemyGO);
            
            // [NEW FIX] INJECT DEPENDENCIES & SUBSCRIBE
            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent))
            {
                // 1. INJECT DEPENDENCIES (DI)
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);

                // 2. SUBSCRIBE TO DEATH EVENT
                enemyComponent.OnEnemyDied += HandleEnemyDied;
                
                // 3. INVOKE SPAWN EVENT
                OnEnemySpawned?.Invoke(enemyComponent);
            }
        }
        return enemyGO;
    }

    public void Despawn(GameObject enemy)
    {
        if (enemy == null) return;

        _cullingManager?.UnregisterObject(enemy);
        _activeEnemies.Remove(enemy);
        _objectPool.ReturnToPool(enemy.name, enemy);
    }

    public int GetSpawnCount() => _activeEnemies.Count;
    #endregion

/// <summary>
/// Spawns a specific enemy type at a given position.
/// Used by special skills (e.g., SingerDuck).
/// </summary>
/// <param name="type">The specific EnemyType to spawn.</param>
/// <param name="position">The world position to spawn at.</param>
    public GameObject SpawnSpecificEnemy(EnemyType type, Vector3 position)
    {
        if (_objectPool == null)
        {
            Debug.LogWarning("[EnemySpawner] Object Pool not initialized. Cannot spawn specific enemy.");
            return null;
        }

        // Find the prefab that matches the requested EnemyType (LINQ is okay here as it's not frequent)
        GameObject prefabToSpawn = _enemyPrefabs.FirstOrDefault(prefab =>
        {
            var enemyComp = prefab.GetComponent<Enemy>();
            return enemyComp != null && enemyComp.EnemyType == type;
        });

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[EnemySpawner] No prefab found for EnemyType: {type}");
            return null;
        }

        // Use the prefab's name as the tag for the object pool
        string objectTag = prefabToSpawn.name;

        var enemyGO = _objectPool.SpawnFromPool(objectTag, position, Quaternion.identity);
        
        if (enemyGO != null) 
        { 
            _activeEnemies.Add(enemyGO); 
            _cullingManager?.RegisterObject(enemyGO); 
            Debug.Log($"[EnemySpawner] Spawned specific enemy {enemyGO.name} at {position}"); 

            // [NEW FIX] INJECT DEPENDENCIES & SUBSCRIBE
            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent)) 
            { 
                // 1. INJECT DEPENDENCIES (DI)
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);

                // 2. SUBSCRIBE TO DEATH EVENT (เพื่อให้ HandleEnemyDied จัดการ Despawn)
                enemyComponent.OnEnemyDied += HandleEnemyDied; 

                // 3. INVOKE SPAWN EVENT 
                OnEnemySpawned?.Invoke(enemyComponent); 
            } 
        } 
        return enemyGO;

    }

    #region Wave Control
    /// <summary>
    /// Starts spawning waves of enemies over time.
    /// </summary>
    public IEnumerator StartWave()
    {
        Debug.Log($"[EnemySpawner] Starting wave {_currentWaveCount + 1} on {_mapType}...");
        _currentWaveCount++;

        while (_activeEnemies.Count < _maxEnemies)
        {
            Spawn();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }
    #endregion

}
