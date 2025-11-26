using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AssetSpawner (Passive/Time-Gated):
/// - รับคำสั่งจาก MapGenerator (Passive)
/// - มีหน้าที่หลักในการตัดสินใจวางโดยอิงจาก "ระยะทาง (Phase)" และ "เวลา (Cooldown)" เท่านั้น
/// - **ไม่มีระบบ Raycast หาพื้น และ Overlap ป้องกันซ้อน (MapGenerator จัดการแล้ว)**
/// - **ใช้ SpawnSlot เพื่อจองตำแหน่งและป้องกันการซ้อนทับ**
/// </summary>
public class AssetSpawner : MonoBehaviour, ISpawn
{
    #region 1. Phase & Prefix Settings
    [Header("Prefix Settings")]
    [Tooltip("เช่น map_asset_School_ / map_asset_RoadTraffic_ / map_asset_Kitchen_")]
    [SerializeField] private string _assetPrefix = "map_asset_School_";

    [Header("Distance Phases (x-axis distance from start)")]
    [SerializeField] private float _phase1End = 700f;   // 0–700
    [SerializeField] private float _phase2End = 1600f;  // 700–1600, หลังจากนั้นคือ Phase3

    [Header("Spawn Interval (Cooldown Control)")]
    [Tooltip("ช่วงเวลา spawn สำหรับ Phase1 (ยิ่งเลขเยอะ ยิ่งเกิดน้อย)")]
    [SerializeField] private Vector2 _phase1Interval = new Vector2(6f, 9f);

    [Tooltip("ช่วงเวลา spawn สำหรับ Phase2")]
    [SerializeField] private Vector2 _phase2Interval = new Vector2(4f, 7f);

    [Tooltip("ช่วงเวลา spawn สำหรับ Phase3 (ไกลสุด, จะถี่สุด)")]
    [SerializeField] private Vector2 _phase3Interval = new Vector2(3f, 5f);
    #endregion

    #region 2. Placement Settings
    [Header("Placement Settings")]
    [Tooltip("Offset แนวตั้งสุดท้าย (แนะนำให้ MapGenerator จัดการให้หมด)")]
    [SerializeField] private float _verticalOffset = 0f; 
    #endregion

    #region 3. Runtime Data
    [Header("Runtime Debug")]
    [SerializeField] private List<string> _cachedAssetKeys = new List<string>();
    [SerializeField] private List<GameObject> _activeAssets = new List<GameObject>();

    private IObjectPool _pool;
    private Transform _pivot;     // ใช้คำนวณระยะทาง (Player)
    private float _startX;
    private float _nextSpawnAllowedTime = 0f; // ตัวคุมเวลา (Cooldown)
    #endregion

    #region Initialization

    private void LateUpdate()
    {
        if (_pivot == null) return;

        for (int i = _activeAssets.Count - 1; i >= 0; i--)
        {
            var obj = _activeAssets[i];
            if (obj == null)
            {
                _activeAssets.RemoveAt(i);
                continue;
            }

            // ถ้า Asset อยู่ด้านหลังผู้เล่นเกินระยะ 30 หน่วย → Despawn
            if (obj.transform.position.x < _pivot.position.x - 30f)
            {
                Despawn(obj);
            }
        }
    }


    public void Initialize(Transform pivot, IObjectPool pool = null)
    {
        _pivot = pivot;
        if (_pivot != null) _startX = _pivot.position.x;

        _pool = pool ?? ObjectPoolManager.Instance; 
        
        if (_pool == null)
        {
            Debug.LogError("[AssetSpawner] ObjectPool is null after initialization!");
            return;
        }

        CacheAssetKeys(_pool as ObjectPoolManager);
        
        CalculateNextSpawnTime(0f);

        Debug.Log($"[AssetSpawner] Initialized with Prefix: '{_assetPrefix}'");
    }
    
    // Helper เดิม
    private void CacheAssetKeys(ObjectPoolManager poolManager)
    {
        _cachedAssetKeys.Clear();
        if (poolManager == null) return;

        List<string> allTags = poolManager.GetAllTags();
        for (int i = 0; i < allTags.Count; i++)
        {
            string tag = allTags[i];
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith(_assetPrefix))
                _cachedAssetKeys.Add(tag);
        }

