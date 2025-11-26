using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class สำหรับทุก Map (แก้ไข Logic การ Spawn ให้เป็น Frontier-based)
/// จัดระเบียบ Region แล้ว
/// </summary>
public abstract class MapGeneratorBase : MonoBehaviour
{
    // ============================================================================
    // 1. REFERENCES & SETTINGS
    // ============================================================================
    #region Spawner References
    [Header("Spawner References")]
    [SerializeField] protected EnemySpawner _enemySpawner;
    [SerializeField] protected CollectibleSpawner _collectibleSpawner;
    [SerializeField] protected BackgroundLooper _backgroundLooper;
    [SerializeField] protected AssetSpawner _assetSpawner;          
    [SerializeField] protected ThrowableSpawner _throwableSpawner;  
    protected ObjectPoolManager _objectPoolManager;
    #endregion

    #region Generation Settings
    [Header("Basic Settings")]
    [Tooltip("จุดเริ่มต้นสำหรับ Spawn Platform / Floor")]
    [SerializeField] protected Vector2 _spawnStartPosition = new Vector2(0f, 0.2f);

    [Tooltip("จำนวน Platform ที่ Active ได้สูงสุด (กันล้น)")]
    [SerializeField] protected int _maxPlatformCount = 20;

    [Tooltip("Pivot สำหรับเช็คระยะทาง (โดยปกติใช้ Player)")]
    [SerializeField] protected Transform _generationPivot;
    #endregion

    #region Platform Settings
    [Header("Platform Endless Settings")]
    [SerializeField] protected float _platformWidth = 10f;
    [SerializeField] protected float _minXOffset = 2f;
    [SerializeField] protected float _maxXOffset = 4f;
    [SerializeField] protected float _minYOffset = -1f;
    [SerializeField] protected float _maxYOffset = 1.5f;

    // กำหนดค่า Offset ใหม่ตรงนี้เพื่อให้ Collectible ลอยอยู่บน Platform พอดี
    [SerializeField] protected float _collectibleOffset = 0.5f;

    protected float _nextSpawnX; //  Cursor สำคัญ: บอกตำแหน่งขวาสุดที่สร้าง Platform ไปแล้ว
    protected float _nextFloorX; //  Cursor สำคัญ: บอกตำแหน่งขวาสุดที่สร้างพื้นหลัง (Floor) ไปแล้ว

    // State สำหรับควบคุมการสุ่ม Y Platform
    protected enum PlatformState 
    { 
        Normal, 
        AscendingSteps, 
        DescendingSteps, 
        HillUp, 
        HillDown 
    }
    [SerializeField] private PlatformState _currentPlatformState = PlatformState.Normal;
    //TODO : [SerializeField] private float _currentHeightLimit = 0f; // ใช้จำกัดความสูงสูงสุดใน Pattern Hill
    [SerializeField] private int _stepsRemaining = 0; // ใช้สำหรับ Pattern Steps
    #endregion

    #region Floor Settings
    [Header("Floor Settings (Tile 1 UNIT)")]
    [SerializeField] protected float _floorY = 0.2f;
    [SerializeField] protected float _floorLength = 1f;
    [SerializeField] protected int _initialFloorSegments = 30;
    #endregion

    #region Wall Settings
    [Header("Wall Control")]
    [SerializeField] protected Transform _endlessWall;
    [SerializeField] protected float _baseWallPushSpeed = 1.0f;
    #endregion

    // ============================================================================
    // 2. RUNTIME STATE (ตัวแปรที่เปลี่ยนค่าตลอดเวลา)
    // ============================================================================
    #region Runtime State
    [Header("Runtime Debug")]
    [SerializeField] protected List<GameObject> _activePlatforms = new List<GameObject>();
    [SerializeField] protected List<GameObject> _activeFloors = new List<GameObject>();

    private float _wallPushSpeed;
    private bool _isPlatformBreakable = true;
    private bool _isWallPushEnabled = true;
    #endregion

