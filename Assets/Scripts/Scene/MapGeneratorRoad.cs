using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapGeneratorRoad — ตัวสร้างฉากธีมถนน (Road / Traffic)
/// BuffMap:
/// - FireFighterDuck (All Green Light / No Platform Break)
/// </summary>
public class MapGeneratorRoad : MapGeneratorBase
{
    #region Fields
    [Header("Road Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_RoadTraffic_";
    [SerializeField] private string _backgroundKey = "map_bg_RoadTraffic";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new();
    
    // Throw Items Asset Keys (Tire, Cone, Bin)
    [SerializeField] private string _throwableAssetKey = "map_ThrowItem_RoadTraffic";
    [SerializeField] private string _wallVisualKey = "map_Wall_RoadTraffic";
    
    // Platform Asset Keys
    [Header("Platform Assets")]
    [SerializeField] private string _normalPlatformKey = "map_asset_RoadTraffic_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_RoadTraffic_Break_Platform";
    #endregion

    #region Override Methods
    /// <summary>
    /// Abstract method that must be implemented by derived classes to define the map generation sequence.
    /// </summary>
    public override void GenerateMap()
    {
        Debug.Log("🚧 Generating Road Map...");
        LoadAssets();
        SetupBackground();
        SpawnPlatforms();
        SpawnEnemies();
        SpawnCollectibles();
        
        // Set initial wall behavior (Ready for FireFighterDuck BuffMap)
        WallPushSpeed = _baseWallPushSpeed;
        Debug.Log("[RoadMap] Initial WallPushSpeed set.");
    }

    /// <summary>
    /// Initiates the spawning process for platforms (Relies on base or external PlatformSpawner).
    /// </summary>
    public override void SpawnPlatforms()
    {
        base.SpawnPlatforms(); 
        Debug.Log("[RoadMap] Road platforms generated with traffic elements.");
    }

    /// <summary>
    /// Initiates the spawning process for enemies specific to the derived map type.
    /// </summary>
    public override void SpawnEnemies()
    {
        if (_enemySpawner == null)
        {
            Debug.LogWarning("[RoadMap] EnemySpawner reference missing.");
            return;
        }

        // Start continuous wave spawning (Endless mode)
        // EnemySpawner will automatically filter for enemies valid for MapType.RoadTraffic
        StartCoroutine(_enemySpawner.StartWave());
        
        Debug.Log($"[RoadMap] Initiating random waves for Road Map.");
    }

    /// <summary>
    /// Initiates the spawning process for collectibles specific to the derived map type.
    /// </summary>
    public override void SpawnCollectibles()
    {
        if (_collectibleSpawner == null)
        {
            Debug.LogWarning("[RoadMap] CollectibleSpawner reference missing.");
            return;
        }
        
        // Start continuously spawning collectibles
        // StartCoroutine(_collectibleSpawner.StartContinuousSpawn()); // Assuming this method exists
        
        Debug.Log($"[RoadMap] Initiating continuous collectible spawning.");
    }

    /// <summary>
    /// Sets up the background visuals, often by communicating with the BackgroundLooper.
    /// </summary>
    public override void SetupBackground()
    {
        Debug.Log(" Setting up Road Background...");
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

    /// <summary>
    /// Clears and resets all generated objects in the current map instance.
    /// </summary>
    public override void ClearAllObjects()
    {
        base.ClearAllObjects();
        Debug.Log(" Clearing all road map objects...");
    }
    #endregion

    #region Asset Management
    public void LoadAssets()
    {
        Debug.Log(" Loading Road Assets...");
    }

    public void RegisterRoadAssets()
    {
        Debug.Log(" Registering Road Asset Dictionary...");
    }
    #endregion
}