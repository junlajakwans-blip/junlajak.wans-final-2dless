using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorKitchen : MapGeneratorBase
{
    #region Fields
    [Header("Kitchen Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_Kitchen_";
    [SerializeField] private string _backgroundKey = "map_bg_Kitchen";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new();
    #endregion

    #region Override Methods
    public override void GenerateMap()
    {
        Debug.Log("Generating Kitchen Map...");
        LoadAssets();
        SetupBackground();
        SpawnPlatforms();
        SpawnEnemies();
        SpawnCollectibles();
    }

    public override void SpawnPlatforms()
    {
        throw new System.NotImplementedException();
    }

    public override void SpawnEnemies()
    {
        throw new System.NotImplementedException();
    }

    public override void SpawnCollectibles()
    {
        throw new System.NotImplementedException();
    }

    public override void SetupBackground()
    {
        Debug.Log("Setting up Kitchen Background...");
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

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
