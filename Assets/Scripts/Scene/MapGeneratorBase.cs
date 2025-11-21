using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class สำหรับทุก Map
/// - Endless Floor (tile 1 UNIT ต่อกันด้วย Pool)
/// - Endless Platform (สุ่ม X,Y)
/// - Wall ไล่หลังผ่าน WallUpdate()
/// - Hook ให้ EnemySpawner / CollectibleSpawner / AssetSpawner / ThrowableSpawner
/// </summary>
public abstract class MapGeneratorBase : MonoBehaviour
{
    #region Spawner References
    [Header("Spawner References")]
    [SerializeField] protected EnemySpawner _enemySpawner;
    [SerializeField] protected CollectibleSpawner _collectibleSpawner;
    [SerializeField] protected BackgroundLooper _backgroundLooper;

    // เพิ่ม hook ไว้ให้ลูกแมพใช้ Asset/Throwable แยกจาก collectible ปกติ
    [SerializeField] protected AssetSpawner _assetSpawner;          // NEW
    [SerializeField] protected ThrowableSpawner _throwableSpawner;  // NEW
    #endregion

    #region Basic Generation Settings
    [Header("Basic Generation Settings")]
    [Tooltip("จุดเริ่มต้นสำหรับ Spawn Platform / Floor")]
    [SerializeField] protected Vector2 _spawnStartPosition = new Vector2(0f, 0.2f);

    [Tooltip("จำนวน Platform ที่ Active ได้สูงสุด (กันล้น)")]
    [SerializeField] protected int _maxPlatformCount = 20;

    [Tooltip("Pivot สำหรับเช็คระยะทาง (โดยปกติใช้ Player)")]
    [SerializeField] protected Transform _generationPivot;
    #endregion

    #region Endless Platform Settings
    [Header("Platform Endless Settings")]
    [SerializeField] protected float _platformWidth = 10f;

    [SerializeField] protected float _minXOffset = 2f;
    [SerializeField] protected float _maxXOffset = 4f;

    [SerializeField] protected float _minYOffset = -1f;
    [SerializeField] protected float _maxYOffset = 1.5f;

    [Tooltip("จำนวน Platform เริ่มต้นตอนเข้าแมพ")]
    [SerializeField] protected int _initialPlatformsCount = 5;

    [Header("Runtime Platform")]
    [SerializeField] protected List<GameObject> _activePlatforms = new List<GameObject>();

    protected float _nextSpawnX;
    #endregion

    #region Floor Settings (Base Ground)
    [Header("Floor Settings (Tile 1 UNIT)")]
    [Tooltip("แกน Y ของพื้นหลัก (ทุก tile จะอยู่ Y นี้)")]
    [SerializeField] protected float _floorY = 0.2f;

    [Tooltip("ความยาวของ Floor หนึ่งชิ้น (1 = 1 ช่อง grid)")]
    [SerializeField] protected float _floorLength = 1f;

    [Tooltip("จำนวน Floor tile เริ่มต้นในฉาก")]
    [SerializeField] protected int _initialFloorSegments = 30;

    [Header("Runtime Floor")]
    [SerializeField] protected List<GameObject> _activeFloors = new List<GameObject>();

    protected float _nextFloorX;
    #endregion

    #region Wall Control
    [Header("Wall Control")]
    [Tooltip("Wall ไล่หลัง (ใช้ร่วมกับ Wall_Kill หรือ WallPushController ได้)")]
    [SerializeField] protected Transform _endlessWall;

    [Tooltip("ความเร็วพื้นฐานของกำแพง")]
    [SerializeField] protected float _baseWallPushSpeed = 1.0f;

    private float _wallPushSpeed;
    private bool _isPlatformBreakable = true;
    private bool _isWallPushEnabled = true;
    #endregion

    #region Pool manager
    protected ObjectPoolManager _objectPoolManager;
    #endregion

    #region Abstract Keys (ให้ลูกคลาสระบุ)
    protected abstract string NormalPlatformKey { get; }
    protected abstract string BreakPlatformKey { get; }
    protected abstract string FloorKey { get; }
    #endregion

    #region Properties / Flags
    public float WallPushSpeed
    {
        get { return _wallPushSpeed; }
        set { _wallPushSpeed = value; }
    }

    public bool IsPlatformBreakable
    {
        get { return _isPlatformBreakable; }
        set { _isPlatformBreakable = value; }
    }

    public bool IsWallPushEnabled
    {
        get { return _isWallPushEnabled; }
        set { _isWallPushEnabled = value; }
    }
    #endregion