    // ============================================================================
    // 3. ABSTRACT & PROPERTIES
    // ============================================================================
    #region Abstract Keys
    protected abstract string NormalPlatformKey { get; }
    protected abstract string BreakPlatformKey { get; }
    protected abstract string FloorKey { get; }
    #endregion

    #region Public Properties
    public float WallPushSpeed { get => _wallPushSpeed; set => _wallPushSpeed = value; }
    public bool IsPlatformBreakable { get => _isPlatformBreakable; set => _isPlatformBreakable = value; }
    public bool IsWallPushEnabled { get => _isWallPushEnabled; set => _isWallPushEnabled = value; }
    #endregion

    // ============================================================================
    // 4. INITIALIZATION
    // ============================================================================
    #region Initialization
    public virtual void InitializeGenerators(Transform pivot = null)
    {
        _objectPoolManager = ObjectPoolManager.Instance;
        if (_objectPoolManager == null)
        {
            Debug.LogError("MapGeneratorBase: ObjectPoolManager.Instance is NULL!");
            return;
        }

        if (pivot != null) _generationPivot = pivot;
        else if (_generationPivot == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null) _generationPivot = player.transform;
        }

        _wallPushSpeed = _baseWallPushSpeed;
        _isPlatformBreakable = true;
        _isWallPushEnabled = true;

        
        // หา dependency อื่น ๆ ที่ CollectibleSpawner ต้องใช้
        var culling     = FindFirstObjectByType<DistanceCulling>();
        var cardManager = FindFirstObjectByType<CardManager>();
        var buffManager = FindFirstObjectByType<BuffManager>();

        // Inject ให้ CollectibleSpawner (สำคัญสุด)
        if (_collectibleSpawner != null)
        {
            _collectibleSpawner.InitializeSpawner(
                _objectPoolManager,
                culling,
                cardManager,
                buffManager
            );

            Debug.Log("[MapGeneratorBase] CollectibleSpawner initialized.");
        }

