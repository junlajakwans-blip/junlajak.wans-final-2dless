using System.Collections.Generic;
using UnityEngine;

public class ThrowableSpawner : MonoBehaviour, ISpawn, IInteractable
{
    [Header("Drop Table (Per Map)")]
    [SerializeField] private ThrowableDropTable _dropTable;

    // TODO : [Header("Distance Phases")] 
    // [SerializeField] private float _phase1End = 700f; ตอนแรกทำไว้แต่เทสยากเลยเอาไว้มาทำหลัง DEMO เสร็จ
    // TODO : [SerializeField] private float _phase2End = 1600f;


    // TODO : [Header("Drop Chance (Phase3)")]
    //[Tooltip("0–1 เช่น 0.15 = 15%")]
    //[SerializeField] private float _phase3DropChance = 0.15f;

    [Header("Placement Offset Y")]
    [Tooltip("Offset แนวตั้งสุดท้าย (ควรตั้งค่าใน MapGenerator)")]
    [SerializeField] private float _spawnYOffset = 0.5f;


    private Transform _pivot; // Player
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
        // FIX: ตรวจสอบ _pivot ก่อนเข้าถึง .position
        if (_pivot != null)
             _startX = _pivot.position.x; 

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

    private void HandleEnemyDied(Enemy enemy) // จัดการ position Enemy when die เพื่อ drop
    {
        enemy.OnEnemyDied -= HandleEnemyDied;

        if (_pivot == null) return; // Guard
        
        float distance = Mathf.Max(0f, _pivot.position.x - _startX);

        // Early game → ดรอปเยอะหน่อย เพื่อให้ Duckling ใช้สู้ได้
        if (distance < 600f)
        {
            if (Random.value < 0.40f)   // 40%
                SpawnThrowableAt(enemy.transform.position);
            return;
        }

        // ระยะไกล → ความถี่ลดลงแต่ยังเจอเรื่อยๆ
        if (Random.value < 0.18f)       // 18%
            SpawnThrowableAt(enemy.transform.position);

        
        Vector3 pos = enemy.transform.position;
        GameObject obj = SpawnThrowableAt(pos);

    }

    #endregion

    #region Spawn Core
    private GameObject SpawnThrowableAt(Vector3 receivedPos)
    {
        if (_pool == null ||
            _dropTable == null ||
            _dropTable.dropList == null ||
            _dropTable.dropList.Count == 0)
            return null;

        // 1. กำหนดตำแหน่งสุดท้าย (เชื่อถือตำแหน่งที่ส่งมา)
        Vector3 finalPos = receivedPos;
        finalPos.y += _spawnYOffset; // เพิ่ม Offset ให้ลอยเหนือจุดเกิด Enemy

        // 2. Spawn Slot Check (Asset/Collectible/Throwable ต้องไม่ทับกัน)
        if (!SpawnSlot.Reserve(finalPos))
        {
            Debug.LogWarning($"[ThrowableSpawner] Spawn Failed: Slot Reserved at X={finalPos.x:F1}.");
            return null;
        }

        string poolTag = GetWeightedTag();
        GameObject obj = _pool.SpawnFromPool(poolTag, finalPos, Quaternion.identity);
        
        if (obj == null) 
        {
            SpawnSlot.Unreserve(finalPos);
            return null;
        }
        
        // 3. Inject Info and Sprite
        var info = obj.GetComponent<ThrowableItemInfo>();
        var sr   = obj.GetComponent<SpriteRenderer>();

        if (info != null)
        {
            var entry = _dropTable.dropList.Find(x => x.poolTag == poolTag);
            if (entry != null)
            {
                // [FIX]: SetInfo จะกำหนด PoolTag และ Icon ให้ ThrowableItemInfo
                info.SetInfo(poolTag, entry.icon); 
                
                // [FIX]: ต้องใช้ SetSprite ของ ThrowableItemInfo เพื่อเปลี่ยน Sprite
                if (sr != null)
                    sr.sprite = entry.icon;
            }
        }
        
        // [FIX]: เพิ่มการลงทะเบียนวัตถุที่เกิดสำเร็จ
        _activeThrowables.Add(obj);
        
        
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
        
        
        // 1. ให้ Player เก็บของชิ้นนี้ (PlayerInteract จะ SetParent และทำให้ Scale เปลี่ยน)
        interact?.SetThrowable(gameObject);
        
        // 2. บอก ThrowableItemInfo ว่าถูกเก็บแล้ว
        // ThrowableItemInfo.DisablePhysicsOnHold() จะทำการตั้งค่า Scale ที่ถูกต้องให้เอง
        if (TryGetComponent<ThrowableItemInfo>(out var info))
            info.DisablePhysicsOnHold(); 

        
        // 3. ยกเลิกการจอง Slot เมื่อถูกเก็บ
        SpawnSlot.Unreserve(transform.position); 
    }

    public void ShowPrompt()
    {
        UIManager.Instance.ShowPrompt("Press E to pick up");
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
        if (obj == null || _pool == null) return;
        
        // 0. รีเซ็ต Local Scale ก่อนคืน Pool
        // โค้ดนี้ถูกคงไว้เพื่อความปลอดภัย แต่ OnReturnedToPool() ใน ThrowableItemInfo ก็ทำหน้าที่นี้แล้ว
        obj.transform.localScale = new Vector3(0.2f, 0.2f, 1f); 

        // 1. Unreserve Slot (ถ้ายังมีการจองอยู่)
        SpawnSlot.Unreserve(obj.transform.position);

        // 2. เรียก OnReturnedToPool ก่อนคืน (รวมถึงการรีเซ็ต Scale และ Unparent ใน ThrowableItemInfo)
        if (obj.TryGetComponent<ThrowableItemInfo>(out var info))
            info.OnReturnedToPool();
        
        // 3. Remove จาก List และ Return
        _activeThrowables.Remove(obj);

        // [FIX]: ใช้ GetObjectTag Helper (ตามที่ทำใน AssetSpawner) หรือใช้ name.Replace
        string key = obj.name.Replace("(Clone)", "").Trim(); 
        _pool.ReturnToPool(key, obj);
    }

    public int GetSpawnCount() => _activeThrowables.Count;
    #endregion
}