    #region Wall Logic
    /// <summary>
    /// ให้กำแพงไล่ตาม Player (หรือ _generationPivot)
    /// เรียกจาก GeneratePlatformsLoop()
    /// </summary>
    public virtual void WallUpdate()
        {
            if (_endlessWall == null || _generationPivot == null) return;
            
            // --- START OF FIX: ถอดโค้ดการเคลื่อนที่กำแพงออก ---
            
            // 1. ตรวจสอบว่ามี WallPushController ติดอยู่กับกำแพงหรือไม่
            if (_endlessWall.TryGetComponent<DuffDuck.Stage.WallPushController>(out var wallController))
            {
                // 2. ส่งค่าความเร็วและสถานะที่ต้องการไปยัง WallPushController
                //    WallPushController จะรับผิดชอบการเคลื่อนที่ใน Update() ของตัวเอง
                wallController.ExecuteMovementAndEvent(_wallPushSpeed, _isWallPushEnabled);
            }
            else
            {
                // [Optional]: Log error ถ้าไม่เจอ controller เพื่อให้รู้ว่าโค้ดเคลื่อนที่ Wall 
                // ถูก MapGenerator ควบคุมโดยตรง (ซึ่งเป็นโค้ดเดิมที่ซ้ำซ้อน)
                
                // 3. (Fallback - สำหรับกรณีที่ไม่มี WallPushController)
                if (!_isWallPushEnabled) return;
                
                // ให้กำแพงอยู่ห่าง pivot ในระยะประมาณ 8 หน่วย
                float targetX = _generationPivot.position.x - 8f;

                if (_endlessWall.position.x < targetX)
                {
                    Vector3 move = Vector3.right * _wallPushSpeed * Time.deltaTime;
                    _endlessWall.Translate(move);
                }
            }
            
        }

