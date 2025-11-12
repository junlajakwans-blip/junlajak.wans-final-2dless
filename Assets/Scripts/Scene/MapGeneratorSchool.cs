using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapGeneratorSchool School = 1,
/// BuffMap
/// - ProgrammerDuck (Wall behind Push Slowly)
/// </summary>
public class MapGeneratorSchool : MapGeneratorBase
{
    #region Fields
    [Header("School Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_School_";
    [SerializeField] private string _backgroundKey = "map_bg_School";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new();
    
    //ThrowableItem find name
    [SerializeField] private string _throwableAssetKey = "map_ThrowItem_School";
    [SerializeField] private string _wallVisualKey = "map_Wall_School";

    // Platform Asset Keys
    [Header("Platform Assets")]
    [SerializeField] private string _normalPlatformKey = "map_asset_School_Normal_Platform"; 
    [SerializeField] private string _breakPlatformKey = "map_asset_School_Break_Platform";   
    #endregion

    #region Override Methods
    public override void GenerateMap()
    {
        Debug.Log("Generating School Map...");
        LoadAssets();
        SetupBackground();
        SpawnPlatforms();
        SpawnEnemies();
        SpawnCollectibles();
        
        // Set initial wall behavior (Ready for ProgrammerDuck BuffMap)
        WallPushSpeed = _baseWallPushSpeed;
        Debug.Log("[SchoolMap] Initial WallPushSpeed set.");
    }

    public override void SpawnPlatforms()
    {
        // Implement Platform Spawn Logic
        base.SpawnPlatforms(); 
        Debug.Log("[SchoolMap] School platforms generated with stationary objects.");
    }

    public override void SpawnEnemies()
    {
        // FIX: Implement Enemy Spawn Logic
        if (_enemySpawner == null)
        {
            Debug.LogWarning("[SchoolMap] EnemySpawner reference missing.");
            return;
        }

        // Start continuous wave spawning (Endless mode)
        // EnemySpawner will automatically filter for enemies valid for MapType.School
        StartCoroutine(_enemySpawner.StartWave());
        
        Debug.Log($"[SchoolMap] Initiating random waves for School Map.");
    }

    public override void SpawnCollectibles()
    {
        // Implement Collectible Spawn Logic
        if (_collectibleSpawner == null)
        {
            Debug.LogWarning("[SchoolMap] CollectibleSpawner reference missing.");
            return;
        }
        
        // Start continuously spawning collectibles
        // StartCoroutine(_collectibleSpawner.StartContinuousSpawn()); // Assuming this method exists
        
        Debug.Log($"[SchoolMap] Initiating continuous collectible spawning.");
    }

    public override void SetupBackground()
    {
        Debug.Log(" Setting up School Background...");
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

    public override void ClearAllObjects()
    {
        base.ClearAllObjects();
        Debug.Log(" Clearing all school map objects...");
    }
    #endregion

    #region Asset Management
    public void LoadAssets()
    {
        Debug.Log(" Loading School Assets...");
        // TODO: Implement logic to load assets
    }

    public void RegisterSchoolAssets()
    {
        Debug.Log(" Registering School Asset Dictionary...");
    }
    #endregion
}