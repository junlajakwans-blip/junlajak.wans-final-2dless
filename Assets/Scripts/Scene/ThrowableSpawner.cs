using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ดรอปของปา (Throwable) จากศัตรูตามระยะทาง
/// - ใช้ EnemySpawner.OnEnemySpawned → สมัคร event OnEnemyDied ของแต่ละตัว
/// - Phase1: 0–700   → ไม่ดรอป
/// - Phase2: 700–1600 → ยังไม่ดรอป (หรือจะเปิดบางส่วนก็ปรับได้)
/// - Phase3: 1600+   → 15% โอกาสดรอป (H3)
/// </summary>
public class ThrowableSpawner : MonoBehaviour, ISpawn
{
    [Header("Prefix Settings")]
    [Tooltip("เช่น map_ThrowItem_School_ / map_ThrowItem_RoadTraffic_ / map_ThrowItem_Kitchen_")]
    [SerializeField] private string _throwablePrefix = "map_ThrowItem_School_";

    [Header("Distance Phases")]
    [SerializeField] private float _phase1End = 700f;
    [SerializeField] private float _phase2End = 1600f;

    [Header("Drop Chance (Phase3)")]
    [Tooltip("โอกาสดรอปใน Phase3 (0–1) เช่น 0.15 = 15%")]
    [SerializeField] private float _phase3DropChance = 0.15f;

    [Header("Placement")]
    [Tooltip("ยกของปาจากพื้นขึ้นตามแกน Y เล็กน้อยจากตำแหน่งศัตรู")]
    [SerializeField] private float _spawnYOffset = 0.5f;

    [Header("Runtime Debug")]
    [SerializeField] private List<string> _cachedThrowableKeys = new List<string>();
    [SerializeField] private List<GameObject> _activeThrowables = new List<GameObject>();

    // references
    private IObjectPool _pool;
    private Transform _pivot;       // ปกติ = Player
    private float _startX;

    private EnemySpawner _enemySpawner;

    #region Initialization

    /// <summary>
    /// เรียกจาก MapGeneratorX
    /// </summary>
    public void Initialize(Transform pivot, EnemySpawner enemySpawner = null)
    {
        _pivot = pivot;
        if (_pivot != null)
            _startX = _pivot.position.x;

        var poolManager = ObjectPoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogError("[ThrowableSpawner] ObjectPoolManager.Instance is null!");
            return;
        }

        _pool = poolManager;
        CacheThrowableKeys(poolManager);

        _enemySpawner = enemySpawner ?? FindFirstObjectByType<EnemySpawner>();
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemySpawned += HandleEnemySpawned;
        }

        Debug.Log($"[ThrowableSpawner] Initialized. Cached {_cachedThrowableKeys.Count} keys with prefix '{_throwablePrefix}'.");
    }

    private void CacheThrowableKeys(ObjectPoolManager poolManager)
    {
        _cachedThrowableKeys.Clear();
        if (poolManager == null) return;

        List<string> allTags = poolManager.GetAllTags();
        for (int i = 0; i < allTags.Count; i++)
        {
            string tag = allTags[i];
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith(_throwablePrefix))
                _cachedThrowableKeys.Add(tag);
        }

        if (_cachedThrowableKeys.Count == 0)
        {
            Debug.LogWarning($"[ThrowableSpawner] No throwable keys found with prefix '{_throwablePrefix}'.");
        }
    }

    private void OnDisable()
    {
        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned -= HandleEnemySpawned;
    }

    #endregion

    #region Enemy Events

    private void HandleEnemySpawned(Enemy enemy)
    {
        if (enemy == null) return;
        // สมัครกับ event ตายของศัตรูแต่ละตัว
        enemy.OnEnemyDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null) return;

        // cleanup subscription เผื่อ enemy กลับเข้า pool แล้ว
        enemy.OnEnemyDied -= HandleEnemyDied;

        if (_pivot == null || _pool == null) return;
        if (_cachedThrowableKeys.Count == 0) return;

        float distance = _pivot.position.x - _startX;
        if (distance < 0f) distance = 0f;

        // Phase1 / Phase2 → ไม่ดรอป
        if (distance < _phase2End) return;

        // Phase3 → ใช้โอกาสดรอป 15%
        if (Random.value > _phase3DropChance) return;

        Vector3 pos = enemy.transform.position;
        pos.y += _spawnYOffset;

        GameObject obj = SpawnThrowableAt(pos);
        if (obj != null)
            _activeThrowables.Add(obj);
    }

    #endregion

    #region Core Spawn

    private GameObject SpawnThrowableAt(Vector3 position)
    {
        if (_pool == null || _cachedThrowableKeys.Count == 0) return null;

        int index = Random.Range(0, _cachedThrowableKeys.Count);
        string key = _cachedThrowableKeys[index];

        GameObject obj = _pool.SpawnFromPool(key, position, Quaternion.identity);
        if (obj == null)
        {
            Debug.LogWarning($"[ThrowableSpawner] Failed to spawn throwable with key '{key}'.");
        }

        return obj;
    }

    #endregion

    #region ISpawn Implementation (not heavily used but for interface consistency)

    public void Spawn()
    {
        // normally not used (ดรอปจากศัตรูเป็นหลัก)
        if (_pivot == null) return;
        Vector3 pos = _pivot.position + Vector3.up * _spawnYOffset;
        GameObject obj = SpawnThrowableAt(pos);
        if (obj != null)
            _activeThrowables.Add(obj);
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        GameObject obj = SpawnThrowableAt(position);
        if (obj != null)
            _activeThrowables.Add(obj);
        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null || _pool == null) return;

        _activeThrowables.Remove(obj);
        _pool.ReturnToPool(obj.name.Replace("(Clone)", "").Trim(), obj);
    }

    public int GetSpawnCount()
    {
        return _activeThrowables.Count;
    }

    #endregion
}
