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
    [SerializeField] private MapType _mapType;
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
    [Tooltip("ลดค่านี้ (จาก 4f) เพื่อให้ Platform ไม่ได้ห่างกันมากเกินไป")]
    [SerializeField] protected float _maxXOffset = 3.0f; // ปรับลดจาก 4f เป็น 3.0f (แก้ปัญหา 'ห่างไป')
    [SerializeField] protected float _minYOffset = -1f;
    [SerializeField] protected float _maxYOffset = 1.5f;

    // กำหนดค่า Offset ใหม่ตรงนี้เพื่อให้ Collectible ลอยอยู่บน Platform พอดี
    [SerializeField] protected float _collectibleOffset = 0.25f;
    [SerializeField] protected float _assetVerticalOffset = 0.1f;

    [Header("Content Spawning Rules")]
    [Tooltip("จำนวน Platform ขั้นต่ำที่ต้องเว้นก่อนสปาวน์ Enemy ตัวถัดไป (ปรับจาก 5 เป็น 3)")]
    [SerializeField] private int _minPlatformsBetweenEnemy = 3; 
    [Tooltip("จำนวน Platform ขั้นต่ำที่ต้องเว้นก่อนสปาวน์ Asset ตัวถัดไป (ปรับจาก 3 เป็น 2)")]
    [SerializeField] private int _minPlatformsBetweenAsset = 2; 

    protected float _nextSpawnX; //  Cursor สำคัญ: บอกตำแหน่งขวาสุดที่สร้าง Platform ไปแล้ว
    protected float _nextFloorX; //  Cursor สำคัญ: บอกตำแหน่งขวาสุดที่สร้างพื้นหลัง (Floor) ไปแล้ว

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
    [Tooltip("Offset สูงสุดที่ Platform จะอยู่สูงกว่า Floor Y (เช่น ถ้า Floor Y=-0.8 และค่านี้เป็น 3.8 จะทำให้ Max Y ประมาณ 3.0)")]
    [SerializeField] private float _maxPlatformHeightOffset = 3.8f; // ปรับให้ได้ Max Y ประมาณ 3.0f เมื่อ Floor Y=-0.8f
    [SerializeField] private int _stepsRemaining = 0; // ใช้สำหรับ Pattern Steps
    #endregion

    #region Floor Settings
    [Header("Floor Settings (Tile 1 UNIT)")]
    // NOTE: ค่า Y นี้คือค่ากึ่งกลางของ Floor/Platform ที่ใช้เป็น Base Y
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

    // ตัวแปรสำหรับควบคุมระยะห่างในการสปาวน์
    private int _platformsSinceLastEnemy = 0;
    private int _platformsSinceLastAsset = 0;
    
    // WebGL OPTIMIZATION: ใช้ Timer เพื่อควบคุมการรัน Logic หนักๆ แทนการรันทุกเฟรม
    private float _generationTimer = 0f;
    private const float GENERATION_CHECK_INTERVAL = 0.1f; // ตรวจสอบ Logic การสร้างทุกๆ 0.1 วินาที
    
    // NEW: ตัวคูณโอกาสสปาวน์ (สำหรับปรับความยาก/Buff)
    private float _contentSpawnChanceMultiplier = 1.0f; 
    [Tooltip("ใช้สำหรับระบบ Buff/Difficulty เพื่อควบคุมโอกาสสปาวน์ Content")]
    private bool _isEnemySpawnDisabled = false; // Flag สำหรับปิดการสปาวน์ศัตรูชั่วคราว (เช่นตอนแปลงร่าง)

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
    
    public float ContentSpawnChanceMultiplier { get => _contentSpawnChanceMultiplier; set => _contentSpawnChanceMultiplier = value; }
    #endregion

    #region  Event
    public event System.Action<Vector3> OnPlatformTopSpawnPoint;
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
        
        // 1. ค้นหา Player ก่อนเพื่อใช้เป็น Pivot และส่งให้ Spawner
        Player player = FindFirstObjectByType<Player>();

        if (pivot != null) _generationPivot = pivot;
        else if (_generationPivot == null)
        {
            // ใช้ player ที่หามาได้
            if (player != null) _generationPivot = player.transform;
        }

        _wallPushSpeed = _baseWallPushSpeed;
        _isPlatformBreakable = true;
        _isWallPushEnabled = true;
        
        // กำหนดค่าเริ่มต้นของตัวนับการสปาวน์
        _platformsSinceLastEnemy = _minPlatformsBetweenEnemy;
        _platformsSinceLastAsset = _minPlatformsBetweenAsset;

        
        // 2. หา dependency อื่น ๆ ที่ Spawner ต้องใช้
        var culling     = FindFirstObjectByType<DistanceCulling>();
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

        //   SetDependencies ให้ EnemySpawner ในคลาสฐานนี้ และให้คลาสลูกเรียก InitializeSpawner
        if (_enemySpawner != null && culling != null)
        {
            // Set Dependencies ที่สามารถหาได้ เพื่อให้คลาสลูกเรียก InitializeSpawner ต่อได้
            // EnemySpawner.cs ต้องการ InitializeSpawner(IObjectPool pool, MapType mapType, Player player, CollectibleSpawner collectibleSpawner, CardManager cardManager, BuffManager buffManager)
            _enemySpawner.SetDependencies(
                player, 
                _collectibleSpawner, 
                cardManager, 
                buffManager, 
                _objectPoolManager, 
                culling
            );
                _enemySpawner.InitializeSpawner(
                _objectPoolManager,
                _mapType,
                player,
                _collectibleSpawner,
                cardManager,
                buffManager
            );
        }
        else if (_enemySpawner != null)
        {
            Debug.LogWarning("[MapGeneratorBase] Cannot fully setup EnemySpawner (Missing Culling/Dependencies).");
        }

        // 4. **แก้ปัญหา AssetSpawner** - ใช้เมธอด Initialize(Transform pivot, IObjectPool pool = null)
        if (_assetSpawner != null)
        {
            // AssetSpawner.cs ต้องการ Initialize(Transform pivot, IObjectPool pool = null)
            _assetSpawner.Initialize(_generationPivot, _objectPoolManager);
        }

        // 5. **แก้ปัญหา ThrowableSpawner** - ใช้เมธอด Initialize(Transform pivot, EnemySpawner enemySpawner = null)
        if (_throwableSpawner != null)
        {
            // ThrowableSpawner.cs ต้องการ Initialize(Transform pivot, EnemySpawner enemySpawner = null)
            _throwableSpawner.Initialize(_generationPivot, _enemySpawner);
        }
    
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
    //  CORE LOOP (FIXED): ใช้ Frontier Logic (ถมของให้เต็มหน้าจอเสมอ)
    protected IEnumerator GeneratePlatformsLoop()
    {
        // WebGL/Mobile Optimization: ใช้ Coroutine และ Timer เพื่อแบ่งภาระงานหนัก
        while (_generationPivot != null)
        {
            _generationTimer += Time.deltaTime;

            // ตรวจสอบ Logic การสร้างแผนที่ทุกๆ GENERATION_CHECK_INTERVAL (0.1 วินาที) เพื่อประหยือบ CPU
            if (_generationTimer >= GENERATION_CHECK_INTERVAL)
            {
                _generationTimer = 0f; // รีเซ็ต Timer

                // 1. คำนวณ "เส้นขอบฟ้า" (Frontier) ที่ ต้องวางของไปให้ถึง
                float frontierX = _generationPivot.position.x + 25f; // สปาวล่วงหน้า 25 units

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
            }

            // WebGL/Mobile Optimization: Yield ทุกเฟรมเพื่อให้ Main Thread ว่าง
            yield return null;
        }
    }

    protected virtual void OnDisable()
    {
        // NEW: สั่งหยุด Coroutine เมื่อ Object ถูกปิด/ทำลาย เพื่อป้องกัน Memory Leak
        StopAllCoroutines();
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

        // แก้ปัญหา BreakPlatform/Platform ซ้อนกัน: สุ่ม BreakPlatform เฉพาะเมื่อไม่ใช่ Starter
        if (!isStarter && Random.value < 0.2f && BreakPlatformKey != "") 
        {
            key = BreakPlatformKey;
        }

        GameObject platform = _objectPoolManager.SpawnFromPool(key, Vector3.zero, Quaternion.identity);
        if (platform == null) 
        {
            Debug.LogWarning($"[MapGen] Failed to spawn platform key: {key}");
            return;
        }

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
            // อัปเดตตัวนับจำนวนแพลตฟอร์มที่สร้างไปแล้ว
            _platformsSinceLastEnemy++;
            _platformsSinceLastAsset++;

            TrySpawnContentOnPlatform(platform, spawnPos, _platformWidth);
        }
    }

