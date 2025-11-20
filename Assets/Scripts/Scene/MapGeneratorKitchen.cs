using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorKitchen : MapGeneratorBase
{
    [Header("Kitchen Map Settings")]
    [SerializeField] private string _backgroundKey = "map_bg_Kitchen";
    [SerializeField] private string _kitchenFloorKey = "map_asset_Kitchen_Floor";

    [SerializeField] private string _throwableAssetKey = "map_ThrowItem_Kitchen";
    [SerializeField] private string _wallVisualKey = "map_Wall_Kitchen";

    [Header("Platform Assets")]
    [SerializeField] private string _normalPlatformKey = "map_asset_Kitchen_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_Kitchen_Break_Platform";

    protected override string NormalPlatformKey => _normalPlatformKey;
    protected override string BreakPlatformKey  => _breakPlatformKey;

    public override void GenerateMap()
    {
        Debug.Log("🍳 Generating Kitchen Map...");

        InitializeGenerators(); 
        RegisterKitchenAssets();

        SetupBackground();
        SetupFloor();

        // Enemy Spawner DI
        _enemySpawner?.InitializeSpawner(
            _objectPoolManager,
            MapType.Kitchen,
            FindAnyObjectByType<Player>(),
            _collectibleSpawner,
            FindAnyObjectByType<CardManager>()
        );
        SpawnEnemies();

        // Collectible DI
        _collectibleSpawner?.InitializeSpawner(
            _objectPoolManager,
            FindAnyObjectByType<DistanceCulling>(),
            FindAnyObjectByType<CardManager>(),
            FindAnyObjectByType<BuffManager>()
        );
        SpawnCollectibles();

        // Platform endless spawn loop
        InitializePlatformGeneration();

        WallPushSpeed = _baseWallPushSpeed;
        Debug.Log("[KitchenMap] Initial WallPushSpeed set.");
    }

    public override void SpawnEnemies()
    {
        if (_enemySpawner == null) return;
        StartCoroutine(_enemySpawner.StartWave());
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
            yield return new WaitForSeconds(Random.Range(3f, 7f));
        }
    }

    public override void SetupBackground()
    {
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

    public override void SetupFloor()
    {
        if (_objectPoolManager == null) return;

        // Floor
        _objectPoolManager.SpawnFromPool(
            _kitchenFloorKey,
            new Vector3(_spawnStartPosition.x, _spawnStartPosition.y - 2, 0),
            Quaternion.identity
        );

        // Wall behind
        GameObject wall = _objectPoolManager.SpawnFromPool(
            _wallVisualKey,
            new Vector3(_spawnStartPosition.x - 10, _spawnStartPosition.y + 2, 0),
            Quaternion.identity
        );

        _endlessWall = wall.transform;
    }

    public void RegisterKitchenAssets()
    {
        if (_objectPoolManager == null) return;

        List<string> assetKeys = new()
        {
            _normalPlatformKey,
            _breakPlatformKey,
            _kitchenFloorKey,
            _wallVisualKey,
            _throwableAssetKey
        };
    }

    public override void ClearAllObjects()
    {
        base.ClearAllObjects();
        Debug.Log("Clearing all kitchen map objects...");
    }

    private void Update()
    {
        if (IsWallPushEnabled)
            WallUpdate();
    }
}
