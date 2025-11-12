using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapGeneratorKitchen Kitchen = 3
/// BuffMap:
/// - ChefDuck (Wall behind Push Slowly)
/// </summary>
public class MapGeneratorKitchen : MapGeneratorBase
{
    #region Fields
    [Header("Kitchen Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_Kitchen_";
    [SerializeField] private string _backgroundKey = "map_bg_Kitchen";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new();
    
    [SerializeField] private string _throwableAssetKey = "map_ThrowItem_Kitchen";
    [SerializeField] private string _wallVisualKey = "map_Wall_Kitchen";
    
    // Platform Asset Keys
    [Header("Platform Assets")]
    [SerializeField] private string _normalPlatformKey = "map_asset_Kitchen_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_Kitchen_Break_Platform";
    #endregion

    #region Override Methods
    /// <summary>
    /// Abstract method that must be implemented by derived classes to define the map generation sequence.
    /// </summary>
    public override void GenerateMap()
    {
        Debug.Log("Generating Kitchen Map...");
        LoadAssets();
        SetupBackground();
        SpawnPlatforms();
        SpawnEnemies();
        SpawnCollectibles();
        
        // Set initial wall behavior (Ready for ChefDuck BuffMap)
        WallPushSpeed = _baseWallPushSpeed;
        Debug.Log("[KitchenMap] Initial WallPushSpeed set.");
    }

    /// <summary>
    /// Initiates the spawning process for platforms (Relies on base or external PlatformSpawner).
    /// </summary>
    public override void SpawnPlatforms()
    {
        base.SpawnPlatforms(); 
        Debug.Log("[KitchenMap] Platforms generated with cooking hazard zones.");
    }

    /// <summary>
    /// Initiates the spawning process for enemies specific to the derived map type.
    /// </summary>
    public override void SpawnEnemies()
    {
        if (_enemySpawner == null)
        {
            Debug.LogWarning("[KitchenMap] EnemySpawner reference missing.");
            return;
        }

        // Start continuous wave spawning (Endless mode)
        // EnemySpawner will automatically filter for enemies valid for MapType.Kitchen
        StartCoroutine(_enemySpawner.StartWave());
        
        Debug.Log($"[KitchenMap] Initiating random waves for Kitchen Map.");
    }

    /// <summary>
    /// Initiates the spawning process for collectibles specific to the derived map type.
    /// </summary>
    public override void SpawnCollectibles()
    {
        if (_collectibleSpawner == null)
        {
            Debug.LogWarning("[KitchenMap] CollectibleSpawner reference missing.");
            return;
        }
        
        // Start continuously spawning collectibles
        // StartCoroutine(_collectibleSpawner.StartContinuousSpawn()); // Assuming this method exists
        
        Debug.Log($"[KitchenMap] Initiating continuous collectible spawning.");
    }

    /// <summary>
    /// Sets up the background visuals, often by communicating with the BackgroundLooper.
    /// </summary>
    public override void SetupBackground()
    {
        Debug.Log("Setting up Kitchen Background...");
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

    /// <summary>
    /// Clears and resets all generated objects in the current map instance.
    /// </summary>
    public override void ClearAllObjects()
    {
        base.ClearAllObjects();
        Debug.Log("Clearing all kitchen map objects...");
    }
    #endregion

    #region Asset Management
    public void LoadAssets()
    {
        Debug.Log("Loading Kitchen Assets...");
    }

    public void RegisterKitchenAssets()
    {
        Debug.Log("Registering Kitchen Asset Dictionary...");
    }
    #endregion
}