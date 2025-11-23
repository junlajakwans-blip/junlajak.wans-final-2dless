using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ดรอปของปา (Throwable) จากศัตรูตามระยะทาง
/// - ใช้ EnemySpawner.OnEnemySpawned → สมัคร event OnEnemyDied ของแต่ละตัว
/// - Phase1: 0–700   → ไม่ดรอป
/// - Phase2: 700–1600 → ยังไม่ดรอป (หรือจะเปิดบางส่วนก็ปรับได้)
/// - Phase3: 1600+   → 15% โอกาสดรอป (H3)
/// </summary>
public class ThrowableSpawner : MonoBehaviour, ISpawn ,IInteractable
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

    [SerializeField] private string _throwableKey;
    [SerializeField] private GameObject _displayPrefab;

    [Header("Runtime Debug")]
    [SerializeField] private List<string> _cachedThrowableKeys = new List<string>();
    [SerializeField] private List<GameObject> _activeThrowables = new List<GameObject>();

    // references
    private IObjectPool _pool;
    private Transform _pivot;       // ปกติ = Player
    private float _startX;

    private EnemySpawner _enemySpawner;

    private bool _canInteract = false;
    public bool CanInteract => _canInteract;

    #region Initialization

    /// <summary>
    /// เรียกจาก MapGeneratorX
    /// </summary>
    public void Initialize(Transform pivot, EnemySpawner enemySpawner = null)
    {
        _pivot = pivot;
        _startX = _pivot != null ? _pivot.position.x : 0f;

        var poolManager = ObjectPoolManager.Instance;
        if (poolManager == null) return;

        _pool = poolManager;
        CacheThrowableKeys(poolManager);

        _enemySpawner = enemySpawner ?? FindFirstObjectByType<EnemySpawner>();
        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned += HandleEnemySpawned;

        // สำหรับ Interact
        _canInteract = true;
    }


    private void CacheThrowableKeys(ObjectPoolManager poolManager)
    {
        _cachedThrowableKeys.Clear();
        foreach (var tag in poolManager.GetAllTags())
            if (tag.StartsWith(_throwablePrefix))
                _cachedThrowableKeys.Add(tag);
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
        enemy.OnEnemyDied -= HandleEnemyDied;

        float distance = Mathf.Max(0f, _pivot.position.x - _startX);

        if (distance < _phase2End) return;
        if (Random.value > _phase3DropChance) return;

        Vector3 pos = enemy.transform.position;
        pos.y += _spawnYOffset;

        GameObject obj = SpawnThrowableAt(pos);
        if (obj != null) _activeThrowables.Add(obj);
    }
    #endregion

    #region Spawn Core
    private GameObject SpawnThrowableAt(Vector3 position)
    {
        if (_pool == null || _cachedThrowableKeys.Count == 0) return null;

        string key = _cachedThrowableKeys[Random.Range(0, _cachedThrowableKeys.Count)];
        return _pool.SpawnFromPool(key, position, Quaternion.identity);
    }
    #endregion


    #region ISpawn Implementation (not heavily used but for interface consistency)

    public void Spawn()
    {
        // normally not used (ดรอปจากศัตรูเป็นหลัก)
        if (_pivot == null) return;
        Vector3 pos = _pivot.position + Vector3.up * _spawnYOffset;
        GameObject obj = SpawnThrowableAt(pos);
        if (obj != null) _activeThrowables.Add(obj);
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        GameObject obj = SpawnThrowableAt(position);
        if (obj != null) _activeThrowables.Add(obj);
        return obj;
    }
    #endregion

    public void Despawn(GameObject obj)
    {
        if (obj == null || _pool == null) return;

        _activeThrowables.Remove(obj);
        _pool.ReturnToPool(obj.name.Replace("(Clone)", "").Trim(), obj);
    }

    public int GetSpawnCount() => _activeThrowables.Count;

    #region IInteractable
    public void Interact(Player player)
    {
        if (!CanInteract || player == null) return;

        var interact = player.GetComponentInChildren<PlayerInteract>();

        Debug.Log($"[ThrowableSpawner] Player picked up {_throwableKey}");

        interact.SetThrowable(gameObject); // ส่งข้อมูลให้ระบบ PlayerInteract
        _canInteract = false;

        Despawn(gameObject);
    }

    
    public void ShowPrompt()
    {
        UIManager.Instance.ShowPrompt("Press E to pick up");
    }

    #endregion
}