        if (_cachedAssetKeys.Count == 0)
            Debug.LogWarning($"[AssetSpawner] No asset keys found for prefix '{_assetPrefix}'");
    }

    #endregion

    #region Core Spawn Logic
    
    /// <summary>
    /// ฟังก์ชันนี้ MapGenerator จะเป็นคนเรียก
    /// AssetSpawner มีสิทธิ์ปฏิเสธถ้ายังไม่ถึงเวลา (Cooldown) และ/หรือ Slot ถูกจองแล้ว
    /// </summary>
    private GameObject SpawnAssetAt(Vector3 targetPos)
    {
        if (_pool == null || _cachedAssetKeys.Count == 0) return null;
        if (_pivot == null) return null;

        // ยังไม่ถึงเวลา → ไม่ spawn
        if (Time.time < _nextSpawnAllowedTime)
            return null;

        // ถึงเวลาแล้ว → ส่งไปให้ SpawnNow ทำงานทั้งหมด
        return SpawnNow(targetPos);
    }


    private GameObject SpawnNow(Vector3 targetPos)
    {
        Vector3 pos = targetPos;
        pos.y += _verticalOffset;

        // Cache Check
        if (_cachedAssetKeys == null || _cachedAssetKeys.Count == 0)
        {
            Debug.LogWarning("[AssetSpawner] No cached keys found — reloading...");
            CacheAssetKeys(_pool as ObjectPoolManager);
            if (_cachedAssetKeys.Count == 0) return null;
        }

        // Slot Check
        if (!SpawnSlot.Reserve(pos))
            return null;

        // Random key
        string key = _cachedAssetKeys[Random.Range(0, _cachedAssetKeys.Count)];
        GameObject obj = _pool.SpawnFromPool(key, pos, Quaternion.identity);

        if (obj == null)
        {
            SpawnSlot.Unreserve(pos);
            return null;
        }

        // Optional flip
        if (Random.value > 0.5f)
        {
            Vector3 s = obj.transform.localScale;
            s.x = -Mathf.Abs(s.x);
            obj.transform.localScale = s;
        }

        // Track pooled obj
        _activeAssets.Add(obj);

        if (Random.value < 0.25f) // 25% chance
        {
            Vector3 offset = Vector3.right * Random.Range(1f, 3f);
            SpawnNow(targetPos + offset);
        }

        // Cooldown next spawn
        float dist = Mathf.Max(0, _pivot.position.x - _startX);
        CalculateNextSpawnTime(dist);

        return obj;
    }



    private void CalculateNextSpawnTime(float distance)
    {
        // Logic เดิม (Phase-based Cooldown)
        Vector2 interval;

        if (distance < _phase1End)
            interval = _phase1Interval;
        else if (distance < _phase2End)
            interval = _phase2Interval;
        else
            interval = _phase3Interval;

        float waitTime = Random.Range(interval.x, interval.y);
        _nextSpawnAllowedTime = Time.time + waitTime;
    }

    #endregion

    #region ISpawn Implementation (Core)

    // Hook ให้ MapGenerator เรียก
    public GameObject SpawnAtPosition(Vector3 position)
    {
        return SpawnAssetAt(position);
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null || _pool == null) return;
        
        // ต้อง Unreserve Slot เมื่อ Despawn
        SpawnSlot.Unreserve(obj.transform.position); 
        
        _activeAssets.Remove(obj);
        string objectTag = GetObjectTag(obj);
        _pool.ReturnToPool(obj.name.Replace("(Clone)", "").Trim(), obj);
    }

    public int GetSpawnCount() => _activeAssets.Count;
    public void Spawn() { } // ไม่ใช้

    #endregion

    #region Helper Methods

    /// <summary>
    /// ดึงชื่อ Prefab/Tag ที่ถูกต้องจากชื่อ GameObject ที่มี "(Clone)" ต่อท้าย
    /// </summary>
    protected string GetObjectTag(GameObject obj)
    {
        if (obj == null) return string.Empty;
        string name = obj.name;
        int index = name.IndexOf("(Clone)");
        
        // ถ้าเจอ (Clone) ให้ตัดส่วนนั้นออก
        if (index > 0) 
            return name.Substring(0, index).Trim();
        
        // ถ้าไม่เจอ อาจจะเป็น Prefab/Tag ที่ถูกต้องอยู่แล้ว หรือเป็นแค่ชื่อ
        return name;
    }
    

    #endregion
}