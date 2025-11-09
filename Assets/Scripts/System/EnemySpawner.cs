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
        Debug.Log($"[EnemySpawner] Initialized for map: {_mapType.ToFriendlyString()} | " + $"Platform: {Application.platform} | MaxEnemies={_maxEnemies}");
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

        if (_activeEnemies.Count >= _maxEnemies)
            return;

        // 🎯 Filter only enemies that can appear in this map
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

        Debug.Log($"[EnemySpawner] Spawned {enemy.name} at {spawnPos}");
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
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
            return null;
        }

        int randomEnemy = Random.Range(0, validEnemies.Count);
        var enemy = _objectPool.SpawnFromPool(validEnemies[randomEnemy].name, position, Quaternion.identity);
        _activeEnemies.Add(enemy);
        return enemy;
    }

    public void Despawn(GameObject enemy)
    {
        if (enemy == null) return;
        _activeEnemies.Remove(enemy);
        _objectPool.ReturnToPool(enemy.name, enemy);
    }

    public int GetSpawnCount() => _activeEnemies.Count;
    #endregion


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
