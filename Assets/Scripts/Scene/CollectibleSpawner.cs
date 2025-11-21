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
    [SerializeField] private CoinTrailGenerator _coinTrailGenerator;
    [SerializeField] private float collectibleGroundOffset = 0.4f;


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

        // --- Coin Trail Mode (20%) ---
        if (_coinTrailGenerator != null && Random.value < 0.20f)
        {
            Vector3 start = GetSpawnBasePosition();
            _coinTrailGenerator.SpawnRandomTrail(start);
            return; // ข้าม collectible เดี่ยวในรอบนี้
        }

        // --- Single collectible spawn ---
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
        SpawnSlot.Unreserve(collectible.transform.position);
        
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
        // ★ Auto follow player position
        if (_playerTransform != null)
        {
            _spawnArea.x = _playerTransform.position.x - (_spawnArea.width * 0.5f);
        }

        float x = Random.Range(_spawnArea.xMin, _spawnArea.xMax);
        float y = _spawnArea.yMax - collectibleGroundOffset ;

        return new Vector3(x, y, 0f);
    }


    /// <summary>
    /// ใช้ตำแหน่งฐานเดียวกับ collectible ปกติ — สำหรับ Coin Trail
    /// เกิดบริเวณเดียวกับของเก็บ แต่สูงจากพื้นเล็กน้อยเพื่ออ่านเส้นวิ่งง่าย
    /// </summary>
    private Vector3 GetSpawnBasePosition()
    {
        if (_playerTransform != null)
        {
            float x = _playerTransform.position.x + 6f; // อยู่ข้างหน้าผู้เล่น
            float y = _spawnArea.yMin + 1.2f;
            return new Vector3(x, y, 0f);
        }

        // ถ้าไม่มี player ใช้แบบสุ่ม fallback
        float fallbackX = Random.Range(_spawnArea.xMin, _spawnArea.xMax);
        float baseY = _spawnArea.yMin + 1.2f;
        return new Vector3(fallbackX, baseY, 0);
    }


    public GameObject SpawnRandomItem()
    {
        if (_collectiblePrefabs.Count == 0) return null;

        Vector3 position = GetRandomSpawnPosition();
        GameObject prefab;
        CollectibleItem itemData;

        do
        {
            prefab = _collectiblePrefabs[Random.Range(0, _collectiblePrefabs.Count)];
            itemData = prefab.GetComponent<CollectibleItem>();
        }
        while (itemData != null && !IsRandomSpawnItem(itemData.GetCollectibleType()));

        var item = _objectPool.SpawnFromPool(
            prefab.name,
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


    private bool IsRandomSpawnItem(CollectibleType type)
    {
        return type == CollectibleType.Coin ||
            type == CollectibleType.GreenTea ||
            type == CollectibleType.Coffee ||
            type == CollectibleType.MooKrata ||
            type == CollectibleType.Takoyaki;
    }
    #endregion

    [SerializeField] private Transform _playerTransform;
    public void SetPlayer(Transform player) => _playerTransform = player;


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
