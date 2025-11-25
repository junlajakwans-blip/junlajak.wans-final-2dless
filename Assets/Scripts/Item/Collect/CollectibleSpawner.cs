using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour, ISpawn
{
    #region Fields
    [Header("Spawner Settings")]
    [Tooltip("ใส่ Prefab ทุกชนิดที่ต้องการ Spawn (รวม Coin/Takoyaki)")]
    [SerializeField] private List<GameObject> _collectiblePrefabs = new();

    [Header("Item Distribution (Rarity)")]
    [Tooltip("Prefabs ที่เป็น Buff/Utility (Coffee, GreenTea, MooKrata)")]
    [SerializeField] private List<GameObject> _buffUtilityPrefabs = new();
    [Tooltip("โอกาสที่ Collectible จะเป็น Takoyaki (Risk Item)")]
    [SerializeField] private float _takoyakiChance = 0.25f;
    [Tooltip("โอกาสที่ Collectible จะเป็น Buff/Utility (Rare Item)")]
    [SerializeField] private float _buffChance = 0.05f;

    [Header("Placement Physics")]
    [SerializeField] private LayerMask _groundLayer;    // Platform/Ground
    [SerializeField] private LayerMask _obstacleLayer;  // Obstacle/Enemy/Collectible
    [SerializeField] private float _rayDistance = 5f;
    [SerializeField] private float _groundOffset = 0.5f;

    [Header("Coin Trail")]
    [SerializeField] private CoinTrailGenerator _coinTrailGenerator;
    [SerializeField] private float _coinTrailChance = 0.20f;

    [Header("Runtime Data")]
    [SerializeField] private List<GameObject> _activeCollectibles = new();

    [Header("References")]
    private IObjectPool _objectPool;
    private DistanceCulling _cullingManager;
    private CardManager _cardManager;
    private BuffManager _buffManager;
    #endregion

    #region Initialization & Cleanup

    public void InitializeSpawner(IObjectPool pool, DistanceCulling cullingManager, CardManager cardManager, BuffManager buffManager)
    {
        _objectPool = pool;
        _cullingManager = cullingManager;
        _cardManager = cardManager;
        this._buffManager = buffManager;
        
        //ยกเลิก Loop เก่าทั้งหมด
        CancelInvoke(); 
    }
    
    public void SetPlayer(Transform player) 
    { 
        // Unused: MapGenerator เป็นคนส่งตำแหน่งมาให้แล้ว
    }

    #endregion

    #region ISpawn Implementation (Core Spawn Logic)
    
    /// <summary>
    /// ถูกเรียกโดย MapGeneratorBase (สำหรับ Item ที่เกิดระหว่างทาง)
    /// </summary>
public GameObject SpawnAtPosition(Vector3 targetPos)
{
    if (_objectPool == null || _collectiblePrefabs.Count == 0) 
    {
        Debug.LogError("[Collectible] Spawn Failed: Pool or Prefab list is empty.");
        return null;
    }

    // 1.Raycast Down: หาพื้นจริงๆ
    // targetPos ที่รับมาตอนนี้คือจุดเริ่มต้น Raycast ที่สูงพอ
    if (!TryFindGround(targetPos, out Vector3 finalPos)) 
    {
        // FIX 1: เพิ่ม Debug Log เมื่อหาพื้นไม่เจอ
        Debug.LogWarning($"[Collectible] Spawn Failed at X={targetPos.x:F1}: No Ground Found (Check _groundLayer/Raycast setup).");
        return null; // ไม่เจอพื้น (เหว)
    }

    // 2.SpawnSlot Check: ป้องกันทับซ้อน (สำคัญ)
    if (!SpawnSlot.Reserve(finalPos))
    {
        // FIX 2: เพิ่ม Debug Log เมื่อ Slot ถูกจองแล้ว
        Debug.LogWarning($"[Collectible] Spawn Failed at X={finalPos.x:F1}: Slot Reserved by another object (Asset/Platform).");
        return null; // Slot ถูกจองแล้ว (ทับ Asset/Collectible อื่น)
    }
    
    // 3.Coin Trail Chance
    if (_coinTrailGenerator != null && Random.value < _coinTrailChance)
    {
        _coinTrailGenerator.SpawnRandomTrail(finalPos);
        SpawnSlot.Unreserve(finalPos); // ไม่ได้วาง Item เดี่ยว เลยต้องคืน Slot
        Debug.Log($"[Collectible] Coin Trail Spawned at X={finalPos.x:F1}."); // Debug Trail
        return null; 
    }

    // 4.Smart Item Selection
    GameObject prefabToSpawn = GetSmartItemPrefab();
    if (prefabToSpawn == null) 
    {
        SpawnSlot.Unreserve(finalPos); // ไม่เจอ Item ที่จะ Spawn คืน Slot
        Debug.LogError($"[Collectible] Failed: GetSmartItemPrefab returned null (Check Prefab names/list)."); // Debug Prefab Null
        return null; 
    }

    // 5. Spawn และ Inject
    var collectible = _objectPool.SpawnFromPool(
        GetObjectTag(prefabToSpawn), 
        finalPos,
        Quaternion.identity
    );

    if (collectible != null)
    {
        _activeCollectibles.Add(collectible);
        _cullingManager?.RegisterObject(collectible);
        if (collectible.TryGetComponent<CollectibleItem>(out var item))
        {
            item.SetDependencies(_cardManager, this, _buffManager);
        }
        // FIX 3: Log การ Spawn ที่สำเร็จ
        Debug.Log($"[Collectible] Spawned SUCCESS: {prefabToSpawn.name} at X={finalPos.x:F1}.");
    }
    else
    {
        SpawnSlot.Unreserve(finalPos); // Spawn ไม่สำเร็จ คืน Slot
        Debug.LogError($"[Collectible] Spawn Failed: ObjectPool failed for {GetObjectTag(prefabToSpawn)}.");
    }

    return collectible;
}

    public void Spawn() { /* Unused */ } 

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
        
        // สำคัญ: ต้อง Unreserve Slot เมื่อ Despawn
        SpawnSlot.Unreserve(collectible.transform.position);
        
        _activeCollectibles.Remove(collectible);
        _cullingManager?.UnregisterObject(collectible);
        
        _objectPool.ReturnToPool(GetObjectTag(collectible), collectible);
    }
    
    public int GetSpawnCount() => _activeCollectibles.Count;
    
    #endregion

    #region Monster Item Drop Logic
    /// <summary>
    /// ถูกเรียกโดย Enemy เมื่อตาย (Monster Drop)
    /// </summary>
    public GameObject DropCollectible(CollectibleType type, Vector3 position)
    {
        if (_objectPool == null)
        {
            Debug.LogError("[CollectibleSpawner] Object Pool is not initialized!");
            return null;
        }
        
    
        // ไอเทมที่ดรอปต้องไม่เกิดทับสิ่งอื่นที่จอง Slot ไว้
        if (!SpawnSlot.Reserve(position))
        {
            // Slot ถูกจองแล้ว เช่น ดรอปทับ Item ที่เพิ่งเกิด หรือ Asset ที่อยู่ตรงนั้น
            // ปล่อยให้มันหายไป (ไม่ดรอป) เพื่อแก้ปัญหาการซ้อนทับ
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
                collectibleItem.SetDependencies(_cardManager, this, _buffManager); 
            }
        }
        else
        {
             // Spawn ไม่สำเร็จ (เช่น Prefab Tag ผิด) ต้อง Unreserve
            SpawnSlot.Unreserve(position);
        }

        return collectible;
    }

    #endregion

    #region Smart Item Logic & Helpers
    
    private GameObject GetSmartItemPrefab()
    {
        // 1. Roll สำหรับ Takoyaki (Risk Item)
        if (Random.value < _takoyakiChance)
        {
            return FindPrefabOfType("Takoyaki");
        }
        
        // 2. Roll สำหรับ Buff/Utility (Rare Item)
        if (Random.value < _buffChance && _buffUtilityPrefabs.Count > 0)
        {
            return _buffUtilityPrefabs[Random.Range(0, _buffUtilityPrefabs.Count)];
        }
        
        // 3. Default: Coin
        return FindPrefabOfType("Coin");
    }
    
    private GameObject FindPrefabOfType(string name)
    {
        // หาจาก Prefab List ทั้งหมด
        return _collectiblePrefabs.Find(p => p != null && p.name.Contains(name));
    }
    
    private string GetObjectTag(GameObject obj)
    {
        string name = obj.name;
        int index = name.IndexOf("(Clone)");
        if (index > 0) return name.Substring(0, index).Trim();
        return name;
    }

    private bool TryFindGround(Vector3 origin, out Vector3 result)
    {
        result = Vector3.zero;
        
        // โค้ดเก่า: Vector2 rayOrigin = new Vector2(origin.x, origin.y + 20f); 
        // โค้ดใหม่: ใช้ origin ที่ MapGeneratorBase ส่งมา (pos.y + 5f) เป็นจุดเริ่มโดยตรง
        Vector2 rayOrigin = origin; 
        float safeRayDistance = 40f; // ระยะยิงยังคงเพียงพอ (จาก Y ที่สูง)
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, safeRayDistance, _groundLayer);

        if (hit.collider != null)
        {
            result = hit.point;
            result.y += _groundOffset;
            return true;
        }
        return false;
    }

    #endregion
    
}