        // ถ้าอยากให้ Enemy / Asset / Throwable ใช้ Pool ด้วย ก็ใส่เพิ่มแบบนี้ได้
        /*
        if (_enemySpawner != null)
            _enemySpawner.InitializeSpawner(_objectPoolManager, culling);

        if (_assetSpawner != null)
            _assetSpawner.InitializeSpawner(_objectPoolManager, culling);

        if (_throwableSpawner != null)
            _throwableSpawner.InitializeSpawner(_objectPoolManager, culling, cardManager, buffManager);
        */
    
    }

    protected void InitializePlatformGeneration()
    {
        _nextSpawnX = _spawnStartPosition.x;
        _nextFloorX = _spawnStartPosition.x;

        // สร้าง Platform ชุดแรกแบบ Frontier (ถมให้เต็มหน้าจอ)
        SpawnInitialFloors();
        
        // สร้าง Platform เริ่มต้น
        // เปลี่ยนมาใช้ Loop แบบ Frontier เลย เพื่อความชัวร์
        float startFrontier = _spawnStartPosition.x + 30f; 
        while (_nextSpawnX < startFrontier)
        {
            SpawnNextPlatform(true);
        }

        StartCoroutine(GeneratePlatformsLoop());
    }

    // Abstract Entry Point
    public abstract void GenerateMap();
    
    // Virtual Hooks
    public virtual void SetupBackground() { }
    public virtual void SetupFloor() { SpawnInitialFloors(); }
    public virtual void SpawnEnemies() { }
    public virtual void SpawnCollectibles() { }
    public virtual void SpawnAssets() { }
    public virtual void SpawnThrowables() { }
    #endregion

    // ============================================================================
    // 5. CORE LOGIC (FRONTIER LOOP)
    // ============================================================================
    #region Core Loop
    //  CORE LOOP (FIXED): ใช้ Frontier Logic (ถมของให้เต็มหน้าจอเสมอ)
    protected IEnumerator GeneratePlatformsLoop()
    {
        while (_generationPivot != null)
        {
            // 1. คำนวณ "เส้นขอบฟ้า" (Frontier) ที่เราต้องวางของไปให้ถึง
            float frontierX = _generationPivot.position.x + 25f;

            // 2. ถม Platform ให้ถึงเส้น Frontier
            while (_nextSpawnX < frontierX)
            {
                SpawnNextPlatform(false);
            }

            // 3. ถม Floor ให้ถึงเส้น Frontier (ถ้ามี)
            if (!string.IsNullOrEmpty(FloorKey))
            {
                while (_nextFloorX < frontierX)
                {
                    SpawnFloorSegment();
                }
            }

            // 4. ลบของเก่าที่หลุดจอซ้าย
            RecycleOffScreenPlatforms();
            RecycleOffScreenFloors();
            
            // 5. อัปเดตกำแพง
            WallUpdate();

            // เช็คทุกๆ 0.1 วินาที (10 FPS Check) ก็พอ ประหยัด CPU
            yield return new WaitForSeconds(0.1f);
        }
    }
    #endregion

    // ============================================================================
    // 6. PLATFORM GENERATION
    // ============================================================================
    #region Platform Logic
    protected void SpawnNextPlatform(bool isStarter)
    {
        if (_objectPoolManager == null) return;
        
        // เลือก Key (Breakable หรือ Normal)
        string key = NormalPlatformKey;
        if (!isStarter && Random.value < 0.2f && BreakPlatformKey != "") 
            key = BreakPlatformKey;

        GameObject platform = _objectPoolManager.SpawnFromPool(key, Vector3.zero, Quaternion.identity);
        if (platform == null) return;

        // คำนวณตำแหน่ง
        Vector3 spawnPos;
        if (isStarter)
        {
            spawnPos = new Vector3(_nextSpawnX, _spawnStartPosition.y, 0f);
            _nextSpawnX += _platformWidth; // ขยับ Cursor ไปข้างหน้า
        }
        else
        {
            // สุ่มระยะห่างจากอันเก่า
            float xOffset = Random.Range(_minXOffset, _maxXOffset);
            float yOffset = 0f; // เปลี่ยนเป็น 0f และให้ Logic ใหม่คำนวณแทน

            _nextSpawnX += xOffset; // ขยับ Cursor (ช่องว่าง)

            // อิง Y จากอันล่าสุด
            float baseY = _spawnStartPosition.y;
            if (_activePlatforms.Count > 0)
            {
                var last = _activePlatforms[_activePlatforms.Count - 1];
                if (last != null) baseY = last.transform.position.y;
            }
            
            // =======================================================
            // State Machine คำนวณ Y-Offset
            yOffset = CalculateYOffsetByState(baseY);
            // =======================================================

            spawnPos = new Vector3(_nextSpawnX, baseY + yOffset, 0f);
            _nextSpawnX += _platformWidth; // ขยับ Cursor (ความกว้าง Platform)
        }

        platform.transform.position = spawnPos;
        platform.transform.SetParent(transform);
        platform.SetActive(true);
        _activePlatforms.Add(platform);

        // สั่ง Spawn ของบน Platform นี้ทันที
        if (!isStarter)
        {
            TrySpawnContentOnPlatform(platform, spawnPos, _platformWidth);
        }
    }

