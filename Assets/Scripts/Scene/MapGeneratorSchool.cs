using System.Collections.Generic;
using UnityEngine;
using System.Collections; // ต้องมีหากใช้ StartCoroutine ในเมธอด Spawn

/// <summary>
/// MapGeneratorSchool (School Map = 1)
/// - จัดการ Assets และ Logic เฉพาะของฉากโรงเรียน
/// - ใช้ Logic Endless Generation และ Object Pooling จาก MapGeneratorBase
/// </summary>
public class MapGeneratorSchool : MapGeneratorBase
{
    #region Fields
    [Header("School Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_School_";
    [SerializeField] private string _backgroundKey = "map_bg_School"; 
    [SerializeField] private string _schoolFloor = "map_asset_School_Floor";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new(); 
    
    // Throwable Item Key (ใช้สำหรับ Spawner หรือ Pool Manager)
    [SerializeField] private string _throwableAssetKey = "map_ThrowItem_School_"; 
    
    // Wall Visual Key (ใช้สำหรับสร้างภาพกำแพงด้านหลัง)
    [SerializeField] private string _wallVisualKey = "map_Wall_School"; 

    // Platform Asset Keys (ต้องตรงกับชื่อ Prefab/Tag ใน ObjectPoolManager)
    [Header("Platform Assets")]
    [SerializeField] private string _normalPlatformKey = "map_asset_School_Normal_Platform"; 
    [SerializeField] private string _breakPlatformKey = "map_asset_School_Break_Platform"; 
    #endregion

    #region Abstract Implementation
    
    /// <summary>
    /// กำหนด Prefab Key สำหรับ Normal Platform ให้ Base Class ใช้งาน
    /// </summary>
    protected override string NormalPlatformKey => _normalPlatformKey;
    
    /// <summary>
    /// กำหนด Prefab Key สำหรับ Breakable Platform ให้ Base Class ใช้งาน
    /// </summary>
    protected override string BreakPlatformKey => _breakPlatformKey;
    
    #endregion

    #region Override Methods
    
    /// <summary>
    /// กำหนดลำดับการทำงานเฉพาะของ Map School
    /// </summary>
    public override void GenerateMap()
    {
        Debug.Log("Generating School Map...");
        SetupBackground();

        SetupFloor();                        // floor Wall follow
        // 1. Initialize Base Generators (จะทำการหา ObjectPoolManager และตั้งค่า Wall)
        InitializeGenerators(); 
        
        // 2. Setup Assets & Keys (แจ้ง Pool Manager ให้เตรียม Prefab ทั้งหมด)
        LoadAssets(); // ถ้าไม่มี Logic ก็จะทำแค่ Debug.Log
        RegisterSchoolAssets(); // แจ้ง Pool Manager ให้เตรียม Pool สำหรับ Keys
        

       _enemySpawner?.InitializeSpawner(_objectPoolManager,MapType.School,FindAnyObjectByType<Player>(),_collectibleSpawner,FindAnyObjectByType<CardManager>());
        // 4. เริ่มต้น Spawner เฉพาะของแมพ
        SpawnEnemies();
        _collectibleSpawner?.InitializeSpawner(_objectPoolManager, FindAnyObjectByType<DistanceCulling>(), FindAnyObjectByType<CardManager>(), FindAnyObjectByType<BuffManager>());
        SpawnCollectibles();
        
         // 3. เริ่มต้น Loop การสร้าง Platform (ใช้ Logic ใน Base Class)
        InitializePlatformGeneration(); 
        _objectPoolManager.InitializePool();

        // กำหนดความเร็ว Wall เริ่มต้น
        WallPushSpeed = _baseWallPushSpeed; // ใช้ Property ที่สืบทอดมาจาก Base Class
        Debug.Log("[SchoolMap] Initial WallPushSpeed set.");
    }

    public override void SpawnEnemies()
    {
        if (_enemySpawner == null)
        {
            Debug.LogWarning("[SchoolMap] EnemySpawner reference missing.");
            return;
        }
        
        // EnemySpawner จะจัดการการดึง Object Pool (รวมถึง Throwable Items)
        StartCoroutine(_enemySpawner.StartWave());
        
        Debug.Log($"[SchoolMap] Initiating random waves for School Map.");
    }

    public override void SpawnCollectibles()
    {
        if (_collectibleSpawner == null) return;
        StartCoroutine(SpawnCollectiblesLoop());
    }

    private IEnumerator SpawnCollectiblesLoop()
    {
        while (true)
        {
            _collectibleSpawner.Spawn();
            yield return new WaitForSeconds(Random.Range(3f, 7f)); // spawn ทุก 3–7 วินาที
        }
    }


    public override void SetupBackground()
    {
        Debug.Log(" Setting up School Background...");
        _backgroundLooper?.SetBackground("map_bg_School");

    }

    /// <summary>
    /// Spawn พื้นเริ่มเกม + กำแพงไล่หลัง โดยใช้ Pool
    /// </summary>
    public override void SetupFloor()
    {
        if (_objectPoolManager == null)
        {
            Debug.LogError("[SchoolMap] ObjectPoolManager missing!");
            return;
        }

        // Floor เริ่มเกม
        GameObject floor = _objectPoolManager.SpawnFromPool(
            _schoolFloor,
            new Vector3(_spawnStartPosition.x, _spawnStartPosition.y - 1.5f, 0),
            Quaternion.identity
        );
        floor.transform.SetParent(transform);
        floor.SetActive(true);
        _activePlatforms.Add(floor);
        _nextSpawnX = _spawnStartPosition.x + _platformWidth;

        Debug.Log($"[{GetType().Name}] SetupFloor completed → first platform spawned.");

        // Wall ไล่
        GameObject wall = _objectPoolManager.SpawnFromPool(
            _wallVisualKey,
            new Vector3(_spawnStartPosition.x - 8f, _spawnStartPosition.y, 0),
            Quaternion.identity
        );

        var controller = wall.GetComponent<WallPushController>();
        controller?.SetPushState(WallPushSpeed, IsWallPushEnabled);

        _endlessWall = wall.transform;

        Debug.Log("[SchoolMap] SetupFloor — Floor + Wall (no visual).");
    }


    #endregion

    #region  Clear
    
    public override void ClearAllObjects()
    {
        // เรียก Base Class เพื่อคืน Platform ที่ Active ทั้งหมดเข้าสู่ Pool
        base.ClearAllObjects(); 
        
        // Logic สำหรับ Clear Object อื่นๆ ที่ไม่ได้อยู่ใน _activePlatforms เช่น Wall Visual
        
        Debug.Log(" Clearing all school map objects...");
    }
    #endregion

    #region Asset Management
    
    public void LoadAssets() 
    {
        Debug.Log(" Loading School Assets... (Pooling system used)");
        // เมธอดนี้ถูกทิ้งว่างไว้เนื่องจาก Pooling จัดการการโหลด Prefab ทั้งหมด
    }

    /// <summary>
    /// ลงทะเบียน Prefab Keys ที่จำเป็นทั้งหมดให้กับ ObjectPoolManager
    /// </summary>
    public void RegisterSchoolAssets()
    {
        // ObjectPoolManager จะถูกหาใน Base.InitializeGenerators()
        if (_objectPoolManager == null) 
        {
             Debug.LogError("[SchoolMap] ObjectPoolManager is null. Cannot register assets.");
             return;
        }

        Debug.Log(" Registering School Asset Dictionary...");

        // รวบรวม Keys ที่จำเป็นทั้งหมด (รวม Platform, Throwable, และ Visuals)
        List<string> assetKeys = new List<string>
        {
            NormalPlatformKey,      
            BreakPlatformKey,       
            _throwableAssetKey,     
            _wallVisualKey,
            _schoolFloor
        };
        
        Debug.Log("[SchoolMap] Platform, Throwable, and Wall Keys registered for Pooling.");
    }
    #endregion

    private void Update()
    {
        if (IsWallPushEnabled)
            WallUpdate();
    }
}