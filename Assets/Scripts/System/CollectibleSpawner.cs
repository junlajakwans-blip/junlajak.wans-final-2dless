using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour, ISpawn
{
    #region Fields
    [Header("Spawner Settings")]
    [SerializeField] private List<GameObject> _collectiblePrefabs = new();
    [SerializeField] private Rect _spawnArea = new Rect(-5f, -2f, 10f, 4f);

    [Header("Spawn Timing")]
    [SerializeField] private float _spawnInterval = 3f; // เกิดทุก 3 วินาที
private float _timer = 0f;

    [Header("Runtime Data")]
    [SerializeField] private List<GameObject> _activeCollectibles = new();

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    [SerializeField] private DistanceCulling _cullingManager;
    private CardManager _cardManager;
    private BuffManager _buffManager;
    #endregion

    #region Initialization

    public void InitializeSpawner(IObjectPool pool, DistanceCulling cullingManager, CardManager cardManager, BuffManager buffManager)
    {
        _objectPool = pool;
        _cullingManager = cullingManager;
        _cardManager = cardManager;
        this._buffManager = buffManager;
        
        Debug.Log("[CollectibleSpawner] Initialized with object pool.");

        StartContinuousSpawning();
    }
    #endregion

    #region Spawning Control

    private void StartContinuousSpawning()
    {
        // ตรวจสอบว่ายังไม่ได้ถูกเรียกอยู่ เพื่อป้องกันการซ้ำซ้อน
        CancelInvoke(nameof(CheckAndSpawnItem)); 
        
        // เริ่มเรียกเมธอด CheckAndSpawnItem ซ้ำๆ ทุก _spawnInterval วินาที
        // 1f: ค่า Delay เริ่มต้นก่อนจะเริ่มเรียกครั้งแรก 
        InvokeRepeating(nameof(CheckAndSpawnItem), 1f, _spawnInterval);
    }

    // เมธอดนี้จะทำหน้าที่ตรวจสอบเงื่อนไขและเรียก Spawn
    private void CheckAndSpawnItem()
    {
        // ใช้เงื่อนไขเดิม: จำกัดจำนวนสูงสุดของ Collectibles ที่ active อยู่ในฉาก
        if (_activeCollectibles.Count >= 20)
        {
            // หากถึงจำนวนสูงสุดแล้ว ให้หยุด Spawn ชั่วคราว (แต่ InvokeRepeating ยังรันอยู่)
            return;
        }
        
        // ถ้ายังไม่ถึง ให้เรียก Spawn
        SpawnRandomItem();
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(CheckAndSpawnItem));
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
        _cullingManager?.RegisterObject(collectible);

        if (collectible.TryGetComponent<CollectibleItem>(out var collectibleItem))
        {
            collectibleItem.SetDependencies(_cardManager, this, _buffManager); 
        }
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
        _cullingManager?.RegisterObject(collectible);
        if (collectible.TryGetComponent<CollectibleItem>(out var collectibleItem))
            {
                collectibleItem.SetDependencies(_cardManager, this, _buffManager); 
            }
            
            return collectible;
    }

    /// <summary>
    /// Returns an object to the pool. Called by the CollectibleItem when collected.
    /// </summary>
    public void Despawn(GameObject collectible)
    {
        if (_objectPool == null)
        {
            Destroy(collectible);
            return;
        }

        // Unregister from Culling (ถ้าใช้) และ Active List
        _activeCollectibles.Remove(collectible);
        _cullingManager?.UnregisterObject(collectible);
        
        // Return to Pool โดยใช้ชื่อ Prefab เป็น Tag
        _objectPool.ReturnToPool(collectible.name, collectible);
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
        if (item.TryGetComponent<CollectibleItem>(out var collectibleItem))
            {
                collectibleItem.SetDependencies(_cardManager, this, _buffManager); 
            }
            
            return item;
    }
    #endregion

    #region Item Drop From Mon
    /// <summary>
    /// Spawns a specific collectible item using the Object Pool based on CollectibleType.
    /// </summary>
    /// <param name="type">The type of item to drop (e.g., Coin, Coffee).</param>
    /// <param name="position">The world position to spawn the item.</param>
    public GameObject DropCollectible(CollectibleType type, Vector3 position)
    {
        if (_objectPool == null)
        {
            Debug.LogError("[CollectibleSpawner] Object Pool is not initialized!");
            return null;
        }

        string prefabName = type.ToString();

        var collectible = _objectPool.SpawnFromPool(
            prefabName,
            position,
            Quaternion.identity
        );

    if (collectible != null)
    {
        _activeCollectibles.Add(collectible);
        
        // [NEW FIX]: INJECT DEPENDENCIES
        if (collectible.TryGetComponent<CollectibleItem>(out var collectibleItem))
        {
            // Inject CardManager (สำหรับ CardPickup) และ 'this' (สำหรับ Despawn)
            collectibleItem.SetDependencies(_cardManager, this, _buffManager); 
        }
        
        Debug.Log($"[CollectibleSpawner] Dropped {type}...");
    }

    return collectible;
    }

    #endregion

}
