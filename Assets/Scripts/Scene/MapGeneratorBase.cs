using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections; // สำหรับ Coroutine

public abstract class MapGeneratorBase : MonoBehaviour
{
    #region Protected Fields
    [Header("Spawner References")]
    [SerializeField] protected EnemySpawner _enemySpawner;
    [SerializeField] protected CollectibleSpawner _collectibleSpawner;
    [SerializeField] protected BackgroundLooper _backgroundLooper;

    [Header("Generation Settings")]
    [SerializeField] protected Vector2 _spawnStartPosition;
    [SerializeField] protected int _maxPlatformCount = 10;
    [SerializeField] protected float _sceneWidth = 50f;

    //  NEW: Fields สำหรับ Endless Generation (ย้ายจาก MapGeneratorSchool)
    [Header("Endless Generation Settings")]
    [SerializeField] protected float _platformWidth = 10f; // ความกว้างของ Platform
    [SerializeField] protected float _nextSpawnX; // ตำแหน่ง X ที่จะสร้าง Platform ถัดไป
    [SerializeField] protected float _minXOffset = 2f; // ระยะห่าง X ต่ำสุด
    [SerializeField] protected float _maxXOffset = 4f; // ระยะห่าง X สูงสุด
    [SerializeField] protected float _minYOffset = -1f; // ระยะห่าง Y ต่ำสุด
    [SerializeField] protected float _maxYOffset = 1.5f; // ระยะห่าง Y สูงสุด
    [SerializeField] protected int _initialPlatformsCount = 5; // จำนวน Platform เริ่มต้น
    [SerializeField] protected Transform _generationPivot; // Pivot สำหรับการสร้าง Platform (Player Transform)
    
    [Header("Wall Control")]
    [SerializeField] protected Transform _endlessWall; // Reference to the actual Wall GameObject
    [SerializeField] protected float _baseWallPushSpeed = 1.0f; // Default push speed

    [Header("Asset Catalogs")]
    [SerializeField] protected Dictionary<string, GameObject> _assetCatalog = new();
    
    [Header("Platform Management")]
    [SerializeField] protected List<GameObject> _activePlatforms = new();
    
    // NEW: Reference to Object Pool Manager
    protected ObjectPoolManager _objectPoolManager; 
    private float _wallPushSpeed;
    private bool _isPlatformBreakable;
    private bool _isWallPushEnabled;
    #endregion

    #region Abstract Properties 
    protected abstract string NormalPlatformKey { get; }
    protected abstract string BreakPlatformKey { get; }
    #endregion


    #region Wall and Scene Control

/// <summary>
    /// Gets or sets the current pushing speed of the endless wall.
    /// </summary>
    //  FIX 2: เพิ่ม Properties ที่ขาดหายไป
    public float WallPushSpeed 
    { 
        get => _wallPushSpeed; 
        set => _wallPushSpeed = value; 
    }

    /// <summary>
    /// Gets or sets a flag indicating whether platforms can be broken.
    /// </summary>
    public bool IsPlatformBreakable 
    { 
        get => _isPlatformBreakable; 
        set => _isPlatformBreakable = value; 
    }

    /// <summary>
    /// Gets or sets a flag indicating whether the wall is currently pushing.
    /// </summary>
    public bool IsWallPushEnabled 
    { 
        get => _isWallPushEnabled; 
        set => _isWallPushEnabled = value; 
    }

    /// <summary>
    /// Handles the continuous movement of the Endless Wall.
    /// </summary>
    public virtual void WallUpdate()
    {
        if (_endlessWall == null || _generationPivot == null) return;

        // ถ้ากำแพงอยู่ต่ำกว่าความเร็วไล่ → วิ่งเข้าใกล้ผู้เล่น
        float targetX = _generationPivot.position.x - 8f; // 8f = ระยะไล่ตาม (จูนได้)

        if (_endlessWall.position.x < targetX)
        {
            _endlessWall.Translate(Vector3.right * WallPushSpeed * Time.deltaTime);
        }
    }


    public virtual void BreakRightmostPlatform()
    {
        if (_objectPoolManager == null || !IsPlatformBreakable || _activePlatforms.Count == 0)
        {
            Debug.Log($"[{GetType().Name}] Cannot break platform: Pool/Breakable status not ready.");
            return;
        }

        var rightmostPlatform = _activePlatforms
            .OrderByDescending(p => p.transform.position.x)
            .FirstOrDefault();

        if (rightmostPlatform != null)
        {
            _activePlatforms.Remove(rightmostPlatform);
            _objectPoolManager.ReturnToPool(GetObjectTag(rightmostPlatform), rightmostPlatform); 
            Debug.Log($"[{GetType().Name}] Rightmost platform broken and returned to pool.");
        }
    }
    #endregion

    #region Initialization

    public virtual void InitializeGenerators()
    {
        //  FIX 4: ค้นหา Object Pool Manager
        _objectPoolManager = FindFirstObjectByType<ObjectPoolManager>();
        if (_objectPoolManager == null)
        {
            Debug.LogError($"[{GetType().Name}] ObjectPoolManager not found! Cannot initialize.");
            return;
        }

        // Set initial state for the wall and buffs
        WallPushSpeed = _baseWallPushSpeed;
        IsPlatformBreakable = true;
        IsWallPushEnabled = true;
        
        Debug.Log($"{GetType().Name}: Generators initialized.");
    }

