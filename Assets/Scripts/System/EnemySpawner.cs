using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, ISpawn
{
    #region Fields
    [Header("Spawner Settings")]
    [SerializeField] private List<GameObject> _enemyPrefabs = new();
    [SerializeField] private List<Transform> _spawnPoints = new();
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxEnemies = 10;

    [Header("Runtime Data")]
    [SerializeField] private int _currentWaveCount = 0;
    [SerializeField] private List<GameObject> _activeEnemies = new();

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    #endregion

    #region Initialization

    public void InitializeSpawner(IObjectPool pool)
    {
        _objectPool = pool;
        Debug.Log("[EnemySpawner] Initialized with object pool.");
    }
    #endregion

    #region ISpawn Implementation
    public void Spawn()
    {
        if (_spawnPoints.Count == 0 || _enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Missing spawn points or enemy prefabs.");
            return;
        }

        if (_activeEnemies.Count >= _maxEnemies)
            return;

        int randomEnemy = Random.Range(0, _enemyPrefabs.Count);
        int randomPoint = Random.Range(0, _spawnPoints.Count);

        Vector3 spawnPos = _spawnPoints[randomPoint].position;
        Quaternion spawnRot = _spawnPoints[randomPoint].rotation;

        var enemy = _objectPool.SpawnFromPool(_enemyPrefabs[randomEnemy].name, spawnPos, spawnRot);
        _activeEnemies.Add(enemy);
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        int randomEnemy = Random.Range(0, _enemyPrefabs.Count);
        var enemy = _objectPool.SpawnFromPool(_enemyPrefabs[randomEnemy].name, position, Quaternion.identity);
        _activeEnemies.Add(enemy);
        return enemy;
    }

    public void Despawn(GameObject enemy)
    {
        if (enemy == null) return;
        _activeEnemies.Remove(enemy);
        _objectPool.ReturnToPool(enemy.name, enemy);
    }

    public int GetSpawnCount()
    {
        return _activeEnemies.Count;
    }
    #endregion

    #region Wave Control
    public IEnumerator StartWave()
    {
        Debug.Log($"[EnemySpawner] Starting wave {_currentWaveCount + 1}...");
        _currentWaveCount++;

        while (_activeEnemies.Count < _maxEnemies)
        {
            Spawn();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }
    #endregion
}
