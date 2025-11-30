using UnityEngine;
using System.Collections.Generic;


public class ThrowableSpawner : MonoBehaviour, ISpawn
{
    //  ลบ DropTable ออก และแทนที่ด้วย List ของ ThrowableItemSO
    [Header("Item List (Per Map)")]
    [Tooltip("รายการ ThrowableItemSO ทั้งหมดที่สามารถสปาวได้ใน Map นี้")]
    [SerializeField] private List<ThrowableItemSO> _throwableItems = new List<ThrowableItemSO>();
    
    //  อ้างอิงถึง Prefab พื้นฐาน (ควรเป็น Prefab เดียวที่มี ThrowableItemInfo)
    [Header("Item Template")]
    [Tooltip("Prefab หลักของ Throwable (ควรมี ThrowableItemInfo ติดอยู่)")]
    [SerializeField] private GameObject _throwablePrefabTemplate; 


    [Header("Distance Phases")] 
    [Tooltip("ระยะทาง (X) สิ้นสุด Phase 1 (0-700)")]
    [SerializeField] private float _phase1End = 700f; 
    [Tooltip("ระยะทาง (X) สิ้นสุด Phase 2 (700-1600)")]
    [SerializeField] private float _phase2End = 1600f;


    [Header("Drop Chance (Phase Based)")]
    [Tooltip("โอกาสดรอปใน Phase 3 (ระยะไกลสุด)")]
    [SerializeField] private float _phase3DropChance = 0.15f; 
    [Tooltip("โอกาสดรอปใน Phase 1 (0-700)")]
    [SerializeField] private float _phase1DropChance = 0.40f; 
    [Tooltip("โอกาสดรอปใน Phase 2 (700-1600)")]
    [SerializeField] private float _phase2DropChance = 0.25f; 


    [Header("Placement Offset Y")]
    [Tooltip("Offset แนวตั้งสุดท้าย (ควรตั้งค่าใน MapGenerator)")]
    [SerializeField] private float _spawnYOffset = 0.5f;

    [Header("Pool Settings")]
    [Tooltip("จำนวน Throwable ที่จะ Pre-spawn ต่อ Type")]
    [SerializeField] private int _preSpawnAmount = 5;

    [Header("WebGL Optimization")]
    [Tooltip("Interval (seconds) to check if thrown items have fallen off screen.")]
    [SerializeField] private float _despawnCheckInterval = 0.2f;
    private float _despawnCheckTimer;


    private Transform _pivot; // Player
    private float _startX;
    private EnemySpawner _enemySpawner;

    private bool _canInteract = false;
    public bool CanInteract => _canInteract;

    // รายการของที่ spawn อยู่บนพื้น (ไม่รวมที่อยู่บนหัวผู้เล่น)
    [SerializeField] private List<GameObject> _activeThrowables = new();

    // Dedicated Pool for Throwables
    private Dictionary<string, Queue<GameObject>> _throwablePoolDictionary = new();

    // 🔥 NEW: Y-position threshold for automatic despawn
    private const float DESPAWN_Y_THRESHOLD = -3.0f; 

    #region Unity Lifecycle
    private void Update()
    {
        // 1. ลดภาระ CPU: ตรวจสอบการ Despawn ตามช่วงเวลาที่กำหนดเท่านั้น
        _despawnCheckTimer -= Time.deltaTime;
        if (_despawnCheckTimer <= 0f)
        {
            // Reset timer
            _despawnCheckTimer = _despawnCheckInterval;

            // 2. Check all active throwables to see if they need to be despawned
            // Note: Using a reverse loop to safely remove elements while iterating
            for (int i = _activeThrowables.Count - 1; i >= 0; i--)
            {
                GameObject obj = _activeThrowables[i];
                
                // 🔥 FIX: Check if the object is still valid (not destroyed)
                if (obj == null)
                {
                    _activeThrowables.RemoveAt(i);
                    continue; // Skip to the next item
                }
                
                // Check if the object has fallen below the screen/death plane
                if (obj.transform.position.y < DESPAWN_Y_THRESHOLD)
                {
                    // This will remove the object from _activeThrowables and return it to the pool
                    Despawn(obj);
                    // Note: Despawn removes the item from _activeThrowables, so the loop continues safely
                }
            }
        }
    }
    #endregion

    #region Initialization
    public void Initialize(Transform pivot, EnemySpawner enemySpawner = null)
    {
        _pivot = pivot;
        // FIX: ตรวจสอบ _pivot ก่อนเข้าถึง .position
        if (_pivot != null)
             _startX = _pivot.position.x; 
        
        // NEW: Reset Timer state
        _despawnCheckTimer = _despawnCheckInterval;

        _enemySpawner = enemySpawner ?? FindFirstObjectByType<EnemySpawner>();

        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemySpawned -= HandleEnemySpawned;
            _enemySpawner.OnEnemySpawned += HandleEnemySpawned;
        }

