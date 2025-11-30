using System.Collections;
using UnityEngine;

public class MapGeneratorRoad : MapGeneratorBase
{
    [Header("Road Map Keys")]
    [SerializeField] private string _backgroundKey = "map_bg_RoadTraffic";
    [SerializeField] private string _floorKey = "map_asset_RoadTraffic_Floor";
    [SerializeField] private string _platformKey = "map_asset_RoadTraffic_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_RoadTraffic_Break_Platform";
    [SerializeField] private string _wallVisualKey = "map_Wall_RoadTraffic";
    protected override string NormalPlatformKey => _platformKey;
    protected override string BreakPlatformKey  => _breakPlatformKey;
    protected override string FloorKey         => _floorKey;

    public override void GenerateMap()
    {
        Debug.Log("🚧 GENERATING MAP >> ROAD TRAFFIC");

        // 1) เตรียม Pool + Pivot
        InitializeGenerators();

        // 2) Background
        SetupBackground();
        SetupFloor();

        // 3) เปิดระบบ endless (Floor + Platform)
        InitializePlatformGeneration();

        // 4) Enemy
        if (_enemySpawner != null)
        {
            _enemySpawner.InitializeSpawner(
                _objectPoolManager,
                MapType.RoadTraffic,
                FindFirstObjectByType<Player>(),
                _collectibleSpawner,
                FindFirstObjectByType<CardManager>(),
                FindFirstObjectByType<BuffManager>()
            );
            _enemySpawner.StartWaveRepeating();
        }

        // 5) Collectibles
        if (_collectibleSpawner != null)
        {
            _collectibleSpawner.InitializeSpawner(
                _objectPoolManager,
                FindFirstObjectByType<DistanceCulling>(),
                FindFirstObjectByType<CardManager>(),
                FindFirstObjectByType<BuffManager>()
            );
        }

        // 6) Asset (auto spawn ตาม distance + difficulty phase)
        _assetSpawner?.Initialize(_generationPivot);

        // 7) Throwable (drop from enemy only)
        _throwableSpawner?.Initialize(_generationPivot, _enemySpawner);

        // 8) Visual Wall
        SpawnRoadWallVisual();

        WallPushSpeed = _baseWallPushSpeed;
    }

    public override void SetupBackground()
    {
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

    private void SpawnRoadWallVisual()
    {
        if (_objectPoolManager == null || string.IsNullOrEmpty(_wallVisualKey)) return;

        GameObject wallGO = _objectPoolManager.SpawnFromPool(
            _wallVisualKey,
            new Vector3(_spawnStartPosition.x - 10f, _spawnStartPosition.y + 2f, 0f),
            Quaternion.identity
        );

        if (wallGO != null)
            _endlessWall = wallGO.transform;
    }

}
