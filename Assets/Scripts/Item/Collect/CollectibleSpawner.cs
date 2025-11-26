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
    // ⬅ REMOVED: ไม่ใช้ Raycast หาพื้นอีกแล้ว
    // [SerializeField] private LayerMask _groundLayer;    // Platform/Ground
    [SerializeField] private LayerMask _obstacleLayer;  // Obstacle/Enemy/Collectible
    //[SerializeField] private float _groundOffset = 0.5f; // ค่า Offset ยังคงเก็บไว้เพื่อใช้อ้างอิง/กำหนดค่าจาก Inspector

    [Header("Coin Trail")]
    [SerializeField] private CoinTrailGenerator _coinTrailGenerator;
    [Tooltip("โอกาสที่จะเกิด Coin Trail แทน Collectible เดี่ยว")]
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

        if (_coinTrailGenerator != null)
        {
            _coinTrailGenerator.InitializeDependencies(
                _objectPool,
                _cardManager,
                this, // ส่งตัวเอง (CollectibleSpawner)
                _buffManager
            ); 
        }
        
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
    /// **สมมติว่า targetPos เป็นตำแหน่งที่ถูกต้องบนพื้นผิวแล้ว (MapGen จัดการ Raycast)**
    /// </summary>
    public GameObject SpawnAtPosition(Vector3 targetPos)
    {
        if (_objectPool == null || _collectiblePrefabs.Count == 0) 
        {
            Debug.LogError("[Collectible] Spawn Failed: Pool or Prefab list is empty.");
            return null;
        }

        // ⬅ NEW — block spawn behind player (เกม 2D endless ต้องเกิดด้านขวา)
        Player player = GameManager.Instance.PlayerRef;
        if (player != null && targetPos.x <= player.transform.position.x)
            return null;

        // 1. กำหนดตำแหน่งสุดท้าย (จาก MapGenerator)
        // เราเชื่อถือ targetPos ว่าเป็นตำแหน่งบนพื้นผิวที่ถูกต้องแล้ว
        Vector3 finalPos = targetPos; 
        
        // 2. Coin trail check (โอกาสเกิด Coin Trail)
        // Coin Trail สามารถเกิดใกล้กันได้ แต่ถ้าเกิด Trail จะไม่มี Collectible เดี่ยว
        if (_coinTrailGenerator != null && Random.value < _coinTrailChance)
        {
            // Coin Trail จะจัดการการ Spawn ของเหรียญและ Slot ภายในตัวเอง
            _coinTrailGenerator.SpawnRandomTrail(finalPos);
            // ไม่ต้องมีการจอง Slot สำหรับ Collectible เดี่ยว เพราะเปลี่ยนเป็น Trail แล้ว
            return null;
        }

        // 3. Slot check สำหรับ Collectible เดี่ยว (ที่ไม่ใช่ Trail)
        // ต้องจอง Slot เพื่อป้องกันการซ้อนทับ Asset หรือ Collectible อื่น
        if (!SpawnSlot.Reserve(finalPos))
        {
            Debug.LogWarning($"[Collectible] Spawn Failed at X={finalPos.x:F1}: Slot Reserved.");
            return null;
        }

        // 4. Select item
        GameObject prefabToSpawn = GetSmartItemPrefab();
        if (prefabToSpawn == null)
        {
            // ยกเลิกการจอง Slot หากไม่มี Prefab ให้ Spawn
            SpawnSlot.Unreserve(finalPos); 
            Debug.LogError("[Collectible] GetSmartItemPrefab returned null.");
            return null;
        }

        // 5. Spawn + DI
        var objectTag = GetObjectTag(prefabToSpawn);
        var collectible = _objectPool.SpawnFromPool(
            objectTag, 
            finalPos,
            Quaternion.identity
        );

        if (collectible != null)
        {
            _activeCollectibles.Add(collectible);
            _cullingManager?.RegisterObject(collectible);

            // DI: Inject Dependencies
            if (collectible.TryGetComponent<CollectibleItem>(out var item))
                item.SetDependencies(_cardManager, this, _buffManager);

            Debug.Log($"[Collectible] Spawned SUCCESS: {objectTag} at X={finalPos.x:F1}.");
            return collectible;
        }

        // Spawn ไม่สำเร็จ (เช่น ObjectPool ล้มเหลว)
        SpawnSlot.Unreserve(finalPos);
        Debug.LogError($"[Collectible] Spawn Failed: ObjectPool failed for {objectTag}.");
        return null;
    }

    public void Spawn() { /* Unused */ } 

    /// <summary>
    /// Returns an object to the pool. Called by the CollectibleItem when collected or culled.
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
        
        // [FIX]: ใช้ GetObjectTag(collectible) แทน GetObjectTag(prefab)
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
        
        string prefabName = type.ToString();

        // 1. การจัดการ Slot สำหรับ Drop: Drop ที่เป็นเหรียญ (Coin) อนุญาตให้เกิดใกล้/ทับ Slot อื่นได้ 
        // (ยกเว้นกรณีที่ต้องการให้เหรียญที่ดรอปเป็น Collectible เดี่ยวเท่านั้น)
        bool isCoinDrop = type.ToString().Contains("Coin");
        bool isSlotReserved = SpawnSlot.IsReserved(position);
        
        if (!isCoinDrop && isSlotReserved)
        {
             // Slot ถูกจองแล้ว เช่น ดรอปทับ Item ที่เพิ่งเกิด หรือ Asset ที่อยู่ตรงนั้น
             // ปล่อยให้มันหายไป (ไม่ดรอป) เพื่อแก้ปัญหาการซ้อนทับ
             return null; 
        }

        // สำหรับ Drop ที่ไม่ใช่ Coin ต้องจอง Slot ก่อน
        if (!isCoinDrop && !SpawnSlot.Reserve(position))
        {
            // ควรไม่เกิดเหตุการณ์นี้แล้ว เพราะเช็คด้านบนแล้ว
            return null;
        }
        
        // 2. Spawn
        var collectible = _objectPool.SpawnFromPool(
            prefabName,
            position,
            Quaternion.identity
        );

        if (collectible != null)
        {
            _activeCollectibles.Add(collectible);
            
            // DI: INJECT DEPENDENCIES
            if (collectible.TryGetComponent<CollectibleItem>(out var collectibleItem))
            {
                collectibleItem.SetDependencies(_cardManager, this, _buffManager); 
            }
        }
        else
        {
             // Spawn ไม่สำเร็จ (เช่น Prefab Tag ผิด) ต้อง Unreserve ถ้าไม่ใช่ Coin Drop
            if (!isCoinDrop) SpawnSlot.Unreserve(position);
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
        // การดึง Tag จากชื่อ Prefab หรือ Object ที่เป็น Clone
        // เช่น "Coin(Clone)" -> "Coin"
        int index = name.IndexOf("(Clone)");
        if (index > 0) return name.Substring(0, index).Trim();
        return name;
    }

    // ⬅ REMOVED: ไม่ใช้ TryFindGround() แล้ว

    #endregion
    
}