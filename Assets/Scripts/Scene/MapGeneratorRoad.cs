using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapGeneratorRoad — ตัวสร้างฉากธีมถนน (Road / Traffic)
/// สืบทอดจาก MapGeneratorBase และ override การ spawn ให้เหมาะกับธีมนี้
/// </summary>
public class MapGeneratorRoad : MapGeneratorBase
{
    #region Fields
    [Header("Road Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_RoadTraffic_";
    [SerializeField] private string _backgroundKey = "map_bg_RoadTraffic";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new();
    #endregion

    #region Override Methods
    public override void GenerateMap()
    {
        Debug.Log("🚧 Generating Road Map...");
        LoadAssets();
        SetupBackground();
        SpawnPlatforms();
        SpawnEnemies();
        SpawnCollectibles();
    }

    public override void SpawnPlatforms()
    {
        Debug.Log(" Spawning Road Platforms...");
        _platformSpawner?.SpawnFromPrefix(_assetPrefix);
    }

    public override void SpawnEnemies()
    {
        Debug.Log(" Spawning Road Enemies...");
        _enemySpawner?.SpawnEnemiesByTag("RoadEnemy");
    }

    public override void SpawnCollectibles()
    {
        Debug.Log(" Spawning Road Collectibles...");
        _collectibleSpawner?.SpawnCollectiblesByTag("RoadCollectible");
    }

    public override void SetupBackground()
    {
        Debug.Log(" Setting up Road Background...");
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

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
