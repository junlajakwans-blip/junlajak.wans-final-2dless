using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AssetSpawner (Hybrid):
/// - รอ MapGenerator สั่ง (Passive)
/// - แต่ตัดสินใจวางโดยอิงจาก "ระยะทาง (Phase)" และ "เวลา (Interval)"
/// - มีระบบ Raycast หาพื้น และ Overlap ป้องกันซ้อน
/// </summary>
public class AssetSpawner : MonoBehaviour, ISpawn
{
    #region 1. Phase & Prefix Settings (นำกลับมาแล้ว ✅)
    [Header("Prefix Settings")]
    [Tooltip("เช่น map_asset_School_ / map_asset_RoadTraffic_ / map_asset_Kitchen_")]
    [SerializeField] private string _assetPrefix = "map_asset_School_";

    [Header("Distance Phases (x-axis distance from start)")]
    [SerializeField] private float _phase1End = 700f;   // 0–700
    [SerializeField] private float _phase2End = 1600f;  // 700–1600, หลังจากนั้นคือ Phase3

    [Header("Spawn Interval (Cooldown Control)")]
    [Tooltip("ช่วงเวลา spawn สำหรับ Phase1 (ยิ่งเลขเยอะ ยิ่งเกิดน้อย)")]
    [SerializeField] private Vector2 _phase1Interval = new Vector2(6f, 9f);

    [Tooltip("ช่วงเวลา spawn สำหรับ Phase2")]
    [SerializeField] private Vector2 _phase2Interval = new Vector2(4f, 7f);

    [Tooltip("ช่วงเวลา spawn สำหรับ Phase3 (ไกลสุด, จะถี่สุด)")]
    [SerializeField] private Vector2 _phase3Interval = new Vector2(3f, 5f);
    #endregion

    #region 2. Placement Settings (Physics)
    [Header("Placement Settings (Physics)")]
    [SerializeField] private LayerMask _groundLayer;    // ⚠️ ต้องตั้งค่าใน Inspector
    [SerializeField] private LayerMask _obstacleLayer;  // ⚠️ ต้องตั้งค่า (Obstacle, Collectible, Enemy)
    [SerializeField] private float _checkRadius = 0.5f; 
    [SerializeField] private float _rayDistance = 5f;   
    [SerializeField] private float _verticalOffset = 0f;
    #endregion

    #region 3. Runtime Data
    [Header("Runtime Debug")]
    [SerializeField] private List<string> _cachedAssetKeys = new List<string>();
    [SerializeField] private List<GameObject> _activeAssets = new List<GameObject>();

    private IObjectPool _pool;
    private Transform _pivot;     // ใช้คำนวณระยะทาง (Player)
    private float _startX;
    private float _nextSpawnAllowedTime = 0f; // ตัวคุมเวลา (Cooldown)
    #endregion

    #region Initialization

    public void Initialize(Transform pivot)
    {
        _pivot = pivot;
        if (_pivot != null) _startX = _pivot.position.x;

        var poolManager = ObjectPoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogError("[AssetSpawner] ObjectPoolManager.Instance is null!");
            return;
        }

        _pool = poolManager;
        CacheAssetKeys(poolManager);

        // ตั้งเวลา Spawn ครั้งแรก
        CalculateNextSpawnTime(0f);

        Debug.Log($"[AssetSpawner] Initialized with Prefix: '{_assetPrefix}'");
    }

    private void CacheAssetKeys(ObjectPoolManager poolManager)
    {
        _cachedAssetKeys.Clear();
        if (poolManager == null) return;

        List<string> allTags = poolManager.GetAllTags();
        for (int i = 0; i < allTags.Count; i++)
        {
            string tag = allTags[i];
            // ✅ Logic เดิม: เลือกเฉพาะ Tag ที่ขึ้นต้นด้วย Prefix ของด่านนั้นๆ
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith(_assetPrefix))
                _cachedAssetKeys.Add(tag);
        }

        if (_cachedAssetKeys.Count == 0)
            Debug.LogWarning($"[AssetSpawner] No asset keys found for prefix '{_assetPrefix}'");
    }

    #endregion

    #region Core Spawn Logic (Hybrid)

    /// <summary>
    /// ฟังก์ชันนี้ MapGenerator จะเป็นคนเรียก
    /// แต่ AssetSpawner มีสิทธิ์ปฏิเสธถ้ายังไม่ถึงเวลา (Cooldown)
    /// </summary>
    private GameObject SpawnAssetAt(Vector3 targetPos)
    {
        if (_pool == null || _cachedAssetKeys.Count == 0) return null;
        if (_pivot == null) return null;

        // 1. ✅ เช็ค Phase & Cooldown
        // ถ้าเวลายังไม่ถึงกำหนด ให้ปฏิเสธการสร้าง (MapGenerator เสนอมา แต่เราไม่เอา)
        if (Time.time < _nextSpawnAllowedTime) 
        {
            return null; 
        }

        // 2. ✅ Check Overlap (ฟิสิกส์)
        if (Physics2D.OverlapCircle(targetPos, _checkRadius, _obstacleLayer))
        {
            return null; // มีของขวาง
        }

        // 3. ✅ Raycast Down (หาพื้น)
        Vector2 rayOrigin = new Vector2(targetPos.x, targetPos.y + 2f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, _rayDistance, _groundLayer);

        if (hit.collider == null) return null; // ไม่เจอพื้น (เหว)

        Vector3 finalPos = hit.point;
        finalPos.y += _verticalOffset;

        // 4. ✅ Spawn Object
        int index = Random.Range(0, _cachedAssetKeys.Count);
        string key = _cachedAssetKeys[index];

        GameObject obj = _pool.SpawnFromPool(key, finalPos, Quaternion.identity);
        
        if (obj != null)
        {
            // Optional: Flip
            if (Random.value > 0.5f)
            {
                Vector3 s = obj.transform.localScale;
                s.x = -Mathf.Abs(s.x);
                obj.transform.localScale = s;
            }

            _activeAssets.Add(obj);

            // 5. ✅ คำนวณ Cooldown รอบถัดไป (ตาม Phase)
            float currentDist = Mathf.Max(0, _pivot.position.x - _startX);
            CalculateNextSpawnTime(currentDist);
        }

        return obj;
    }

    private void CalculateNextSpawnTime(float distance)
    {
        Vector2 interval;

        // เลือกช่วงเวลาตามระยะทาง (Phase)
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

    #region ISpawn Implementation

    // Hook ให้ MapGenerator เรียก
    public GameObject SpawnAtPosition(Vector3 position)
    {
        return SpawnAssetAt(position);
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null || _pool == null) return;
        _activeAssets.Remove(obj);
        _pool.ReturnToPool(obj.name.Replace("(Clone)", "").Trim(), obj);
    }

    public int GetSpawnCount() => _activeAssets.Count;
    public void Spawn() { } // ไม่ใช้

    #endregion

    #region Editor Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _checkRadius);
    }
    #endregion
}