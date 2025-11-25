using System.Collections.Generic;
using UnityEngine;

public class ThrowableSpawner : MonoBehaviour, ISpawn, IInteractable
{
    [Header("Drop Table (Per Map)")]
    [SerializeField] private ThrowableDropTable _dropTable;

    [Header("Distance Phases")]
    [SerializeField] private float _phase1End = 700f;
    [SerializeField] private float _phase2End = 1600f;

    [Header("Drop Chance (Phase3)")]
    [Tooltip("0–1 เช่น 0.15 = 15%")]
    [SerializeField] private float _phase3DropChance = 0.15f;

    [Header("Placement Offset Y")]
    [SerializeField] private float _spawnYOffset = 0.5f;

    [SerializeField] private LayerMask groundLayer;

    private Transform _pivot;        // Player
    private float _startX;
    private IObjectPool _pool;
    private EnemySpawner _enemySpawner;

    private bool _canInteract = false;
    public bool CanInteract => _canInteract;

    // รายการของที่ spawn อยู่บนพื้น (ไม่รวมที่อยู่บนหัวผู้เล่น)
    [SerializeField] private List<GameObject> _activeThrowables = new();

    #region Initialization
    public void Initialize(Transform pivot, EnemySpawner enemySpawner = null)
    {
        _pivot = pivot;
        _startX = pivot.position.x;

        _pool = ObjectPoolManager.Instance;
        _enemySpawner = enemySpawner ?? FindFirstObjectByType<EnemySpawner>();

        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned += HandleEnemySpawned;

        _canInteract = true;
    }

    private void OnDisable()
    {
        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned -= HandleEnemySpawned;
    }
    #endregion

    #region Enemy Events → Drop Logic

    private void HandleEnemySpawned(Enemy enemy)
    {
        enemy.OnEnemyDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnEnemyDied -= HandleEnemyDied;

        float distance = Mathf.Max(0f, _pivot.position.x - _startX);
        if (distance < _phase1End) return;
        if (distance < _phase2End) return;
        if (Random.value > _phase3DropChance) return;

        Vector3 pos = enemy.transform.position + Vector3.up * _spawnYOffset;
        GameObject obj = SpawnThrowableAt(pos);
        if (obj != null) _activeThrowables.Add(obj);
    }

    #endregion

    #region Spawn Core
    private GameObject SpawnThrowableAt(Vector3 pos)
    {
        if (_pool == null ||
            _dropTable == null ||
            _dropTable.dropList == null ||
            _dropTable.dropList.Count == 0)
            return null;

        // ยิง Ray ลงหา Ground Layer เพื่อตกบนพื้นเท่านั้น
        RaycastHit2D hit = Physics2D.Raycast(
            pos + Vector3.up * 1f,   // เริ่มยิงจากด้านบน
            Vector2.down,
            15f,
            groundLayer
        );

        Debug.DrawRay(pos + Vector3.up * 1f, Vector2.down * 5f, hit.collider != null ? Color.green : Color.red, 2f);


        if (hit.collider != null)
        {
            pos = new Vector3(pos.x, hit.point.y + _spawnYOffset, pos.z);
        }
        else
        {
            // ถ้าไม่เจอพื้นก็ไม่ spawn — ป้องกันลอยกลางอากาศ
            return null;
        }

        string poolTag = GetWeightedTag();
        GameObject obj = _pool.SpawnFromPool(poolTag, pos, Quaternion.identity);
        if (obj == null) return null;

        var info = obj.GetComponent<ThrowableItemInfo>();
        var sr   = obj.GetComponent<SpriteRenderer>();

        if (info != null)
        {
            var entry = _dropTable.dropList.Find(x => x.poolTag == poolTag);
            if (entry != null)
            {
                info.SetInfo(poolTag, entry.icon);   // ใช้แบบเดิม
                if (sr != null)
                    sr.sprite = entry.icon;
            }
        }
        Debug.LogWarning("[ThrowableSpawner] Raycast did not hit the Ground Layer at position: " + (pos + Vector3.up * 1f));
        return obj;
    }



    private string GetWeightedTag()
    {
        float total = 0f;
        foreach (var e in _dropTable.dropList) total += e.weight;

        float r = Random.value * total;
        foreach (var e in _dropTable.dropList)
        {
            r -= e.weight;
            if (r <= 0f) return e.poolTag;
        }
        return _dropTable.dropList[_dropTable.dropList.Count - 1].poolTag;
    }
    #endregion

    #region IInteractable — Pick Up
    public void Interact(Player player)
    {
        var interact = player.GetComponentInChildren<PlayerInteract>();
        interact?.SetThrowable(gameObject);

        if (TryGetComponent<ThrowableItemInfo>(out var info))
            info.SetInteractable(false);

        _activeThrowables.Remove(gameObject);
    }

    public void ShowPrompt()
    {
        UIManager.Instance.ShowPrompt("Press W to pick up");
    }
    #endregion

    #region ISpawn Implementation
    public void Spawn()
    {
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

    public void Despawn(GameObject obj)
    {
        if (obj == null || _pool == null) return;
        _activeThrowables.Remove(obj);

        string key = obj.name.Replace("(Clone)", "").Trim();
        _pool.ReturnToPool(key, obj);
    }

    public int GetSpawnCount() => _activeThrowables.Count;
    #endregion
}