    /// <summary>
    ///  NEW: เริ่มต้นการสร้าง Platform อย่างต่อเนื่อง
    /// </summary>
    protected void InitializePlatformGeneration()
    {
        _nextSpawnX = _spawnStartPosition.x; 

        // 1. สร้าง Platform เริ่มต้นให้ Player ยืน
        for (int i = 0; i < _initialPlatformsCount; i++)
        {
             SpawnNextPlatform(isStarter: true);
        }

        // 2. เริ่ม Loop สร้าง Platform ต่อไปอย่างต่อเนื่อง
        StartCoroutine(GeneratePlatformsLoop());
        
        Debug.Log($"[{GetType().Name}] Initialization complete. Started Generation Loop.");
    }
    #endregion

    #region Abstract Method
    public abstract void GenerateMap();
    #endregion

    #region Virtual Methods (Core Generation Logic)

    public virtual void SpawnPlatforms() { /* Deprecated */ } 
    public virtual void SpawnEnemies() { Debug.Log($"{GetType().Name}: Spawning enemies..."); }
    public virtual void SpawnCollectibles() { Debug.Log($"{GetType().Name}: Spawning collectibles..."); }
    public virtual void SetupBackground() { Debug.Log($"{GetType().Name}: Setting up background..."); }
    public virtual void SetupFloor(){Debug.Log($"{GetType().Name}] SetupFloor not implemented.");
}



    public virtual void ClearAllObjects()
    {
        if (_objectPoolManager != null)
        {
            //  FIX 5: คืน Platform ที่เหลือทั้งหมดกลับเข้า Pool
            for (int i = _activePlatforms.Count - 1; i >= 0; i--)
            {
                GameObject platform = _activePlatforms[i];
                if (platform != null)
                    _objectPoolManager.ReturnToPool(GetObjectTag(platform), platform); 
            }
        }
        _activePlatforms.Clear();

        Debug.Log($"{GetType().Name}: Clearing all objects...");
    }
    #endregion

    #region Endless Generation Implementation

    protected IEnumerator GeneratePlatformsLoop()
    {
        while (_generationPivot != null) // ให้ Loop หยุดเมื่อ _generationPivot ถูกทำลาย
        {
            // สร้าง Platform ใหม่เมื่อ Pivot (Player) เข้าใกล้ตำแหน่งสร้าง Platform ถัดไป
            if (_generationPivot.position.x > _nextSpawnX - (_platformWidth * 2))
            {
                SpawnNextPlatform();
            }

            RecycleOffScreenPlatforms();

            yield return null; 
        }
    }

    protected void SpawnNextPlatform(bool isStarter = false)
    {
        if (_objectPoolManager == null) return;

        string assetKey = NormalPlatformKey;
        if (!isStarter && Random.value < 0.2f) // 20% โอกาสเป็น Breakable
        {
            assetKey = BreakPlatformKey;
        }

        //  ใช้ ObjectPoolManager.SpawnFromPool()
        GameObject newPlatform = _objectPoolManager.SpawnFromPool(assetKey, Vector3.zero, Quaternion.identity); 
        
        if (newPlatform == null)
        {
            Debug.LogError($"[{GetType().Name}] Failed to get object from pool for key: {assetKey}");
            return;
        }

        // ... (Logic คำนวณ Spawn Position เดิม) ...
        Vector3 spawnPosition = Vector3.zero;

        if (isStarter)
        {
            spawnPosition = new Vector3(_nextSpawnX, _spawnStartPosition.y, 0f);
            _nextSpawnX += _platformWidth;
        }
        else
        {
            float xOffset = Random.Range(_minXOffset, _maxXOffset);
            float yOffset = Random.Range(_minYOffset, _maxYOffset);
            
            _nextSpawnX += xOffset;
            float baseY = _spawnStartPosition.y;
            if (_activePlatforms.Count > 0)
                baseY = _activePlatforms[^1].transform.position.y;

            spawnPosition = new Vector3(_nextSpawnX, baseY + yOffset, 0f);
            _nextSpawnX += _platformWidth;
        }

        // 3. ตั้งค่า Transform
        newPlatform.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        newPlatform.transform.SetParent(this.transform); 
        newPlatform.SetActive(true); 

        _activePlatforms.Add(newPlatform);
        
        // Debug.Log($"[{GetType().Name}] Spawned {assetKey} at X: {spawnPosition.x}");
    }

    protected void RecycleOffScreenPlatforms()
    {
        if (_objectPoolManager == null || _generationPivot == null) return;

        float recycleXThreshold = _generationPivot.position.x - 15f; 
        
        for (int i = _activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject platform = _activePlatforms[i];
            
            if (platform != null && platform.transform.position.x < recycleXThreshold)
            {
                _activePlatforms.RemoveAt(i);
                
                //  ใช้ ObjectPoolManager.ReturnToPool()
                _objectPoolManager.ReturnToPool(GetObjectTag(platform), platform); 
            }
        }
    }
    
    // Helper function เพื่อดึงชื่อ Prefab (Tag) ที่แท้จริงออกมาจากชื่อ GameObject
    protected string GetObjectTag(GameObject obj)
    {
        string name = obj.name;
        int index = name.IndexOf("(Clone)");
        if (index > 0)
            return name.Substring(0, index).Trim();
        return name;
    }

    #endregion
}