protected float CalculateYOffsetByState(float currentBaseY)
{
    float yOffset = 0f;
    float maxDeltaY = 0.45f; // การขึ้นลงสูงสุดต่อแพลตฟอร์ม

    float minY = _floorY + 0.25f; // ระดับต่ำสุดที่ยอมให้แพลตฟอร์มอยู่ (กันมุด)
    float maxY = _floorY + 2.8f;  // ระดับสูงสุด (ให้ผู้เล่นกระโดดถึง)

    // -------------------------
    // เปลี่ยน Pattern เมื่อจบ Step
    // -------------------------
    if (_stepsRemaining <= 0)
    {
        float r = Random.value;
        if (r < 0.70f) _currentPlatformState = PlatformState.Normal;
        else if (r < 0.80f) _currentPlatformState = PlatformState.AscendingSteps;
        else if (r < 0.90f) _currentPlatformState = PlatformState.DescendingSteps;
        else if (r < 0.95f) _currentPlatformState = PlatformState.HillUp;
        else _currentPlatformState = PlatformState.HillDown;

        _stepsRemaining = Random.Range(3, 6);
    }

    // -------------------------
    // คำนวณ Offset ตาม Pattern
    // -------------------------
    switch (_currentPlatformState)
    {
        case PlatformState.Normal:
            yOffset = Random.Range(-0.05f, 0.05f);
            break;

        case PlatformState.AscendingSteps:
            yOffset = maxDeltaY;
            break;

        case PlatformState.DescendingSteps:
            yOffset = -maxDeltaY;
            break;

        case PlatformState.HillUp:
            yOffset = Random.Range(0.1f, maxDeltaY);
            break;

        case PlatformState.HillDown:
            yOffset = Random.Range(-maxDeltaY, -0.1f);
            break;
    }

    _stepsRemaining--;

    float candidateY = currentBaseY + yOffset;

    // -------------------------
    //  Anti-Clipping Logic
    // -------------------------
    if (candidateY < minY)
    {
        candidateY = minY;
        _currentPlatformState = PlatformState.AscendingSteps; // บังคับให้ขึ้นต่อ
    }
    else if (candidateY > maxY)
    {
        candidateY = maxY;
        _currentPlatformState = PlatformState.DescendingSteps; // บังคับให้ลงต่อ
    }

    return candidateY - currentBaseY;
}


    protected void RecycleOffScreenPlatforms()
    {
        if (_generationPivot == null) return;
        float threshold = _generationPivot.position.x - 20f; 

        for (int i = _activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject p = _activePlatforms[i];
            if (p == null) { _activePlatforms.RemoveAt(i); continue; }

            if (p.transform.position.x < threshold)
            {
                _activePlatforms.RemoveAt(i);
                _objectPoolManager.ReturnToPool(GetObjectTag(p), p);
            }
        }
    }

    public virtual void BreakRightmostPlatform()
    {
        if (_objectPoolManager == null || !_isPlatformBreakable) return;
        if (_activePlatforms == null || _activePlatforms.Count == 0) return;

        int index = -1;
        float maxX = float.MinValue;

        for (int i = 0; i < _activePlatforms.Count; i++)
        {
            if (_activePlatforms[i] == null) continue;
            float x = _activePlatforms[i].transform.position.x;
            if (x > maxX) { maxX = x; index = i; }
        }

        if (index >= 0)
        {
            GameObject rightmost = _activePlatforms[index];
            _activePlatforms.RemoveAt(index);
            _objectPoolManager.ReturnToPool(GetObjectTag(rightmost), rightmost);
        }
    }
    #endregion