        // NEW: Initialize Dedicated Pools
        InitializeThrowablePools();

        _canInteract = true;
    }

    private void OnDisable()
    {
        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned -= HandleEnemySpawned;
    }
    
    /// <summary>
    ///  Creates a dedicated pool for each type of throwable item using the SO tag.
    /// </summary>
    private void InitializeThrowablePools()
    {
        //  ใช้ _throwableItems แทน _dropTable
        if (_throwableItems == null || _throwableItems.Count == 0 || _throwablePrefabTemplate == null) 
        {
            Debug.LogError("[ThrowableSpawner Pool] Item List or Template is missing. Cannot initialize pool.");
            return;
        }

        //  ใช้ Prefab Template ตัวเดียวในการ Instantiate ทุก Type
        GameObject prefabTemplate = _throwablePrefabTemplate; 

        //  วนลูปผ่าน List<ThrowableItemSO> โดยตรง
        foreach (var itemSO in _throwableItems)
        {
            string poolTag = itemSO?.poolTag; 
            if (string.IsNullOrEmpty(poolTag)) continue;
            
            if (!_throwablePoolDictionary.ContainsKey(poolTag))
            {
                _throwablePoolDictionary[poolTag] = new Queue<GameObject>();
                
                // Pre-spawn instances (ลด GC Spike)
                for (int i = 0; i < _preSpawnAmount; i++)
                {
                    //  Instantiate จาก Prefab Template ตัวเดียว
                    var obj = Instantiate(prefabTemplate, transform); 
                    obj.name = poolTag; // ตั้งชื่อให้ถูกต้องสำหรับ Lookup
                    obj.SetActive(false);
                    _throwablePoolDictionary[poolTag].Enqueue(obj);
                }
            }
        }
        Debug.Log($"[ThrowableSpawner] Initialized dedicated pools for {_throwablePoolDictionary.Count} throwable types.");
    }

    //  FindPrefabTemplate ถูกลบไปแล้ว เพราะใช้ _throwablePrefabTemplate ตัวเดียว
    #endregion

    #region Enemy Events → Drop Logic

    private void HandleEnemySpawned(Enemy enemy)
    {
        enemy.OnEnemyDied -= HandleEnemyDied;
        enemy.OnEnemyDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy) // จัดการ position Enemy when die เพื่อ drop
    {
        enemy.OnEnemyDied -= HandleEnemyDied;

        if (_pivot == null) return; // Guard
        
        float distance = Mathf.Max(0f, _pivot.position.x - _startX);
        
        float dropChance = 0f;
        
        //  FIX: ใช้ Logic Phase-based Drop Chance
        if (distance < _phase1End)
        {
            dropChance = _phase1DropChance;   // Phase 1
        }
        else if (distance < _phase2End)
        {
            dropChance = _phase2DropChance;      // Phase 2
        }
        else
        {
            dropChance = _phase3DropChance;      // Phase 3
        }
        
        if (Random.value < dropChance)
        {
             Vector3 pos = enemy.transform.position;
             SpawnThrowableAt(pos);
        }
    }

    #endregion

    #region Spawn Core
    private GameObject SpawnThrowableInstance(string poolTag, Vector3 position, Quaternion rotation)
    {
        if (!_throwablePoolDictionary.ContainsKey(poolTag))
        {
            Debug.LogError($"[ThrowableSpawner Pool] Missing pool for tag: {poolTag}. Cannot spawn.");
            return null;
        }

        var queue = _throwablePoolDictionary[poolTag];
        GameObject obj = null;

        // ดึงของจากคิว
        while (queue.Count > 0 && obj == null)
        {
            obj = queue.Dequeue();
        }

        if (obj == null)
        {
            //  Dynamic Expansion: ใช้ Template เดิมในการสร้างใหม่
            if (_throwablePrefabTemplate == null) return null;
            
            obj = Instantiate(_throwablePrefabTemplate, transform);
            obj.name = poolTag;
            Debug.LogWarning($"[ThrowableSpawner Pool] Dynamic created NEW instance for {poolTag} (Pool empty/destroyed).");
        }

        // Reset & Activate
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }


    private GameObject SpawnThrowableAt(Vector3 receivedPos)
    {
        //  ตรวจสอบ List _throwableItems
        if (_throwableItems == null || _throwableItems.Count == 0)
             return null;

        //  1. เลือก ThrowableItemSO ตามน้ำหนัก
        ThrowableItemSO itemSO = GetWeightedThrowableSO();
        if (itemSO == null) return null;

        string poolTag = itemSO.poolTag;


        // 2. กำหนดตำแหน่งสุดท้าย (เชื่อถือตำแหน่งที่ส่งมา)
        Vector3 finalPos = receivedPos;
        finalPos.y += _spawnYOffset; // เพิ่ม Offset ให้ลอยเหนือจุดเกิด Enemy

        // 3. Spawn Slot Check
        if (!SpawnSlot.Reserve(finalPos))
        {
            // หาก Slot ถูกจอง ให้ลองขยับไปด้านข้างเล็กน้อย (0.5 หน่วย)
            float offset = 0.5f;
            if (Random.value > 0.5f) offset = -offset;
            
            Vector3 tryPos = finalPos + new Vector3(offset, 0f, 0f);

            if (!SpawnSlot.Reserve(tryPos))
            {
                 Debug.LogWarning($"[ThrowableSpawner] Spawn Failed (Slot Reserved) at X={finalPos.x:F1}.");
                 return null;
            }
            finalPos = tryPos;
        }

        // 4. Spawn from Dedicated Pool
        GameObject obj = SpawnThrowableInstance(poolTag, finalPos, Quaternion.identity);
        
        if (obj == null) 
        {
            SpawnSlot.Unreserve(finalPos);
            return null;
        }
        
        // 5. Inject SO Data
        // การเรียก GetComponent ในครั้งแรกหลังจาก Pool นั้นไม่ก่อให้เกิด GC Spike ร้ายแรง
        if (obj.TryGetComponent<ThrowableItemInfo>(out var info))
        {
            // ส่ง SO เข้าไปใน Info โดยตรงเพื่อกำหนดคุณสมบัติทั้งหมด (Damage, Sprite, Scale)
            info.ApplyData(itemSO); 
        }
        
        // 6. ลงทะเบียนวัตถุที่เกิดสำเร็จ
        _activeThrowables.Add(obj);
        
        return obj;
    }

    /// <summary>
    ///  NEW: Return the ThrowableItemSO based on its weight.
    /// </summary>
    private ThrowableItemSO GetWeightedThrowableSO()
    {
        if (_throwableItems == null || _throwableItems.Count == 0) return null;
        
        float total = 0f;
        //  ใช้ SO.weight โดยตรง
        foreach (var itemSO in _throwableItems) 
        {
            if (itemSO != null) total += itemSO.weight;
        }
        
        if (total <= 0f) return null;

        float r = Random.value * total;
        //  ใช้ SO.weight โดยตรง
        foreach (var itemSO in _throwableItems)
        {
            if (itemSO != null)
            {
                r -= itemSO.weight;
                if (r <= 0f) return itemSO;
            }
        }
        // Fallback
        return _throwableItems[_throwableItems.Count - 1];
    }
    #endregion

    #region ISpawn Implementation
    public void Spawn()
    {
        if (_pivot == null) return;
        
        Vector3 pos = _pivot.position;
        SpawnAtPosition(pos);
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        return SpawnThrowableAt(position); 
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;
        
        // 1. Unreserve Slot (ถ้ายังมีการจองอยู่)
        SpawnSlot.Unreserve(obj.transform.position);

        // 2. เรียก OnReturnedToPool ก่อนคืน (รวมถึงการรีเซ็ต Scale และ Unparent ใน ThrowableItemInfo)
        if (obj.TryGetComponent<ThrowableItemInfo>(out var info))
            info.OnReturnedToPool();
        
        // 3. Remove จาก List และ Return (เข้า Pool ของตัวเอง)
        _activeThrowables.Remove(obj);
        ReturnThrowableToPool(obj);
    }

    /// <summary>
    /// Returns a throwable instance to its dedicated pool.
    /// </summary>
    private void ReturnThrowableToPool(GameObject obj)
    {
        if (obj == null) return;

        // ดึง Tag ที่ถูกต้อง
        string objectTag = obj.name;
        int index = objectTag.IndexOf("(Clone)");
        if (index > 0) objectTag = objectTag.Substring(0, index).Trim();

        if (!_throwablePoolDictionary.ContainsKey(objectTag))
        {
            Debug.LogWarning($"❌ [THROWABLE POOL ERROR] Missing dedicated pool for: {objectTag} (Destroying instance).");
            Destroy(obj); 
            return;
        }

        // Reset & Return
        obj.SetActive(false);
        _throwablePoolDictionary[objectTag].Enqueue(obj);
    }


    public int GetSpawnCount() => _activeThrowables.Count;

    public void HidePrompt()
    {
        // ใช้งานเมื่อมี UIManager
        // throw new System.NotImplementedException();
    }
    #endregion
}