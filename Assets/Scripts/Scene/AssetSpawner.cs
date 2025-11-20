using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn สิ่งกีดขวาง / กล่อง / ตู้ / อะไรก็ตามที่ผู้เล่นใช้ปีน
/// - ใช้ ObjectPoolManager + prefix หา prefab อัตโนมัติ
/// - ปรับโหมดตามระยะทาง:
///   Phase1: 0–700   → Mode A, interval ช้า
///   Phase2: 700–1600 → Mode A, interval เร็วขึ้น
///   Phase3: 1600+   → Hybrid (spawn ถี่สุด)
/// </summary>
public class AssetSpawner : MonoBehaviour, ISpawn
{
    [Header("Prefix Settings")]
    [Tooltip("เช่น map_asset_School_ / map_asset_RoadTraffic_ / map_asset_Kitchen_")]
    [SerializeField] private string _assetPrefix = "map_asset_School_";

    [Header("Distance Phases (x-axis distance from start)")]
    [SerializeField] private float _phase1End = 700f;   // 0–700
    [SerializeField] private float _phase2End = 1600f;  // 700–1600, หลังจากนั้นคือ Phase3

    [Header("Spawn Interval (seconds)")]
    [Tooltip("ช่วงเวลา spawn สำหรับ Phase1 (สุ่มระหว่าง min–max)")]
    [SerializeField] private Vector2 _phase1Interval = new Vector2(6f, 9f);

    [Tooltip("ช่วงเวลา spawn สำหรับ Phase2")]
    [SerializeField] private Vector2 _phase2Interval = new Vector2(4f, 7f);

    [Tooltip("ช่วงเวลา spawn สำหรับ Phase3 (ไกลสุด, จะถี่สุด)")]
    [SerializeField] private Vector2 _phase3Interval = new Vector2(3f, 5f);

    [Header("Placement")]
    [Tooltip("ระยะห่างจากผู้เล่นไปข้างหน้าที่จะวาง asset (ตามแกน X)")]
    [SerializeField] private float _spawnOffsetX = 8f;

    [Tooltip("แกน Y สำหรับวางฐานของ asset (เอาให้ผู้เล่นปีนได้)")]
    [SerializeField] private float _spawnY = 0.6f;

    [Tooltip("สุ่ม offset Y เพิ่มเล็กน้อยเพื่อให้ไม่เรียบเกินไป")]
    [SerializeField] private float _randomYOffset = 0.3f;

    [Header("Runtime Debug")]
    [SerializeField] private bool _autoSpawn = true;
    [SerializeField] private List<string> _cachedAssetKeys = new List<string>();
    [SerializeField] private List<GameObject> _activeAssets = new List<GameObject>();

    // references
    private IObjectPool _pool;
    private Transform _pivot;        // ปกติ = Player
    private float _startX;
    private float _nextSpawnTime;

    #region Initialization

    /// <summary>
    /// เรียกจาก MapGeneratorX หลังจาก InitializeGenerators เสร็จ
    /// </summary>
    public void Initialize(Transform pivot)
    {
        _pivot = pivot;
        if (_pivot != null)
            _startX = _pivot.position.x;

        var poolManager = ObjectPoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogError("[AssetSpawner] ObjectPoolManager.Instance is null!");
            return;
        }

        _pool = poolManager;
        CacheAssetKeys(poolManager);

        ScheduleNextSpawn(); // ตั้งเวลาสปาวรอบแรก

        Debug.Log($"[AssetSpawner] Initialized. Cached {_cachedAssetKeys.Count} asset keys with prefix '{_assetPrefix}'.");
    }

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
        {
            Debug.LogWarning($"[AssetSpawner] No asset keys found with prefix '{_assetPrefix}'.");
        }
    }

    #endregion

    private void Update()
    {
        if (!_autoSpawn) return;
        if (_pool == null || _pivot == null) return;
        if (_cachedAssetKeys.Count == 0) return;

        float distance = _pivot.position.x - _startX;
        if (distance < 0f) distance = 0f;

        if (Time.time >= _nextSpawnTime)
        {
            SpawnByDistance(distance);
            ScheduleNextSpawn(distance);
        }
    }

    #region Core Spawn

    private void SpawnByDistance(float distance)
    {
        // ตอนนี้ใช้ pattern แบบเดียวทั้ง 3 Phase
        // แตกต่างแค่ interval ที่ตั้งใน ScheduleNextSpawn()
        Vector3 basePos = _pivot.position;
        float x = basePos.x + _spawnOffsetX;
        float y = _spawnY + Random.Range(-_randomYOffset, _randomYOffset);

        Vector3 spawnPos = new Vector3(x, y, 0f);

        GameObject obj = SpawnAssetAt(spawnPos);
        if (obj != null)
        {
            _activeAssets.Add(obj);
        }
    }

    private void ScheduleNextSpawn(float distance = 0f)
    {
        Vector2 interval;

        if (distance < _phase1End)
        {
            interval = _phase1Interval;
        }
        else if (distance < _phase2End)
        {
            interval = _phase2Interval;
        }
        else
        {
            // Phase3 → Hybrid (H3) ให้สั้นสุดหน่อย
            interval = _phase3Interval;
        }

        float min = Mathf.Max(0.1f, interval.x);
        float max = Mathf.Max(min, interval.y);

        _nextSpawnTime = Time.time + Random.Range(min, max);
    }

    private GameObject SpawnAssetAt(Vector3 position)
    {
        if (_pool == null || _cachedAssetKeys.Count == 0) return null;

        int index = Random.Range(0, _cachedAssetKeys.Count);
        string key = _cachedAssetKeys[index];

        GameObject obj = _pool.SpawnFromPool(key, position, Quaternion.identity);
        if (obj == null)
        {
            Debug.LogWarning($"[AssetSpawner] Failed to spawn asset with key '{key}'.");
        }

        return obj;
    }

    #endregion

    #region ISpawn Implementation (minimal)

    public void Spawn()
    {
        if (_pivot == null) return;
        float distance = _pivot.position.x - _startX;
        if (distance < 0f) distance = 0f;

        SpawnByDistance(distance);
    }

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

    public int GetSpawnCount()
    {
        return _activeAssets.Count;
    }

    #endregion
}
