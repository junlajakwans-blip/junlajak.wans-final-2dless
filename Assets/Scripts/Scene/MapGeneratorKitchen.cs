using System.Collections;
using UnityEngine;

public class MapGeneratorKitchen : MapGeneratorBase
{
    [Header("Kitchen Map Keys")]
    [SerializeField] private string _backgroundKey = "map_bg_Kitchen";
    [SerializeField] private string _floorKey = "map_asset_Kitchen_Floor";
    [SerializeField] private string _platformKey = "map_asset_Kitchen_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_Kitchen_Break_Platform";
    [SerializeField] private string _wallVisualKey = "map_Wall_Kitchen";

    protected override string NormalPlatformKey => _platformKey;
    protected override string BreakPlatformKey  => _breakPlatformKey;
    protected override string FloorKey         => _floorKey;


    public override void GenerateMap()
    {
        Debug.Log("🍳 GENERATING MAP >> KITCHEN");

        // 1) เตรียม Pool + Pivot
        InitializeGenerators();

        // 2) Background
        SetupBackground();
        SetupFloor();

        // 3) Begin endless floor + platform
        InitializePlatformGeneration();

        // 4) Enemy init & spawn wave
        if (_enemySpawner != null)
        {
            _enemySpawner.InitializeSpawner(
                _objectPoolManager,
                MapType.Kitchen,
                FindFirstObjectByType<Player>(),
                _collectibleSpawner,
                FindFirstObjectByType<CardManager>(),
                FindFirstObjectByType<BuffManager>()
            );
            StartCoroutine(_enemySpawner.StartWave());
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

        // 6) Asset / Throwable
        _assetSpawner?.Initialize(_generationPivot);
        _throwableSpawner?.Initialize(_generationPivot, _enemySpawner);

        // 7) Wall Push
        WallPushSpeed = _baseWallPushSpeed;
    }


    public override void SetupBackground()
    {
        _backgroundLooper?.SetBackground(_backgroundKey);
    }


    public override void SetupFloor()
    {
        if (_objectPoolManager == null) return;

        // Wall visual placed behind pivot
        GameObject wallGO = _objectPoolManager.SpawnFromPool(
            _wallVisualKey,
            new Vector3(_spawnStartPosition.x - 10f, _spawnStartPosition.y + 2f, 0f),
            Quaternion.identity
        );

        if (wallGO != null)
            _endlessWall = wallGO.transform;

    
    }



    private void Update()
    {
        if (IsWallPushEnabled)
            WallUpdate();
    }
}
