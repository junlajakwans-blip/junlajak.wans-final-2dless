using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class MapGeneratorRoad : MapGeneratorBase
{
    [Header("Road Map Settings")]
    [SerializeField] private string _backgroundKey = "map_bg_RoadTraffic";
    [SerializeField] private string _roadFloorKey = "map_asset_RoadTraffic_Floor";
    [SerializeField] private string _wallVisualKey = "map_Wall_RoadTraffic";
    
    // Throw item
    [SerializeField] private string _throwableAssetKey = "map_ThrowItem_RoadTraffic";

    [Header("Platform Assets")]
    [SerializeField] private string _normalPlatformKey = "map_asset_RoadTraffic_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_RoadTraffic_Break_Platform";

    protected override string NormalPlatformKey => _normalPlatformKey;
    protected override string BreakPlatformKey => _breakPlatformKey;

    public override void GenerateMap()
    {
        Debug.Log("🚧 Generating RoadTraffic Map...");

        InitializeGenerators();
        RegisterRoadAssets();

        SetupBackground();
        SetupFloor();

        _enemySpawner?.InitializeSpawner(
            _objectPoolManager,
            MapType.RoadTraffic,
            FindAnyObjectByType<Player>(),
            _collectibleSpawner,
            FindAnyObjectByType<CardManager>()
        );
        SpawnEnemies();

        _collectibleSpawner?.InitializeSpawner(
            _objectPoolManager,
            FindAnyObjectByType<DistanceCulling>(),
            FindAnyObjectByType<CardManager>(),
            FindAnyObjectByType<BuffManager>()
        );
        SpawnCollectibles();

        InitializePlatformGeneration();
        WallPushSpeed = _baseWallPushSpeed;
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

        _objectPoolManager.SpawnFromPool(
            _roadFloorKey,
            new Vector3(_spawnStartPosition.x, _spawnStartPosition.y - 2, 0),
            Quaternion.identity
        );

        GameObject wall = _objectPoolManager.SpawnFromPool(
            _wallVisualKey,
            new Vector3(_spawnStartPosition.x - 10, _spawnStartPosition.y + 2, 0),
            Quaternion.identity
        );

        _endlessWall = wall.transform;
    }

    public void RegisterRoadAssets()
    {
        if (_objectPoolManager == null) return;

        List<string> assetKeys = new()
        {
            _normalPlatformKey,
            _breakPlatformKey,
            _roadFloorKey,
            _wallVisualKey,
            _throwableAssetKey
        };
    }

    private void Update()
    {
        if (IsWallPushEnabled)
            WallUpdate();
    }
}
