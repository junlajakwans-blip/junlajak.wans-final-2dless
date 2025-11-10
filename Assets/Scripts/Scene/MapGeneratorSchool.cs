using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorSchool : MapGeneratorBase
{
    #region Fields
    [Header("School Map Settings")]
    [SerializeField] private string _assetPrefix = "map_asset_School_";
    [SerializeField] private string _backgroundKey = "map_bg_School";
    [SerializeField] private Dictionary<string, GameObject> _objectDictionary = new();
    #endregion

    #region Override Methods
    public override void GenerateMap()
    {
        Debug.Log("🏫 Generating School Map...");
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

//TODO: Add more school-specific methods here
    public override void SpawnCollectibles()
    {
        throw new System.NotImplementedException();
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
        
    }

    public void RegisterSchoolAssets()
    {
        Debug.Log(" Registering School Asset Dictionary...");
        
    }
    #endregion
}