#region Content Spawning
protected virtual void TrySpawnContentOnPlatform(GameObject platform, Vector3 pos, float width)
{
    //  NEW FIX: ตรวจสอบ Tag ของ Platform ที่ส่งมา ต้องเป็นหนึ่งใน Tag ที่อนุญาต
    string platformTag = platform.tag;
    if (platformTag != "Floor" && 
        platformTag != "Platform" && 
        platformTag != "BreakPlatform")
    {
        // ถ้า Tag ไม่อยู่ในรายการที่อนุญาต ให้หยุดการสปาวน์เนื้อหา
        Debug.Log($"[MapGen] Skipped content spawn: Invalid Platform Tag: {platformTag}");
        return; 
    }
    
    float chance = Random.value;

    // 1. คำนวณจุด Spawn ที่ถูกต้องบน Platform
    // (สมมติว่า Platform มีความสูง 1 หน่วย และ pos คือจุดกึ่งกลาง Y)
    float platformTopY = pos.y + 0.5f; 

    // ตำแหน่ง Collectible ที่อยู่ "บน" Platform 
    Vector3 collectibleSpawnPos = new Vector3(
        pos.x, 
        platformTopY + _collectibleOffset, // ใช้ _collectibleOffset ที่กำหนดไว้ใน MapGeneratorBase
        0f
    );
    
    // 2. จุด Center ของ Platform (สำหรับ Enemy)
    Vector3 platformCenter = new Vector3(pos.x, pos.y, 0f); 

    if (chance < 0.3f && _collectibleSpawner != null)
    {
        // 30% เกิด Collectible
        _collectibleSpawner.SpawnAtPosition(collectibleSpawnPos); 
    }
    else if (chance < 0.5f && _assetSpawner != null)
    {
        // 20% เกิด Asset
        _assetSpawner.SpawnAtPosition(collectibleSpawnPos); 
    }
    else if (chance < 0.6f && _enemySpawner != null)
    {
        // 10% เกิด Enemy
        _enemySpawner.SpawnAtPosition(platformCenter); 
    }
}
#endregion

    // ============================================================================
    // 8. FLOOR GENERATION
    // ============================================================================
    #region Floor Logic
    protected void SpawnInitialFloors()
    {
        if (_objectPoolManager == null || string.IsNullOrEmpty(FloorKey)) return;
        _activeFloors.Clear();
        _nextFloorX = _spawnStartPosition.x;

        // ไม่ต้อง Loop สร้างเองแล้ว เดี๋ยว GeneratePlatformsLoop จะจัดการให้เองตาม Frontier
    }

    protected void SpawnFloorSegment()
    {
        GameObject floor = _objectPoolManager.SpawnFromPool(FloorKey, Vector3.zero, Quaternion.identity);
        if (floor == null) return;

        Vector3 pos = new Vector3(_nextFloorX, _floorY, 0f);
        floor.transform.position = pos;
        floor.transform.SetParent(transform);
        floor.SetActive(true);

        _activeFloors.Add(floor);
        _nextFloorX += _floorLength; // ขยับ Cursor พื้น
        TrySpawnContentOnPlatform(floor, pos, _floorLength);
    }

    protected void RecycleOffScreenFloors()
    {
        if (_generationPivot == null || string.IsNullOrEmpty(FloorKey)) return;
        float threshold = _generationPivot.position.x - 25f;

        for (int i = _activeFloors.Count - 1; i >= 0; i--)
        {
            GameObject f = _activeFloors[i];
            if (f == null) { _activeFloors.RemoveAt(i); continue; }

            if (f.transform.position.x < threshold)
            {
                _activeFloors.RemoveAt(i);
                _objectPoolManager.ReturnToPool(FloorKey, f);
                // ไม่ต้องเติมเอง Loop หลักจัดการให้
            }
        }
    }
    #endregion

    // ============================================================================
    // 9. WALL & HELPERS
    // ============================================================================
    #region Wall Logic
    public virtual void WallUpdate()
    {
        if (_endlessWall == null) return;
        
        if (_endlessWall.TryGetComponent<DuffDuck.Stage.WallPushController>(out var wallController))
        {
            wallController.SetPushState(_wallPushSpeed, _isWallPushEnabled);
        }
    }

    // ============================================================================
    // 10. Public API for Skill Buffs (Career → Map)
    // ============================================================================
    public void SetWallPushSpeed(float speed)
    {
        _wallPushSpeed = speed;
    }

    public void EnableWallPush(bool enabled)
    {
        _isWallPushEnabled = enabled;
    }
    #endregion

    #region Helper Methods
    public virtual void ClearAllObjects()
    {
        if (_objectPoolManager != null)
        {
            foreach (var p in _activePlatforms) if (p) _objectPoolManager.ReturnToPool(GetObjectTag(p), p);
            foreach (var f in _activeFloors) if (f) _objectPoolManager.ReturnToPool(FloorKey, f);
        }
        _activePlatforms.Clear();
        _activeFloors.Clear();
    }

    protected string GetObjectTag(GameObject obj)
    {
        if (obj == null) return string.Empty;
        string name = obj.name;
        int index = name.IndexOf("(Clone)");
        if (index > 0) return name.Substring(0, index).Trim();
        return name;
    }
    #endregion
}