    /// <summary>
    /// ใช้สำหรับสกิลพวกที่ทำลายแพลตฟอร์มด้านขวาสุด
    /// หาแบบ manual loop (ไม่ใช้ LINQ เพื่อ WebGL)
    /// </summary>
    public virtual void BreakRightmostPlatform()
    {
        if (_objectPoolManager == null || !_isPlatformBreakable) return;
        if (_activePlatforms == null || _activePlatforms.Count == 0) return;

        int index = -1;
        float maxX = float.MinValue;

        for (int i = 0; i < _activePlatforms.Count; i++)
        {
            GameObject p = _activePlatforms[i];
            if (p == null) continue;

            float x = p.transform.position.x;
            if (x > maxX)
            {
                maxX = x;
                index = i;
            }
        }

        if (index >= 0)
        {
            GameObject rightmost = _activePlatforms[index];
            _activePlatforms.RemoveAt(index);
            _objectPoolManager.ReturnToPool(GetObjectTag(rightmost), rightmost);
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// เรียกหลังโหลด scene จาก SceneManager / GameManager
    /// </summary>
    public virtual void InitializeGenerators(Transform pivot = null)
    {
        _objectPoolManager = ObjectPoolManager.Instance;
        if (_objectPoolManager == null)
        {
            Debug.LogError("MapGeneratorBase: ObjectPoolManager.Instance is NULL! Make sure pool exists in bootstrap/MainMenu scene.");
            return;
        }

        if (pivot != null)
        {
            _generationPivot = pivot;
        }
        else if (_generationPivot == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
                _generationPivot = player.transform;
        }

        _wallPushSpeed = _baseWallPushSpeed;
        _isPlatformBreakable = true;
        _isWallPushEnabled = true;

        Debug.Log("MapGeneratorBase: Generators initialized (Pool + Pivot ready).");
    }

    /// <summary>
    /// เริ่มระบบ Floor + Platform Endless
    /// ให้ลูกแมพเรียกใน GenerateMap()
    /// </summary>
    protected void InitializePlatformGeneration()
    {
        _nextSpawnX = _spawnStartPosition.x;
        _nextFloorX = _spawnStartPosition.x;

        // Floor tiles แรก
        SpawnInitialFloors();

        // Platform เริ่มต้น
        for (int i = 0; i < _initialPlatformsCount; i++)
        {
            SpawnNextPlatform(true);
        }

        StartCoroutine(GeneratePlatformsLoop());
    }
    #endregion

    #region Abstract Entry Point
    /// <summary>
    /// ลูกแมพ (School / Road / Kitchen) ต้องจัดลำดับเอง:
    /// - InitializeGenerators
    /// - SetupBackground
    /// - SetupFloor (ถ้าอยาก override)
    /// - InitializePlatformGeneration
    /// - SpawnEnemies / SpawnCollectibles / SpawnAssets / SpawnThrowables
    /// </summary>
    public abstract void GenerateMap();
    #endregion

    #region Virtual Hooks (ให้ลูกคลาส override)
    public virtual void SetupBackground() { }
    public virtual void SetupFloor()
    {
        // default = ใช้ SpawnInitialFloors()
        SpawnInitialFloors();
    }

    public virtual void SpawnEnemies() { }
    public virtual void SpawnCollectibles() { }

    // 🆕 Asset & Throwable hooks
    public virtual void SpawnAssets() { }
    public virtual void SpawnThrowables() { }

    #endregion

    #region ClearAll
    public virtual void ClearAllObjects()
    {
        if (_objectPoolManager != null)
        {
            // Platform
            for (int i = _activePlatforms.Count - 1; i >= 0; i--)
            {
                GameObject p = _activePlatforms[i];
                if (p != null)
                    _objectPoolManager.ReturnToPool(GetObjectTag(p), p);
            }

            // Floor
            for (int i = _activeFloors.Count - 1; i >= 0; i--)
            {
                GameObject f = _activeFloors[i];
                if (f != null)
                    _objectPoolManager.ReturnToPool(FloorKey, f);
            }
        }

        _activePlatforms.Clear();
        _activeFloors.Clear();
    }
    #endregion

    #region Endless Loop (Platform + Floor + Wall)

    protected IEnumerator GeneratePlatformsLoop()
    {
        // NOTE: Coroutine เดียว รันทุกเฟรม → WebGL OK ถ้าทำงานเบา ๆ
        while (_generationPivot != null)
        {
            // สร้าง platform ใหม่เมื่อเข้าใกล้ขอบขวา
            if (_generationPivot.position.x > _nextSpawnX - (_platformWidth * 2f))
            {
                SpawnNextPlatform(false);
            }

            RecycleOffScreenPlatforms();
            RecycleOffScreenFloors();
            WallUpdate();

            // FIX: เปลี่ยนจาก yield return null; เป็นการรอตามเวลา
            yield return new WaitForSeconds(0.05f);
        }
    }

    protected void SpawnNextPlatform(bool isStarter)
    {
        if (_objectPoolManager == null) return;
        if (_activePlatforms.Count >= _maxPlatformCount) return;

        string key = NormalPlatformKey;
        if (!isStarter)
        {
            if (Random.value < 0.2f) // 20% = Breakable
                key = BreakPlatformKey;
        }

        GameObject platform = _objectPoolManager.SpawnFromPool(key, Vector3.zero, Quaternion.identity);
        if (platform == null)
        {
            Debug.LogError("MapGeneratorBase: Platform pool key not found: " + key);
            return;
        }

        Vector3 spawnPos;

        if (isStarter)
        {
            spawnPos = new Vector3(_nextSpawnX, _spawnStartPosition.y, 0f);
            _nextSpawnX += _platformWidth;
        }
        else
        {
            float xOffset = Random.Range(_minXOffset, _maxXOffset);
            float yOffset = Random.Range(_minYOffset, _maxYOffset);

            _nextSpawnX += xOffset;

            float baseY = _spawnStartPosition.y;
            if (_activePlatforms.Count > 0)
            {
                GameObject last = _activePlatforms[_activePlatforms.Count - 1];
                if (last != null)
                    baseY = last.transform.position.y;
            }

            spawnPos = new Vector3(_nextSpawnX, baseY + yOffset, 0f);
            _nextSpawnX += _platformWidth;
        }

        platform.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        platform.transform.SetParent(transform);
        platform.SetActive(true);

        _activePlatforms.Add(platform);
    }

    protected void RecycleOffScreenPlatforms()
    {
        if (_objectPoolManager == null || _generationPivot == null) return;

        float threshold = _generationPivot.position.x - 15f;

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

    protected void SpawnInitialFloors()
    {
        if (_objectPoolManager == null) return;
        if (string.IsNullOrEmpty(FloorKey)) return;

        _activeFloors.Clear();
        _nextFloorX = _spawnStartPosition.x;

        for (int i = 0; i < _initialFloorSegments; i++)
        {
            SpawnFloorSegment();
        }
    }

    protected void SpawnFloorSegment()
    {
        GameObject floor = _objectPoolManager.SpawnFromPool(FloorKey, Vector3.zero, Quaternion.identity);
        if (floor == null)
        {
            Debug.LogError("MapGeneratorBase: FloorKey not found in pool: " + FloorKey);
            return;
        }

        Vector3 pos = new Vector3(_nextFloorX, _floorY, 0f);
        floor.transform.position = pos;
        floor.transform.SetParent(transform);
        floor.SetActive(true);

        _activeFloors.Add(floor);
        _nextFloorX += _floorLength;
    }

    protected void RecycleOffScreenFloors()
    {
        if (_objectPoolManager == null || _generationPivot == null) return;
        if (string.IsNullOrEmpty(FloorKey)) return;

        float threshold = _generationPivot.position.x - 20f;

        for (int i = _activeFloors.Count - 1; i >= 0; i--)
        {
            GameObject f = _activeFloors[i];
            if (f == null) { _activeFloors.RemoveAt(i); continue; }

            if (f.transform.position.x < threshold)
            {
                _activeFloors.RemoveAt(i);
                _objectPoolManager.ReturnToPool(FloorKey, f);
                // เติม floor ใหม่ด้านขวา
                SpawnFloorSegment();
            }
        }
    }
    #endregion

    #region Helper
    /// <summary>
    /// ตัด "(Clone)" ออกจากชื่อเพื่อนำไปเป็น pool key
    /// </summary>
    protected string GetObjectTag(GameObject obj)
    {
        if (obj == null) return string.Empty;

        string name = obj.name;
        int index = name.IndexOf("(Clone)");
        if (index > 0)
            return name.Substring(0, index).Trim();

        return name;
    }
    #endregion
}
