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

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    [SerializeField] private DistanceCulling _cullingManager;

    private List<GameObject> _validPrefabsCache = new();

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
    public void InitializeSpawner(IObjectPool pool, MapType mapType)
    {
        _objectPool = pool;
        _mapType = mapType;
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

        if (_activeEnemies.Count >= _maxEnemies || _validPrefabsCache.Count ==0)
            return;

        // Filter only enemies that can appear in this map
        var validEnemies = _enemyPrefabs
            .Where(prefab =>
            {
                var enemyComp = prefab.GetComponent<Enemy>();
                return enemyComp != null && enemyComp.EnemyType.CanAppearInMap(_mapType);
            })
            .ToList();

        if (validEnemies.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] No valid enemies for map {_mapType}.");
            return;
        }

        int randomEnemy = Random.Range(0, validEnemies.Count);
        int randomPoint = Random.Range(0, _spawnPoints.Count);

        Vector3 spawnPos = _spawnPoints[randomPoint].position;
        Quaternion spawnRot = _spawnPoints[randomPoint].rotation;

        var enemy = _objectPool.SpawnFromPool(validEnemies[randomEnemy].name, spawnPos, spawnRot);

        _activeEnemies.Add(enemy);
        _cullingManager?.RegisterObject(enemy);

        Debug.Log($"[EnemySpawner] Spawned {enemy.name} at {spawnPos}");
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        if (_validPrefabsCache.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] No valid enemies for map {_mapType}.");
            return null;
        }

        int randomEnemy = Random.Range(0, _validPrefabsCache.Count);
        var enemy = _objectPool.SpawnFromPool(_validPrefabsCache[randomEnemy].name, position, Quaternion.identity);
        _activeEnemies.Add(enemy);
        _cullingManager?.RegisterObject(enemy);
        return enemy;
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

        // Find the prefab that matches the requested EnemyType
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

        var enemy = _objectPool.SpawnFromPool(objectTag, position, Quaternion.identity);
        
        if (enemy != null)
        {
            _activeEnemies.Add(enemy);
            _cullingManager?.RegisterObject(enemy);
            Debug.Log($"[EnemySpawner] Spawned specific enemy {enemy.name} at {position}");
        }

        return enemy;
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
