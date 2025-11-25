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
    [SerializeField] private float _currentHeightLimit = 0f; // ใช้จำกัดความสูงสูงสุดใน Pattern Hill
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
            float yOffset = 0f; // 🔥 เปลี่ยนเป็น 0f และให้ Logic ใหม่คำนวณแทน

            _nextSpawnX += xOffset; // ขยับ Cursor (ช่องว่าง)

            // อิง Y จากอันล่าสุด
            float baseY = _spawnStartPosition.y;
            if (_activePlatforms.Count > 0)
            {
                var last = _activePlatforms[_activePlatforms.Count - 1];
                if (last != null) baseY = last.transform.position.y;
            }
            
            // =======================================================
            // 🔥 FIX: ใช้ State Machine คำนวณ Y-Offset
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
        float maxDeltaY = 0.5f; // จำกัดการขึ้นลงสูงสุดต่อ Platform

        // 1. ตรวจสอบและเปลี่ยน State เมื่อ Pattern ปัจจุบันจบลง
        if (_currentPlatformState == PlatformState.Normal || _stepsRemaining <= 0)
        {
            // สุ่มเลือกระหว่างการรักษาความสูง (Normal) หรือเริ่ม Pattern ใหม่
            if (Random.value < 0.8f) // 80% เป็น Normal ต่อ
            {
                _currentPlatformState = PlatformState.Normal;
            }
            else
            {
                // เริ่ม Pattern ใหม่ (20% โอกาส)
                int pattern = Random.Range(1, 5); // 1..4 (Ascending, Descending, HillUp, HillDown)
                _currentPlatformState = (PlatformState)pattern;
                _stepsRemaining = Random.Range(3, 8); // Pattern จะอยู่ 3-7 Platform
                _currentHeightLimit = currentBaseY;
            }
        }

        // 2. คำนวณ Y-Offset ตาม State
        switch (_currentPlatformState)
        {
            case PlatformState.Normal:
                // อยู่ในระดับ Y เดิม (เพิ่ม/ลดแบบสุ่มเล็กน้อยเพื่อไม่ให้แบนเกินไป)
                yOffset = Random.Range(-0.1f, 0.1f);
                break;
                
            case PlatformState.AscendingSteps:
                // ขึ้นบันไดทีละ 0.5f
                yOffset = maxDeltaY;
                _stepsRemaining--;
                break;

            case PlatformState.DescendingSteps:
                // ลงบันไดทีละ -0.5f
                yOffset = -maxDeltaY;
                _stepsRemaining--;
                break;

            case PlatformState.HillUp:
                // ค่อยๆ ขึ้น (maxDeltaY ลดลงตามความชัน)
                yOffset = Random.Range(0.1f, maxDeltaY * 0.7f); 
                _stepsRemaining--;
                break;

            case PlatformState.HillDown:
                // ค่อยๆ ลง
                yOffset = Random.Range(-maxDeltaY * 0.7f, -0.1f);
                _stepsRemaining--;
                break;
        }
        
        // 3. จำกัดความสูงรวม (ป้องกันเหินฟ้า)
        // ตรวจสอบว่า Platform ใหม่ไม่สูงเกินไปจากจุดเริ่มต้น Map
        float globalMaxY = 4f; // จำกัดความสูงสูงสุดที่รับได้ (ปรับค่านี้ใน Inspector ได้)
        if (currentBaseY + yOffset > _spawnStartPosition.y + globalMaxY)
        {
            yOffset = 0f; // บังคับให้หยุดขึ้น
            _currentPlatformState = PlatformState.DescendingSteps; // บังคับให้เริ่มลง
        }
        
        return yOffset;
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

    // ============================================================================
    // 7. CONTENT SPAWNING (ITEMS / ENEMIES / ASSETS)
    // ============================================================================
    #region Content Spawning
    // NEW FEATURE: Spawn ของบน Platform (Asset / Item / Enemy)
    protected virtual void TrySpawnContentOnPlatform(GameObject platform, Vector3 pos, float width)
    {
        // ตัวอย่าง Logic ง่ายๆ: สุ่มว่าจะเกิดอะไรบน Platform นี้
        float chance = Random.value;

        // 1. จุดเริ่มต้น Raycast (สำหรับ Collectible/Asset)
        //    ต้องสูงกว่า Platform (pos.y) เพื่อยิง Ray ลงไปหาพื้น
        Vector3 raycastOrigin = new Vector3(pos.x, pos.y + 5f, 0f); 
        
        // 2. จุด Center ของ Platform (สำหรับ Enemy)
        //    ให้ Enemy Spawner จัดการเพิ่ม Offset เอง
        Vector3 platformCenter = new Vector3(pos.x, pos.y, 0f); 

        if (chance < 0.3f && _collectibleSpawner != null)
        {
            // 30% เกิด Collectible
            // CollectibleSpawner จะทำ Raycast จากจุดที่ส่งไป (raycastOrigin)
            _collectibleSpawner.SpawnAtPosition(raycastOrigin); 
        }
        else if (chance < 0.5f && _assetSpawner != null)
        {
            // 20% เกิด Asset (สิ่งกีดขวาง)
            // AssetSpawner จะทำ Raycast จากจุดที่ส่งไป (raycastOrigin)
            _assetSpawner.SpawnAtPosition(raycastOrigin);
        }
        else if (chance < 0.6f && _enemySpawner != null)
        {
            // 10% เกิด Enemy
            // EnemySpawner จะใช้ Y ของ Platform (platformCenter.y) + offset เพื่อให้ยืนบนพื้น
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