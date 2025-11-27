using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles spawning of enemies according to the current <see cref="MapType"/>.
/// Integrates with <see cref="IObjectPool"/> for performance and reuse.
/// </summary>
public class EnemySpawner : MonoBehaviour, ISpawn
{
    #region Fields
    [Header("Spawner Settings")]
    [SerializeField] private MapType _mapType = MapType.None;
    [SerializeField] private List<GameObject> _enemyPrefabs = new();
    [SerializeField] private List<Transform> _spawnPoints = new();
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxEnemies = 5;

    


    [Header("Runtime Data")]
    [SerializeField] private int _currentWaveCount = 0;
    [SerializeField] private List<GameObject> _activeEnemies = new();

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    [SerializeField] private DistanceCulling _cullingManager;


    [Header("Injected Managers")]
    [SerializeField] private Player _player; 
    [SerializeField] private CollectibleSpawner _collectibleSpawner; 
    [SerializeField] private CardManager _cardManager;
    protected BuffManager _buffManagerRef;

    [Header("GoldenMon Difficulty Scaling")]
    [SerializeField] private AnimationCurve _goldenChanceCurve =
    new AnimationCurve(
        new Keyframe(0f, 0.10f),     // เริ่มเกม: 10%
        new Keyframe(300f, 0.08f),   // ระยะกลาง: 8%
        new Keyframe(1500f, 0.05f),  // คงที่ช่วงกลางเกม
        new Keyframe(2500f, 0.07f)   // ท้ายเกมเพิ่มนิดหน่อย
    );

    #region  Enemy First
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private float _spawnStartDistance = 100f;
    [SerializeField] private bool _firstEnemySpawned = false;
    [SerializeField] private EnemyType _firstEnemyType = EnemyType.MamaMon; // บังคับเจอตัวนี้ก่อน

    [Header("Spawn Control")]
    [Tooltip("ระยะห่างขั้นต่ำ (World Unit) ระหว่างศัตรูแต่ละตัวที่สปาว")]
    [SerializeField] private float _minSpawnSpacing = 10f;     
    private Vector3 _lastEnemySpawnPosition = Vector3.negativeInfinity; 
    [Tooltip("Vertical lift to place the enemy on the platform/floor.")]
    [SerializeField] private float _verticalSpawnOffset = 0.5f;


    #endregion

    private List<GameObject> _validPrefabsCache = new();



    /// <summary>
    /// Event triggered every time a new enemy is spawned from the pool.
    /// Payload: The Enemy component of the newly spawned object.
    /// </summary>
    public event System.Action<Enemy> OnEnemySpawned;

    #endregion


    #region Platform Settings
    /// <summary>
    /// Determines the recommended maximum number of enemies based on the current platform.
    /// </summary>
    private int GetRecommendedEnemyCount()
    {
#if UNITY_WEBGL
            return 4;
#elif UNITY_ANDROID || UNITY_IOS
            return 5;
#else
        return 8; // PC / Console
#endif
    }
    #endregion


    #region Initialization
    /// <summary>
    /// Initializes the spawner with a given object pool and map context.
    /// </summary>
    public void InitializeSpawner(IObjectPool pool, MapType mapType, Player player, CollectibleSpawner collectibleSpawner, CardManager cardManager, BuffManager buffManager)
    {
        _objectPool = pool;
        _mapType = mapType;
        this._player = player;
        this._collectibleSpawner = collectibleSpawner;
        this._cardManager = cardManager;
        this._buffManagerRef = buffManager;
        
        Debug.Log("[EnemySpawner] Initialized with dependencies (Player, CollectibleSpawner, CardManager).");

        _maxEnemies = GetRecommendedEnemyCount();

        CacheValidEnemies();

        Debug.Log($"[EnemySpawner] Initialized for map: {_mapType.ToFriendlyString()} | " + $"Platform: {Application.platform} | MaxEnemies={_maxEnemies}");
    }

    /// <summary>
    /// Filters the main prefab list based on the current MapType and stores the result in a cache.
    /// This prevents repeated GC allocation from LINQ in the Spawn methods.
    /// </summary>
    private void CacheValidEnemies()
    {
        _validPrefabsCache.Clear();
        foreach (var prefab in _enemyPrefabs)
                {
                    var enemyComp = prefab.GetComponent<Enemy>();
                    // ต้อง assume ว่า EnemyTypeExtensions.CanAppearInMap มีอยู่
                    if (enemyComp != null && enemyComp.EnemyType.CanAppearInMap(_mapType))
                    {
                        _validPrefabsCache.Add(prefab);
                    }
                }
        
        if (_validPrefabsCache.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] Caching resulted in 0 valid enemies for map {_mapType}. Check CanAppearInMap logic.");
        }
    }
    #endregion


    #region Enemy Handling & BuffMap Logic
        
