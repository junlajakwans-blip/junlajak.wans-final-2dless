using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles spawning of enemies according to the current <see cref="MapType"/>.
/// Integrates with a dedicated internal pooling system for performance and reuse.
/// This class also manages enemy difficulty scaling and break periods between waves.
/// Implements the <see cref="ISpawn"/> interface (assumed).
/// </summary>
public class EnemySpawner : MonoBehaviour, ISpawn
{
    #region Fields

    [Header("Spawner Configuration")]
    [SerializeField] private MapType _mapType = MapType.None;
    [SerializeField] private List<GameObject> _enemyPrefabs = new();
    [SerializeField] private float _spawnInterval = 2f;

    [Tooltip("The maximum number of enemies allowed on screen simultaneously (overridden by platform).")]
    [SerializeField] private int _maxEnemies = 5;

    [Header("Spawn Control")]
    [Tooltip("The minimum player distance (X-axis) required to start spawning.")]
    [SerializeField] private float _spawnStartDistance = 5f;
    [Tooltip("Minimum world unit distance between consecutive enemy spawns.")]
    [SerializeField] private float _minSpawnSpacing = 10f; 
    [Tooltip("Vertical lift to place the enemy on the platform/floor.")]
    [SerializeField] private float _verticalSpawnOffset = 0.05f;

    [Header("GoldenMon Difficulty Scaling")]
    [SerializeField] private AnimationCurve _goldenChanceCurve =
    new AnimationCurve(
        new Keyframe(0f, 0.10f),    // Start: 10%
        new Keyframe(300f, 0.08f),  // Mid-range: 8%
        new Keyframe(1500f, 0.05f), // Steady mid-game
        new Keyframe(2500f, 0.07f)  // Late game increase
    );

    [Header("Runtime Data")]
    [SerializeField] private int _currentWaveCount = 0;
    [SerializeField] private List<GameObject> _activeEnemies = new();
    
    // Tracks the last successful spawn position to enforce _minSpawnSpacing
    private Vector3 _lastEnemySpawnPosition = Vector3.negativeInfinity;
    private bool _firstEnemySpawned = false;
    [SerializeField] private EnemyType _firstEnemyType = EnemyType.MamaMon; // Forced first enemy type

    // Used to queue safe, non-breakable platform positions from MapGenerator
    private readonly Queue<Vector3> _safeGroundQueue = new();

    // Internal dedicated object pool for enemies (avoids using IObjectPool for quick access)
    private Dictionary<string, Queue<GameObject>> _enemyPoolDictionary = new();
    // Cache of enemy prefabs valid for the current map type
    private List<GameObject> _validPrefabsCache = new();

    [Header("References")]
    [SerializeField] private IObjectPool _objectPool;
    [SerializeField] private DistanceCulling _cullingManager;

    [Header("Injected Managers")]
    [SerializeField] private Player _player; 
    [SerializeField] private CollectibleSpawner _collectibleSpawner; 
    [SerializeField] private CardManager _cardManager;
    protected BuffManager _buffManagerRef;

    /// <summary>
    /// Event triggered every time a new enemy is spawned from the pool.
    /// Payload: The Enemy component of the newly spawned object.
    /// </summary>
    public event System.Action<Enemy> OnEnemySpawned;
    
    // Cooldown for extra buff spawns (e.g., GoldenMon from SingerDuck skill)
    private float _lastBuffSpawnTime = 0f;
    private float _buffSpawnCooldown = 4f; 

    #endregion

    #region Platform Configuration
    
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

    #region Unity Lifecycle