protected float CalculateYOffsetByState(float currentBaseY)
{
    // ----------------------------------------------------
    // ปรับปรุง: ลดความชันสูงสุด (maxDeltaY) และเพิ่มระดับต่ำสุด (minY)
    // ----------------------------------------------------
    float yOffset = 0f;
    float maxDeltaY = 0.35f; // ลดการขึ้นลงสูงสุดต่อแพลตฟอร์ม (จาก 0.45f) เพื่อให้ทางลาดไม่ชันเกินไป (แก้ปัญหา 'สูงเกิน/ต่ำเกิน')

    //  ปรับปรุงความสูงต่ำสุด/สูงสุดของ Platform
    // ใช้ค่า _floorY ที่มาจาก Inspector (-0.8f) เป็นฐาน
    // ถ้า _floorY = -0.8f, minY จะเท่ากับ -0.8 + 2.0 = 1.2f. (Platform จะไม่ต่ำกว่า Y=1.2)
    float minY = _floorY + 2.0f; // **ปรับเพิ่มจาก 1.6f เป็น 2.0f** เพื่อให้ Platform ลอยสูงขึ้นจากพื้น (Floor Y=-0.8) อย่างชัดเจน
    float maxY = _floorY + _maxPlatformHeightOffset;  // ใช้ค่าจาก Inspector เพื่อควบคุม Max Y (ถ้าตั้ง 3.8 จะได้ Y=3.0)

    // -------------------------
    // เปลี่ยน Pattern เมื่อจบ Step
    // -------------------------
    if (_stepsRemaining <= 0)
    {
        float r = Random.value;
        //  ปรับโอกาสเกิด Normal Pattern ลดลงเหลือ 30% และเพิ่มโอกาสเกิด Pattern อื่นๆ เพื่อความหลากหลาย
        if (r < 0.30f) _currentPlatformState = PlatformState.Normal; // 30% (แก้ปัญหา 'Pattern ไม่ค่อยหลากหลาย')
        else if (r < 0.55f) _currentPlatformState = PlatformState.AscendingSteps; // 25% (เพิ่มขึ้น)
        else if (r < 0.80f) _currentPlatformState = PlatformState.DescendingSteps; // 25% (เพิ่มขึ้น)
        else if (r < 0.90f) _currentPlatformState = PlatformState.HillUp; // 10%
        else _currentPlatformState = PlatformState.HillDown; // 10%

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
            // ใช้ค่า maxDeltaY ที่ปรับแล้ว
            yOffset = Random.Range(0.1f, maxDeltaY); 
            break;

        case PlatformState.HillDown:
            // ใช้ค่า maxDeltaY ที่ปรับแล้ว
            yOffset = Random.Range(-maxDeltaY, -0.1f);
            break;
    }

    _stepsRemaining--;

    float candidateY = currentBaseY + yOffset;

    // -------------------------
    //  Anti-Clipping Logic (ป้องกันหลุดขอบ)
    // -------------------------
    if (candidateY < minY)
    {
        candidateY = minY;
        // บังคับให้เปลี่ยนเป็น Ascending เพื่อให้ Platform กลับขึ้นมา
        _currentPlatformState = PlatformState.AscendingSteps; 
        _stepsRemaining = Random.Range(3, 5); // รีเซ็ต Step ให้สั้นลง
    }
    else if (candidateY > maxY)
    {
        candidateY = maxY;
        // บังคับให้เปลี่ยนเป็น Descending เพื่อให้ Platform กลับลง
        _currentPlatformState = PlatformState.DescendingSteps; 
        _stepsRemaining = Random.Range(3, 5); // รีเซ็ต Step ให้สั้นลง
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
    //   ตรวจสอบ Tag ของ Platform ที่ส่งมา ต้องเป็นหนึ่งใน Tag ที่อนุญาต
    string platformTag = platform.tag;
    if (platformTag != "Floor" && 
        platformTag != "Platform" && 
        platformTag != "BreakPlatform")
    {
        // ถ้า Tag ไม่อยู่ในรายการที่อนุญาต ให้หยุดการสปาวน์เนื้อหา
        Debug.Log($"[MapGen] Skipped content spawn: Invalid Platform Tag: {platformTag}");
        return; 
    }

    float px = _generationPivot != null ? _generationPivot.position.x : 0f;
    if (Mathf.Abs(pos.x - px) < 12f)
    {
        // อยู่ใกล้ผู้เล่นเกิน (12 units) ห้ามสปาว
        return;
    }
    
    // ปรับโอกาสในการสปาวน์ Content ทั้งหมดตามตัวคูณความยาก/Buff
    float contentChance = Random.value * _contentSpawnChanceMultiplier; 

    // 1. คำนวณจุด Spawn ที่ถูกต้องบน Platform
    // (สมมติว่า Platform มีความสูง 1 หน่วย และ pos คือจุดกึ่งกลาง Y)
    float platformTopY = pos.y + 0.5f; 


    // ตำแหน่ง Collectible ที่อยู่ "บน" Platform 
    Vector3 collectibleSpawnPos = new Vector3(
        pos.x, 
        platformTopY + _collectibleOffset, // ใช้ _collectibleOffset ที่กำหนดไว้ใน MapGeneratorBase
        0f
    );
    
    Vector3 assetSpawnPos = new Vector3(
        pos.x, 
        platformTopY + _assetVerticalOffset,
        0f
    );

    // 2. จุด Center ของ Platform (สำหรับ Enemy/Throwable ที่ควรอยู่บน Platform)
    Vector3 platformTop = new Vector3(pos.x, platformTopY, 0f); 

    // แจ้งตำแหน่ง Platform นี้ว่า Enemy สามารถยืนได้
    OnPlatformTopSpawnPoint?.Invoke(new Vector3(pos.x, platformTopY, 0f));

    // ----------------------------------------------------
    // UPDATED LOGIC: ใช้ contentChance ที่ถูกปรับแล้ว และเช็ค Enemy Disabled Flag
    // ----------------------------------------------------
    
    // 1. Collectible (35% Chance) - High Priority, Low Impact (ไม่นับเป็น Exclusive Slot)
    if (Random.value < 0.35f && _collectibleSpawner != null) 
    {
        _collectibleSpawner.SpawnAtPosition(collectibleSpawnPos); 
    }

    // 2. Roll สำหรับ Enemy, Asset, Throwable (รวมกัน 70% ของโอกาสที่เหลือ)
    
    // 2.1 Enemy (15% Base Chance) - Needs Distance Lock (Min 3 platforms)
    if (contentChance < 0.15f && _enemySpawner != null && !_isEnemySpawnDisabled && _platformsSinceLastEnemy >= _minPlatformsBetweenEnemy)
    {
        _enemySpawner.SpawnAtPosition(platformTop); 
        _platformsSinceLastEnemy = 0; // รีเซ็ตตัวนับเมื่อสปาวสำเร็จ
    }

    // 2.2 Asset (25% Base Chance) - Needs Distance Lock (Min 2 platforms)
    // โอกาสรวมถึงช่องว่างที่ Enemy ไม่สปาวน์ (0.15f - 0.40f)
    else if (contentChance < 0.40f && _assetSpawner != null && _platformsSinceLastAsset >= _minPlatformsBetweenAsset) 
    {
        _assetSpawner.SpawnAtPosition(assetSpawnPos); 
        _platformsSinceLastAsset = 0; // รีเซ็ตตัวนับเมื่อสปาวสำเร็จ
    }
    
    // 2.3 Throwable (20% Base Chance) - No Distance Lock
    // โอกาสรวมถึงช่องว่างที่ Enemy/Asset ไม่สปาวน์ (0.40f - 0.60f)
    else if (contentChance < 0.60f && _throwableSpawner != null)
    {
        // ใช้ตำแหน่งเดียวกับ Asset/Enemy แต่ตั้งค่าความสูงให้เหมาะสมใน Spawner นั้นๆ
        _throwableSpawner.SpawnAtPosition(platformTop); 
    }
    

}

// NEW: API สำหรับระบบ Buff/Transformation เข้ามาสั่งปิด/เปิดการสปาวน์ศัตรู
public void SetEnemySpawnDisabled(bool disabled)
{
    _isEnemySpawnDisabled = disabled;
    if (disabled)
    {
        Debug.Log("[MapGen] Enemy spawning temporarily disabled (e.g., player transformed).");
    }
    else
    {
        Debug.Log("[MapGen] Enemy spawning re-enabled.");
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
        
        // อัปเดตตัวนับจำนวนแพลตฟอร์มที่สร้างไปแล้ว (นับ Floor ด้วย)
        _platformsSinceLastEnemy++;
        _platformsSinceLastAsset++;

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