/// <summary>
    /// Event handler for enemy death, used to trigger BuffMap effects and Despawn.
    /// </summary>
    private void HandleEnemyDied(Enemy enemy)
    {
        
        if (enemy == null) return;
        if (!_activeEnemies.Contains(enemy.gameObject)) return;
        
        // 1: เก็บตำแหน่งไว้ในตัวแปรท้องถิ่นก่อน
        // ป้องกัน MissingReferenceException เมื่อ Spawn GoldenMon
        Vector3 deathPosition = enemy.transform.position; 
        
        // 1. Clean up from active list and event
        _activeEnemies.Remove(enemy.gameObject);
        enemy.OnEnemyDied -= HandleEnemyDied;
        
        // 2. SingerDuck BuffMap Logic (ใช้ deathPosition แทน enemy.transform.position)
        if (_player != null && _player.CurrentCareerID == DuckCareer.Singer)
        {
            // ระยะที่ player วิ่ง → ใช้คำนวณสเกลความยาก
            float distance = Mathf.Max(0f, _player.transform.position.x);

            float goldenChance = Mathf.Clamp01(_goldenChanceCurve.Evaluate(distance));

            if (Random.value <= goldenChance)
            {
                SpawnSpecificEnemy(EnemyType.GoldenMon, deathPosition);
            }

        }
        
        // 3: สั่ง Despawn/ReturnToPool ทันทีหลังจบ Logic ทั้งหมด
        // นี่คือส่วนที่ขาดหายไปซึ่งทำให้มอนสเตอร์ค้างอยู่ในฉาก
        Despawn(enemy.gameObject); 
    }
    #endregion



    #region ISpawn Implementation
    /// <summary>
    /// Spawns a random valid enemy for the current map.
    /// </summary>
    public void Spawn()
    {
        if (_player == null || _player.transform == null) return;
        float distance = Mathf.Max(0f, _player.transform.position.x);

        // ยังไม่ถึงระยะเริ่มต้น → ไม่สปาว
        if (distance < _spawnStartDistance)
            return;

        if (_spawnPoints.Count == 0 || _enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Missing spawn points or enemy prefabs.");
            return;
        }

        if (_activeEnemies.Count >= _maxEnemies || _validPrefabsCache.Count == 0)
            return;

    // ---------------- Spawn Check & Reserve ----------------

    int randomPoint = Random.Range(0, _spawnPoints.Count);
    Vector3 spawnPos = _spawnPoints[randomPoint].position;
    Quaternion spawnRot = _spawnPoints[randomPoint].rotation;

    Vector3 finalSpawnPos = spawnPos;
    finalSpawnPos.y += _verticalSpawnOffset;

    if (Vector3.Distance(finalSpawnPos, _lastEnemySpawnPosition) < _minSpawnSpacing)
    {
        return; // ห่างไม่พอ ไม่สร้าง
    }


    if (!SpawnSlot.Reserve(spawnPos))
    {
        return; // Slot ถูกจองแล้ว ไม่สร้าง (ทับ Asset/Collectible/Enemy อื่น)
    }
    // ---------------- Reservation Complete ----------------

        GameObject prefab;

        // สปาว MamaMon ตัวแรกเสมอ
        if (!_firstEnemySpawned)
                {
                    // FIX CS0414: ใช้ _firstEnemyType
                    prefab = FindSpecificPrefab(_firstEnemyType); 

                    if (prefab == null)
                    {
                        Debug.LogError($"[EnemySpawner] First Enemy Type ({_firstEnemyType}) prefab not found. Falling back to weighted spawn.");
                        prefab = GetWeightedEnemyPrefab();
                    }
                    else
                    {
                        _firstEnemySpawned = true;
                        Debug.Log($"🟣 FirstEnemy Forced → {prefab.name}");
                    }
                }
                else
                {
                    // ตัวถัดไปสุ่ม WeightedSpawn ปกติ
                    prefab = GetWeightedEnemyPrefab();
                }

                if (prefab == null)
                {
                    Debug.LogWarning($"[EnemySpawner] Failed to pick a prefab. Check weighted logic.");
                    SpawnSlot.Unreserve(spawnPos); 
                    return;
                }

        string objectTag = prefab.name;

        // ---------------- Debug Weighted Details (ใช้ logic เดิมไม่ยุ่ง) ----------------
        float currentDistance = Mathf.Max(0f, _player.transform.position.x);
        float totalWeight = CalculateTotalWeight(currentDistance);
        
        // ต้องหา Component ใหม่ เพราะ GetWeightedEnemyPrefab ใช้ Random.value แล้ว
        float selectedWeight = GetWeightForEnemy(prefab.GetComponent<Enemy>().EnemyType, currentDistance);
        float normalizedChance = totalWeight > 0 ? selectedWeight / totalWeight : 0f;

        Debug.Log(
            $"🟢 [WeightedSpawn]\n" +
            $"• Picked: {prefab.name}\n" +
            $"• Distance: {currentDistance:F0}\n" +
            $"• Weight: {selectedWeight:F3} / Total {totalWeight:F3}\n" +
            $"• Chance: {normalizedChance:P2}"
        );
        

        // ---------------- Spawn ----------------

        var enemyGO = _objectPool.SpawnFromPool(objectTag, finalSpawnPos, spawnRot);

        if (enemyGO != null)
        {
            _lastEnemySpawnPosition = finalSpawnPos;
            _activeEnemies.Add(enemyGO);
            _cullingManager?.RegisterObject(enemyGO);

            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent))
            {
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);
                enemyComponent.OnEnemyDied += HandleEnemyDied;
                OnEnemySpawned?.Invoke(enemyComponent);
            }
            Debug.Log($"[EnemySpawner] Spawned {enemyGO.name} at {spawnPos}");
        }
        else
        {
            SpawnSlot.Unreserve(spawnPos);
        }
    }
    #endregion


    public GameObject SpawnAtPosition(Vector3 position)
    {
        if (_validPrefabsCache.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] No valid enemies for map {_mapType}.");
            return null;
        }

        Vector3 finalPos = position;
        finalPos.y += _verticalSpawnOffset; 
            
            // Slot Reservation (MapGenerator ไม่ได้เช็ค Slot สำหรับศัตรู)
        if (!SpawnSlot.Reserve(finalPos)) 
            {
                // Slot ถูกจองแล้ว เช่น ทับกับ Collectible/Asset ที่เกิดบน Platform เดียวกัน
                Debug.LogWarning($"[EnemySpawner] SpawnAtPosition skipped: Slot reserved by another object at {finalPos}.");
                return null; 
            }

        int randomEnemy = Random.Range(0, _validPrefabsCache.Count);
        var enemyGO = _objectPool.SpawnFromPool(_validPrefabsCache[randomEnemy].name, finalPos, Quaternion.identity);
        
        if (enemyGO != null)
        {
            _activeEnemies.Add(enemyGO);
            _cullingManager?.RegisterObject(enemyGO);
            
            // [NEW FIX] INJECT DEPENDENCIES & SUBSCRIBE
            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent))
            {
                // 1. INJECT DEPENDENCIES (DI)
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);

                // 2. SUBSCRIBE TO DEATH EVENT
                enemyComponent.OnEnemyDied += HandleEnemyDied;
                
                // 3. INVOKE SPAWN EVENT
                OnEnemySpawned?.Invoke(enemyComponent);
            }
            else
            {
                // ถ้า Spawn ไม่สำเร็จ ต้อง Unreserve
                SpawnSlot.Unreserve(finalPos);
            }
        }
        return enemyGO;
    }