    private void LateUpdate()
    {
        // Check active enemies for null or falling off the map
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject e = _activeEnemies[i];
            if (e == null) 
            {
                _activeEnemies.RemoveAt(i);
                continue;
            }

            // Check if the enemy fell below the map boundary
            if (e.transform.position.y < -3f) 
            {
                Debug.Log($"[EnemySpawner] Auto-despawn (fell off map): {e.name}");
                Despawn(e);
            }
        }
    }
    
    #endregion

    #region Initialization & Setup
    
    /// <summary>
    /// Sets core manager dependencies, typically called by a base MapGenerator class.
    /// </summary>
    public void SetDependencies(Player player, CollectibleSpawner collectibleSpawner, CardManager cardManager, BuffManager buffManager, IObjectPool pool, DistanceCulling culling)
    {
        this._player = player;
        this._collectibleSpawner = collectibleSpawner;
        this._cardManager = cardManager;
        this._buffManagerRef = buffManager;
        this._objectPool = pool;
        this._cullingManager = culling;
        Debug.Log("[EnemySpawner] Base Dependencies Set (Player, Managers, Pool, Culling).");

        
        if (_player != null && _collectibleSpawner != null && _cardManager != null && _buffManagerRef != null)
        {
            if (!IsInvoking(nameof(SpawnWaveTick)))
            {
                Debug.Log("[EnemySpawner] Auto-starting wave after full DI.");
                StartWaveRepeating();
            }
        }
    }


    /// <summary>
    /// Initializes the spawner with the map context, object pool, and maximum enemy count.
    /// This method should be called by the specific MapGenerator (e.g., MapGeneratorSchool).
    /// </summary>
    public void InitializeSpawner(IObjectPool pool, MapType mapType, Player player, CollectibleSpawner collectibleSpawner, CardManager cardManager, BuffManager buffManager)
    {
        // 1. Set Dependencies if not already set by SetDependencies
        if (this._objectPool == null)
        {
            this._objectPool = pool;
            this._player = player;
            this._collectibleSpawner = collectibleSpawner;
            this._cardManager = cardManager;
            this._buffManagerRef = buffManager;
        }

        // 2. Set Map-specific context
        _mapType = mapType;
        
        Debug.Log("[EnemySpawner] Initialized with MapType and all dependencies.");

        _maxEnemies = GetRecommendedEnemyCount();

        CacheValidEnemies();

        InitializeEnemyPools();

        // Note: ToFriendlyString is an assumed extension method on MapType
        Debug.Log($"[EnemySpawner] Initialized for map: {_mapType} | Platform: {Application.platform} | MaxEnemies={_maxEnemies}");
    }

    /// <summary>
    /// Creates and pre-populates the dedicated internal enemy pools.
    /// </summary>
    private void InitializeEnemyPools()
    {
        foreach (var prefab in _validPrefabsCache)
        {
            string tag = prefab.name;
            if (!_enemyPoolDictionary.ContainsKey(tag))
            {
                _enemyPoolDictionary[tag] = new Queue<GameObject>();
                // Pre-spawn 5 instances per enemy type
                for (int i = 0; i < 5; i++)
                {
                    var obj = Instantiate(prefab, transform); 
                    obj.SetActive(false);
                    _enemyPoolDictionary[tag].Enqueue(obj);
                }
            }
        }
        Debug.Log($"[EnemySpawner] Initialized dedicated pools for {_enemyPoolDictionary.Count} enemy types.");
    }
    
    /// <summary>
    /// Filters the main prefab list based on the current <see cref="MapType"/> 
    /// and stores the result in a cache to improve runtime performance.
    /// </summary>
    private void CacheValidEnemies()
    {
        _validPrefabsCache.Clear();
        foreach (var prefab in _enemyPrefabs)
        {
            var enemyComp = prefab.GetComponent<Enemy>();
            // Must assume that EnemyTypeExtensions.CanAppearInMap exists
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

    #region Enemy Death & Buff Logic
        
    /// <summary>
    /// Event handler for enemy death, used to trigger BuffMap effects and Despawn.
    /// </summary>
    private void HandleEnemyDied(Enemy enemy)
    {
        if (!_activeEnemies.Contains(enemy.gameObject))
        {
            Debug.LogWarning($"[EnemySpawner] {enemy.name} died BUT was not registered in _activeEnemies. Auto-fix.");
            _activeEnemies.Add(enemy.gameObject);
        }

        if (enemy == null) return;
        if (!_activeEnemies.Contains(enemy.gameObject)) return;

        Vector3 deathPosition = enemy.transform.position;

        _activeEnemies.Remove(enemy.gameObject);
        enemy.OnEnemyDied -= HandleEnemyDied;
        enemy.OnRequestDrop -= HandleEnemyDropRequest; // ??? memory leak
        Debug.Log($"[EnemySpawner] HandleEnemyDied for {enemy.name} | Active now: {_activeEnemies.Count}");
        // ปิด GameObject ก่อน เพื่อกันโดน hit ซ้ำ
        GameObject obj = enemy.gameObject;
        obj.SetActive(false);
        _cullingManager?.UnregisterObject(obj);

        // BuffMap Spawn (Singer → GoldenMon)
        if (_player != null && _player.CurrentCareerID == DuckCareer.Singer)
        {
            float now = Time.time;
            if (now - _lastBuffSpawnTime >= _buffSpawnCooldown)
            {
                float distance = Mathf.Max(0f, _player.transform.position.x);
                float goldenChance = Mathf.Clamp01(_goldenChanceCurve.Evaluate(distance));

                if (Random.value <= goldenChance)
                    SpawnSpecificEnemy(EnemyType.GoldenMon, deathPosition);

                _lastBuffSpawnTime = now;
            }
        }

        // Reset หลัง inactive ปลอดภัย ไม่โดน damage ซ้ำ
        enemy.ResetStateForPooling();

        // คืนเข้าคิว Pool
        ReturnEnemyToPool(obj);

        // Refill check
        if (_activeEnemies.Count > 0) return;

        float breakTime = Random.Range(5f, 7f);
        Debug.Log($"[EnemySpawner] Break time: {breakTime:F1}s before next wave.");
        Invoke(nameof(SpawnBreakWave), breakTime);
    }


    /// <summary>
    /// Handles requests from enemies to drop collectibles upon death.
    /// This method delegates the actual dropping logic to the CollectibleSpawner.
    /// </summary>
    private void HandleEnemyDropRequest(Enemy.DropRequest req)
    {
        // ultra lightweight dispatch
        if (_collectibleSpawner == null) return;
        _collectibleSpawner.DropCollectible(req.Type, req.Position);
    }

    /// <summary>
    /// Executed after the break time expires to restart a small wave.
    /// </summary>
    private void SpawnBreakWave()
    {
        // If enemies were spawned naturally during the break, cancel the break wave
        if (_activeEnemies.Count > 0) return;

        Debug.Log("[EnemySpawner] Break ended. Wave resumed.");

        // Spawn a few enemies to restart the wave
        for (int i = 0; i < Mathf.Min(3, _maxEnemies); i++)
            Spawn();

        // Let InvokeRepeating gradually refill the rest
    }

    /// <summary>
    /// Spawns a specific enemy type at a given position.
    /// Used by special skills (e.g., SingerDuck) or events.
    /// </summary>
    /// <param name="type">The specific EnemyType to spawn.</param>
    /// <param name="position">The world position to spawn at.</param>
    public GameObject SpawnSpecificEnemy(EnemyType type, Vector3 position)
    {
        if (_player == null || _buffManagerRef == null || _objectPool == null)
        {
            Debug.LogWarning("[EnemySpawner] Dependencies missing. Cannot spawn specific enemy.");
            return null;
        }

        GameObject prefabToSpawn = FindSpecificPrefab(type); 

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[EnemySpawner] No prefab found for EnemyType: {type}");
            return null;
        }

        string objectTag = prefabToSpawn.name;

        // Note: SpawnSpecificEnemy does not enforce SpawnSlot reservation or min distance
        var enemyGO = SpawnEnemyInstance(objectTag, position, Quaternion.identity);
        
        if (enemyGO != null) 
        { 
            _activeEnemies.Add(enemyGO); 
            _cullingManager?.RegisterObject(enemyGO); 
            Debug.Log($"[EnemySpawner] Spawned specific enemy {enemyGO.name} at {position}"); 

            // INJECT DEPENDENCIES & SUBSCRIBE
            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent)) 
            { 
                // 1. INJECT DEPENDENCIES (DI)
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);

                // 2. SUBSCRIBE TO DEATH & DROP EVENT (Allows HandleEnemyDied to manage despawn)
                enemyComponent.OnEnemyDied += HandleEnemyDied; 
                enemyComponent.OnRequestDrop += HandleEnemyDropRequest; // <-- NEW SUBSCRIPTION

                // 3. INVOKE SPAWN EVENT 
                OnEnemySpawned?.Invoke(enemyComponent); 
            } 
        } 
        return enemyGO;
    }
    
    #endregion

    #region Spawn Core Logic
    
    /// <summary>
    /// Registers a safe, non-breakable platform position provided by the MapGenerator.
    /// Enemies will be spawned at positions drawn from this queue.
    /// </summary>
    /// <param name="platformPos">The safe position.</param>
    /// <param name="isBreakable">If the platform can be broken (should be false for enemies).</param>
    public void RegisterSafeGround(Vector3 platformPos, bool isBreakable)
    {
        // Only queue non-breakable platforms
        if (isBreakable) return;

        Debug.Log($"[SafeGround] Registered: {platformPos.x:F1}");
        _safeGroundQueue.Enqueue(platformPos);
    }

    /// <summary>
    /// Spawns a random valid enemy for the current map, ensuring distance and spacing constraints are met.
    /// </summary>
    public void Spawn()
    {
        Debug.Log($"[Spawn Attempt] Player distance={_player.transform.position.x:F1}");

        if (_player == null || _player.transform == null) return;
        float distance = Mathf.Max(0f, _player.transform.position.x);

        // 1. Guard: Check minimum start distance
        if (distance < _spawnStartDistance)
        {
            Debug.Log($"[EnemySpawner] Spawn skipped: Player distance ({distance:F1}) < Start distance ({_spawnStartDistance:F1}).");
            return;
        }

        if (_validPrefabsCache.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] Spawn skipped: No valid enemy prefabs available for this map.");
            return;
        }

        // 2. Guard: Check maximum enemy count
        if (_activeEnemies.Count >= _maxEnemies)
        {
            Debug.Log($"[EnemySpawner] Spawn skipped: Max enemies reached ({_activeEnemies.Count}/{_maxEnemies}).");
            return;
        }

        if (_safeGroundQueue.Count == 0)
        {
            Debug.Log("[EnemySpawner] Waiting for platform position from SafeGroundQueue...");
            return;
        }

        // Dequeue the next safe platform position
        Vector3 finalSpawnPos = _safeGroundQueue.Dequeue();
        Quaternion spawnRot = Quaternion.identity;
        finalSpawnPos.y += _verticalSpawnOffset; // Apply vertical offset

        // 3. Guard: Enemy spacing check
        if (Vector3.Distance(finalSpawnPos, _lastEnemySpawnPosition) < _minSpawnSpacing)
        {
            Debug.Log($"[EnemySpawner] Spawn skipped: Min spacing ({_minSpawnSpacing:F1}) not met. Last pos: {_lastEnemySpawnPosition.x:F1}");
            return; 
        }

        // 4. Guard: Check and reserve the spawn slot 
        // (SpawnSlot is assumed to be a static utility class for world slot reservation)
        if (!SpawnSlot.Reserve(finalSpawnPos))
        {
            Debug.LogWarning($"[EnemySpawner] Spawn skipped: Slot {Mathf.RoundToInt(finalSpawnPos.x)} is reserved (by Asset/Collectible/Platform).");
            return; 
        }
        
        GameObject prefab;

        // Force spawn the first enemy type
        if (!_firstEnemySpawned)
        {
            prefab = FindSpecificPrefab(_firstEnemyType); 

            if (prefab == null)
            {
                Debug.LogError($"[EnemySpawner] First Enemy Type ({_firstEnemyType}) prefab not found. Falling back to weighted spawn.");
                prefab = GetWeightedEnemyPrefab();
            }
            else
            {
                _firstEnemySpawned = true;
                Debug.Log($"[FirstEnemy Forced] Picked: {prefab.name} at X={finalSpawnPos.x:F1}"); 
            }
        }
        else
        {
            // Normal weighted spawn
            prefab = GetWeightedEnemyPrefab();
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[EnemySpawner] Failed to pick a prefab. Check weighted logic.");
            SpawnSlot.Unreserve(finalSpawnPos); 
            return;
        }

        string objectTag = prefab.name;

        // Debug Weighted Details
        float totalWeight = CalculateTotalWeight(distance);
        float selectedWeight = GetWeightForEnemy(prefab.GetComponent<Enemy>().EnemyType, distance);
        float normalizedChance = totalWeight > 0 ? selectedWeight / totalWeight : 0f;

        Debug.Log(
            $"[Weighted Spawn Details]\n" + 
            $"• Picked: {prefab.name}\n" +
            $"• Distance: {distance:F0}\n" +
            $"• Weight: {selectedWeight:F3} / Total {totalWeight:F3}\n" +
            $"• Chance: {normalizedChance:P2}"
        );
        
        // Final Spawn
        var enemyGO = SpawnEnemyInstance(objectTag, finalSpawnPos, spawnRot);

        if (enemyGO != null)
        {
            _lastEnemySpawnPosition = finalSpawnPos;
            _activeEnemies.Add(enemyGO);
            _cullingManager?.RegisterObject(enemyGO);

            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent))
            {
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);
                enemyComponent.OnEnemyDied += HandleEnemyDied;
                enemyComponent.OnRequestDrop += HandleEnemyDropRequest; // <-- NEW SUBSCRIPTION
                Debug.Log($"[EnemySpawner] Subscribed death handlers for {enemyGO.name}. Listeners: {enemyComponent.OnEnemyDied?.GetInvocationList()?.Length ?? 0}");
                OnEnemySpawned?.Invoke(enemyComponent);
            }
            Debug.Log($"[EnemySpawner] Spawned {enemyGO.name} at X={finalSpawnPos.x:F1}"); 
        }
        else
        {
            SpawnSlot.Unreserve(finalSpawnPos);
            Debug.LogWarning($"[EnemySpawner] Spawn failed (Instance Creation Error), unreserving slot {finalSpawnPos.x:F0}.");
        }
    }

    /// <summary>
    /// Spawns a random enemy at a position specified externally (e.g., by the MapGenerator).
    /// </summary>
    /// <param name="position">The exact world position to spawn at.</param>
    /// <returns>The spawned enemy GameObject, or null.</returns>
    public GameObject SpawnAtPosition(Vector3 position)
    {
        // Guard for overflow
        if (_activeEnemies.Count >= _maxEnemies || _validPrefabsCache.Count == 0)
            return null;

        Vector3 finalPos = position;
        finalPos.y += _verticalSpawnOffset;

        // Check and reserve slot 
        if (SpawnSlot.IsReserved(finalPos))
        {
            Debug.LogWarning($"[EnemySpawner] MapGen Spawn skipped: Slot {Mathf.RoundToInt(finalPos.x)} is already reserved.");
            return null;
        }
        
        if (!SpawnSlot.Reserve(finalPos))
        {
            Debug.LogError($"[EnemySpawner] MapGen Spawn skipped: Critical Reserve failure at {finalPos.x:F0}.");
            return null;
        }
        
        int randomEnemy = Random.Range(0, _validPrefabsCache.Count);
        string objectTag = _validPrefabsCache[randomEnemy].name;

        var enemyGO = SpawnEnemyInstance(objectTag, finalPos, Quaternion.identity);
        if (enemyGO != null)
        {
            _activeEnemies.Add(enemyGO);
            _cullingManager?.RegisterObject(enemyGO);

            if (enemyGO.TryGetComponent<Enemy>(out var enemyComponent))
            {
                enemyComponent.SetDependencies(_player, _collectibleSpawner, _cardManager, _buffManagerRef, _objectPool);
                enemyComponent.OnEnemyDied += HandleEnemyDied;
                enemyComponent.OnRequestDrop += HandleEnemyDropRequest; // <-- NEW SUBSCRIPTION
                OnEnemySpawned?.Invoke(enemyComponent);
            }
            Debug.Log($"[EnemySpawner] MapGen Spawned {enemyGO.name} at X={finalPos.x:F1}");
        }
        else
        {
            SpawnSlot.Unreserve(finalPos);
        }

        return enemyGO;
    }
    
    #endregion

    #region Enemy Pooling & Despawn

    /// <summary>
    /// Retrieves an enemy instance from the internal dedicated pool or instantiates a new one.
    /// </summary>
    /// <param name="objectTag">The name/tag of the enemy prefab.</param>
    /// <param name="position">World position to spawn at.</param>
    /// <param name="rotation">World rotation.</param>
    /// <returns>The active enemy GameObject.</returns>
    private GameObject SpawnEnemyInstance(string objectTag, Vector3 position, Quaternion rotation)
    {
        // Clean tag if it contains "(Clone)"
        if (objectTag.Contains("(Clone)"))
        {
            objectTag = objectTag.Replace("(Clone)", "").Trim();
        }
        
        if (!_enemyPoolDictionary.ContainsKey(objectTag))
        {
            Debug.LogWarning($"[EnemySpawner Pool] Tag '{objectTag}' not found. Expanding pool.");
            // Fallback: If pool doesn't exist, create it immediately
            var prefab = FindSpecificPrefab(objectTag); 
            if (prefab == null) return null;
            _enemyPoolDictionary[objectTag] = new Queue<GameObject>();
        }

        var queue = _enemyPoolDictionary[objectTag];
        GameObject obj = null;

        // Dequeue available object, skipping destroyed (null) objects
        while (queue.Count > 0 && obj == null)
        {
            obj = queue.Dequeue();
        }

        if (obj == null)
        {
            // Instantiate new if the pool is empty or all items were destroyed
            var prefab = FindSpecificPrefab(objectTag);
            if (prefab == null) return null;

            obj = Instantiate(prefab, transform);
            Debug.LogWarning($"[EnemySpawner Pool] Created NEW instance for {objectTag} (Pool empty/destroyed).");
        }

        // Reset & Activate
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }


    /// <summary>
    /// Returns an enemy instance to the internal pool and performs necessary cleanup.
    /// </summary>
    /// <param name="obj">The enemy GameObject to return.</param>
    private void ReturnEnemyToPool(GameObject obj)
    {
        if (obj == null) return;

        string objectTag = obj.name;
        if (objectTag.Contains("(Clone)"))
            objectTag = objectTag.Replace("(Clone)", "").Trim();

        if (!_enemyPoolDictionary.ContainsKey(objectTag))
        {
            Debug.LogWarning($"[ENEMY POOL ERROR] Missing dedicated pool for: {objectTag} (Destroying instance).");
            Destroy(obj);
            return;
        }

        if (obj.TryGetComponent<Enemy>(out var enemy))
        {
            // Always reset before pooled
            enemy.ResetStateForPooling();

            // Unsubscribe every time (prevent leak)
            enemy.OnEnemyDied -= HandleEnemyDied;
            enemy.OnRequestDrop -= HandleEnemyDropRequest; // ??? memory leak
        }

        obj.SetActive(false);
        _cullingManager?.UnregisterObject(obj);
        _enemyPoolDictionary[objectTag].Enqueue(obj);
        Debug.Log($"[POOL] Return {objectTag} → Queue size before = {_enemyPoolDictionary[objectTag].Count}");

    }


    /// <summary>
    /// Despawns an enemy object and returns it to the pool.
    /// Used for culling or falling off map.
    /// </summary>
    public void Despawn(GameObject enemy)
    {
        if (enemy == null) return;

        _cullingManager?.UnregisterObject(enemy);
        _activeEnemies.Remove(enemy);
        
        // Remove death handler subscription to prevent memory leaks if Despawn happens before death
        if (enemy.TryGetComponent<Enemy>(out var enemyComponent))
        {
            enemyComponent.OnEnemyDied -= HandleEnemyDied;
            // OnRequestDrop is unsubscribed in ReturnEnemyToPool
        }
        
        ReturnEnemyToPool(enemy); 
    }

    /// <summary>
    /// Gets the current count of actively spawned enemies.
    /// </summary>
    public int GetSpawnCount() => _activeEnemies.Count;

    #endregion

    #region Weighted Spawn Helpers 
    
    /// <summary>
    /// Calculates the total weight of all valid enemies for weighted random selection.
    /// </summary>
    /// <param name="distance">The current player distance.</param>
    /// <returns>The sum of all enemy weights.</returns>
    private float CalculateTotalWeight(float distance)
    {
        float totalWeight = 0f;
        foreach (var prefab in _validPrefabsCache)
        {
            var enemyComp = prefab.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                totalWeight += GetWeightForEnemy(enemyComp.EnemyType, distance);
            }
        }
        return totalWeight;
    }

    /// <summary>
    /// Gets the calculated weight for a specific enemy type based on player distance.
    /// </summary>
    /// <param name="type">The type of enemy.</param>
    /// <param name="distance">The current player distance.</param>
    /// <returns>The weight value.</returns>
    private float GetWeightForEnemy(EnemyType type, float distance)
    {
        return type switch
        {
            EnemyType.GoldenMon => _goldenChanceCurve.Evaluate(distance), // Rare scale by distance
            _ => 1f // Common enemies weight = 1
        };
    }


    /// <summary>
    /// Selects an enemy prefab based on weighted chance, scaled by player distance.
    /// </summary>
    /// <returns>The selected enemy prefab, or null if selection fails.</returns>
    private GameObject GetWeightedEnemyPrefab()
    {
        if (_player == null || _player.transform == null) return null;
        
        float distance = Mathf.Max(0f, _player.transform.position.x);

        float commonWeight = 1f;
        float rareWeight = _goldenChanceCurve.Evaluate(distance);

        List<(GameObject prefab, float weight)> weighted = new();
        float total = 0f;
        
        foreach (var prefab in _validPrefabsCache)
        {
            var type = prefab.GetComponent<Enemy>().EnemyType;
            float currentWeight = commonWeight;

            // Assumed IsRare() extension exists
            if (type.IsRare()) 
            {
                // Block rare/GoldenMon enemy during early game
                float playTime = GameManager.Instance.PlayTime;

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

        // Safety check and fallback
        if (total <= 0f)
        {
            // Fallback: Find the first common enemy.
            foreach (var w in weighted)
            {
                // Assumed IsCommon() extension exists
                if (w.weight > 0f && w.prefab.GetComponent<Enemy>()?.EnemyType.IsCommon() == true)
                {
                    return w.prefab;
                }
            }
            // Final fallback: return the first prefab in the list
            if (_validPrefabsCache.Count > 0) return _validPrefabsCache[0];
            return null; 
        }

        // Weighted Random Selection
        float r = Random.value * total;
        
        foreach (var w in weighted)
        {
            r -= w.weight;
            if (r <= 0f) return w.prefab;
        }

        return weighted[weighted.Count - 1].prefab;
    }
    
    /// <summary>
    /// Finds a specific enemy prefab from the master list using its <see cref="EnemyType"/>.
    /// </summary>
    /// <param name="type">The type of enemy to find.</param>
    /// <returns>The GameObject prefab.</returns>
    private GameObject FindSpecificPrefab(EnemyType type)
    {
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

    /// <summary>
    /// Finds a specific enemy prefab from the master list using its name/tag.
    /// </summary>
    /// <param name="objectTag">The name of the prefab.</param>
    /// <returns>The GameObject prefab.</returns>
    private GameObject FindSpecificPrefab(string objectTag)
    {
        foreach (var prefab in _enemyPrefabs)
        {
            if (prefab.name == objectTag)
            {
                return prefab;
            }
        }
        return null;
    }
    #endregion

    #region Wave Control

    /// <summary>
    /// Starts the enemy spawning loop using InvokeRepeating (reliable for WebGL).
    /// </summary>
    public void StartWaveRepeating()
    {
        // Reset important runtime data before starting a new wave
        _activeEnemies.Clear();
        _lastEnemySpawnPosition = Vector3.negativeInfinity;
        _firstEnemySpawned = false;
        Debug.Log("[EnemySpawner] Reset for new wave (ActiveEnemies cleared).");

        if (IsInvoking(nameof(SpawnWaveTick)))
        {
            Debug.LogWarning("[EnemySpawner] Wave is already running. Cancelling previous loop.");
            CancelInvoke(nameof(SpawnWaveTick));
        }
        
        Debug.Log($"[EnemySpawner] Starting wave 1 on {_mapType} using InvokeRepeating...");
        _currentWaveCount++;

        // Start the wave tick immediately, repeating every _spawnInterval
        InvokeRepeating(nameof(SpawnWaveTick), 0f, _spawnInterval);
    }
    
    /// <summary>
    /// The core spawn logic executed repeatedly by InvokeRepeating.
    /// </summary>
    private void SpawnWaveTick()
    {
        Debug.Log($"[SpawnWaveTick] ActiveEnemies: {_activeEnemies.Count}");
        
        // 1. Check Player/Game State (if dead/paused, stop the loop)
        // Must use CancelInvoke instead of coroutine 'yield break'
        if (_player == null || _player.IsDead || (GameManager.Instance != null && GameManager.Instance.IsPaused))
        {
            CancelInvoke(nameof(SpawnWaveTick));
            Debug.Log("[EnemySpawner] Wave cancelled due to player death or game pause.");
            return;
        }

        // 2. Check Max Enemy (if full, wait for the next tick)
        if (_activeEnemies.Count < _maxEnemies)
        {
            Spawn();
        }
        // else: Holding spawn
    }
    
    /// <summary>
    /// Stops the repeating spawn wave.
    /// </summary>
    public void StopWave()
    {
        CancelInvoke(nameof(SpawnWaveTick));
        Debug.Log("[EnemySpawner] Wave repeating loop stopped.");
    }
    #endregion

}

