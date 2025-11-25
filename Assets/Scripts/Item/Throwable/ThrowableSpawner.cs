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
    [Tooltip("Offset แนวตั้งสุดท้าย (ควรตั้งค่าใน MapGenerator)")]
    [SerializeField] private float _spawnYOffset = 0.5f;

    // ⬅️ REMOVED: ไม่ใช้ Raycast แล้ว
    // [SerializeField] private LayerMask groundLayer; 

    private Transform _pivot;        // Player
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
        // ⚠️ FIX: ตรวจสอบ _pivot ก่อนเข้าถึง .position
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

    private void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnEnemyDied -= HandleEnemyDied;

        if (_pivot == null) return; // Guard
        
        float distance = Mathf.Max(0f, _pivot.position.x - _startX);
        
        if (distance < _phase1End) return;
        if (distance < _phase2End) return;
        if (Random.value > _phase3DropChance) return;

        // ⚠️ FIX: ตำแหน่ง Drop ต้องเชื่อถือว่า EnemySpawner ส่งมาเป็นตำแหน่งที่ถูกต้อง
        // แต่ในกรณีนี้เป็นการ Drop หลัง Enemy ตาย เราต้องคำนวณตำแหน่งเอง
        // เราจะส่งตำแหน่งที่ Enemy ตาย (+ Offset) ไปให้ SpawnThrowableAt(pos)
        
        // ⚠️ NOTE: การ drop จาก Enemy ตาย ควรใช้ SpawnSlot.Reserve 
        // แต่เราจะให้ SpawnThrowableAt จัดการ Reserve
        
        Vector3 pos = enemy.transform.position;
        GameObject obj = SpawnThrowableAt(pos);
        
        // ⚠️ REMOVED: การเพิ่ม obj เข้า _activeThrowables ถูกย้ายไปที่ SpawnThrowableAt
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
        // ⚠️ NOTE: การใช้ Raycast ถูกลบออกแล้ว
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
        var sr   = obj.GetComponent<SpriteRenderer>();

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
        
        // ⚠️ REMOVED: Debug.LogWarning Raycast ที่ไม่จำเป็น
        
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
        
        // ⚠️ FIX: ใช้ SetThrowable บน interact และ Despawn ตัวเอง
        
        // 1. ให้ Player เก็บของชิ้นนี้
        interact?.SetThrowable(gameObject);
        
        // 2. บอก ThrowableItemInfo ว่าถูกเก็บแล้ว
        if (TryGetComponent<ThrowableItemInfo>(out var info))
            info.DisablePhysicsOnHold(); // ใช้ DisablePhysicsOnHold เพื่อจัดการ physics และ interactable

        // 3. ❌ REMOVED: ไม่ควร Despawn ตัวเองทันทีที่ Interact เพราะ Player ยังถืออยู่
        // _activeThrowables.Remove(gameObject); // ถูกจัดการเมื่อ Despawn (ตอนโยน/คัดทิ้ง)
        
        // 4. ยกเลิกการจอง Slot เมื่อถูกเก็บ (ถือว่าไม่อยู่บนพื้นแล้ว)
        SpawnSlot.Unreserve(transform.position); 
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
        
        // ⚠️ FIX: ต้องส่งตำแหน่งที่ถูกต้อง (บนพื้น) ให้ SpawnThrowableAt
        // แต่เนื่องจาก Spawn ถูกใช้สำหรับสุ่มบน Pivot จึงควรใช้ SpawnAtPosition
        Vector3 pos = _pivot.position;
        SpawnAtPosition(pos);
    }

    public GameObject SpawnAtPosition(Vector3 position)
    {
        // ⚠️ FIX: เราไม่ควรเพิ่ม obj ใน _activeThrowables ซ้ำซ้อน 
        // SpawnThrowableAt จัดการแล้ว
        return SpawnThrowableAt(position); 
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null || _pool == null) return;
        
        // 1. Unreserve Slot (ถ้ายังมีการจองอยู่)
        SpawnSlot.Unreserve(obj.transform.position);

        // 2. เรียก OnReturnedToPool ก่อนคืน
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