#region Weighted Spawn Helpers (No LINQ)
    
    private float CalculateTotalWeight(float distance)
    {
        float totalWeight = 0f;
        // Refactored: ใช้ foreach loop แทน Linq
        foreach (var prefab in _validPrefabsCache)
        {
            var enemyComp = prefab.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                // Note: GetWeightForEnemy handles the GoldenMon block logic internally
                totalWeight += GetWeightForEnemy(enemyComp.EnemyType, distance);
            }
        }
        return totalWeight;
    }

    private float GetWeightForEnemy(EnemyType type, float distance)
    {
        // Logic เดิมของ GoldenMon คือใช้ Curve และ Common คือ 1
        return type switch
        {
            EnemyType.GoldenMon => _goldenChanceCurve.Evaluate(distance), // Rare scale by distance
            _ => 1f // Common enemies weight = 1
        };
    }


    private GameObject GetWeightedEnemyPrefab()
    {
        if (_player == null || _player.transform == null) return null;
        
        float distance = Mathf.Max(0f, _player.transform.position.x);

        // น้ำหนักเริ่มต้นศัตรูทั่วไป
        float commonWeight = 1f;
        float rareWeight = _goldenChanceCurve.Evaluate(distance);

        List<(GameObject prefab, float weight)> weighted = new();
        float total = 0f;
        
        // Refactored: ใช้ foreach loop แทน Linq
        foreach (var prefab in _validPrefabsCache)
        {
            var type = prefab.GetComponent<Enemy>().EnemyType;
            float currentWeight = commonWeight;

            if (type.IsRare())
            {
                float playTime = GameManager.Instance.PlayTime;

                //  บล็อก GoldenMon ช่วงต้นเกม (ใช้ PlayTime)
                if (playTime < 300f) 
                {
                    currentWeight = 0f;
                }
                else
                {
                    currentWeight = rareWeight;
                }
            }
            
            weighted.Add((prefab, currentWeight));
            total += currentWeight;
        }

        // 2. Safety check and fallback
        if (total <= 0f)
        {
            // Fallback: Find the first common enemy.
            foreach (var w in weighted)
            {
                if (w.weight > 0f && w.prefab.GetComponent<Enemy>()?.EnemyType.IsCommon() == true)
                {
                    return w.prefab;
                }
            }
            // Fallback สุดท้าย: คืน prefab แรกสุดใน list (กัน crash)
            if (_validPrefabsCache.Count > 0) return _validPrefabsCache[0];
            return null; 
        }

        // 3. Weighted Random Selection
        float r = Random.value * total;
        
        foreach (var w in weighted)
        {
            r -= w.weight;
            if (r <= 0f) return w.prefab;
        }

        return weighted[weighted.Count - 1].prefab;
    }
    
    private GameObject FindSpecificPrefab(EnemyType type)
    {
        // Refactored: ใช้ foreach loop แทน FirstOrDefault
        foreach (var prefab in _enemyPrefabs)
        {
            var enemyComp = prefab.GetComponent<Enemy>();
            if (enemyComp != null && enemyComp.EnemyType == type)
            {
                return prefab;
            }
        }
        return null;
    }
    #endregion


    public void Despawn(GameObject enemy)
    {
        if (enemy == null) return;

        _cullingManager?.UnregisterObject(enemy);
        _activeEnemies.Remove(enemy);

        // เรียกคืนเข้าพูลก่อน
        _objectPool.ReturnToPool(enemy.name, enemy);

        // ถ้ายัง active/ยัง enable หลังจากคืน → แปลว่า Pool ยังไม่จัดการ → Destroy
        if (enemy.activeSelf)
        {
            Debug.LogWarning($"[EnemySpawner] Pool busy → Destroying {enemy.name}");
            Destroy(enemy);
        }
    }


    public int GetSpawnCount() => _activeEnemies.Count;


