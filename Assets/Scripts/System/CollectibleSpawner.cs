using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour, ISpawn
{
    #region Fields
    [Header("Spawner Settings")]
    [SerializeField] private List<GameObject> _collectiblePrefabs = new();
    [SerializeField] private Rect _spawnArea = new Rect(-5f, -2f, 10f, 4f);
    [SerializeField] private float _spawnInterval = 2.5f;

    [Header("Runtime Data")]
    [SerializeField] private List<GameObject> _activeCollectibles = new();

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    #endregion

    #region Initialization

    public void InitializeSpawner(IObjectPool pool)
    {
        _objectPool = pool;
        Debug.Log("[CollectibleSpawner] Initialized with object pool.");
    }
    #endregion

    #region ISpawn Implementation
    public void Spawn()
    {
        if (_collectiblePrefabs.Count == 0)
        {
            Debug.LogWarning("[CollectibleSpawner] No collectible prefabs assigned.");
            return;
        }

        Vector3 position = GetRandomSpawnPosition();
        int randomIndex = Random.Range(0, _collectiblePrefabs.Count);

        var collectible = _objectPool.SpawnFromPool(
            _collectiblePrefabs[randomIndex].name,
            position,
            Quaternion.identity
        );

        _activeCollectibles.Add(collectible);
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        int randomIndex = Random.Range(0, _collectiblePrefabs.Count);
        var collectible = _objectPool.SpawnFromPool(
            _collectiblePrefabs[randomIndex].name,
            position,
            Quaternion.identity
        );

        _activeCollectibles.Add(collectible);
        return collectible;
    }

    public void Despawn(GameObject item)
    {
        if (item == null) return;
        _activeCollectibles.Remove(item);
        _objectPool.ReturnToPool(item.name, item);
    }

    public int GetSpawnCount()
    {
        return _activeCollectibles.Count;
    }
    #endregion

    #region Additional Logic

    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(_spawnArea.xMin, _spawnArea.xMax);
        float y = Random.Range(_spawnArea.yMin, _spawnArea.yMax);
        return new Vector3(x, y, 0f);
    }

    public GameObject SpawnRandomItem()
    {
        if (_collectiblePrefabs.Count == 0) return null;

        Vector3 position = GetRandomSpawnPosition();
        int randomIndex = Random.Range(0, _collectiblePrefabs.Count);

        var item = _objectPool.SpawnFromPool(
            _collectiblePrefabs[randomIndex].name,
            position,
            Quaternion.identity
        );

        _activeCollectibles.Add(item);
        return item;
    }
    #endregion
}