/// <summary>
/// Spawns a specific enemy type at a given position.
/// Used by special skills (e.g., SingerDuck).
/// </summary>
/// <param name="type">The specific EnemyType to spawn.</param>
/// <param name="position">The world position to spawn at.</param>
    public GameObject SpawnSpecificEnemy(EnemyType type, Vector3 position)
    {
        if (_objectPool == null)
        {
            Debug.LogWarning("[EnemySpawner] Object Pool not initialized. Cannot spawn specific enemy.");
            return null;
        }

        // Find the prefab that matches the requested EnemyType (LINQ is okay here as it's not frequent)
        GameObject prefabToSpawn = FindSpecificPrefab(type); 

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[EnemySpawner] No prefab found for EnemyType: {type}");
                return null;
            }

            string objectTag = prefabToSpawn.name;

        var enemyGO = _objectPool.SpawnFromPool(objectTag, position, Quaternion.identity);
        
        if (enemyGO != null) 
        { 
            _activeEnemies.Add(enemyGO); 
            _cullingManager?.RegisterObject(enemyGO); 
            Debug.Log($"[EnemySpawner] Spawned specific enemy {enemyGO.name} at {position}"); 

            // [NEW FIX] INJECT DEPENDENCIES & SUBSCRIBE
            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent)) 
            { 
                // 1. INJECT DEPENDENCIES (DI)
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);

                // 2. SUBSCRIBE TO DEATH EVENT (เพื่อให้ HandleEnemyDied จัดการ Despawn)
                enemyComponent.OnEnemyDied += HandleEnemyDied; 

                // 3. INVOKE SPAWN EVENT 
                OnEnemySpawned?.Invoke(enemyComponent); 
            } 
        } 
        return enemyGO;

    }


    #region Wave Control
    /// <summary>
    /// Starts spawning waves of enemies over time.
    /// </summary>
    public IEnumerator StartWave()
    {
        Debug.Log($"[EnemySpawner] Starting wave {_currentWaveCount + 1} on {_mapType}...");
        _currentWaveCount++;

        while (_activeEnemies.Count < _maxEnemies)
        {
            Spawn();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }
    #endregion

}
