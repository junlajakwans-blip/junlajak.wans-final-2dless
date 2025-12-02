using System.Collections;
using UnityEngine;

public class MapGeneratorRoad : MapGeneratorBase
{
    [Header("Road Keys")]
    [SerializeField] private string _floorKey = "map_asset_RoadTraffic_Floor";
    [SerializeField] private string _platformKey = "map_asset_RoadTraffic_Normal_Platform";
    [SerializeField] private string _breakPlatformKey = "map_asset_RoadTraffic_Break_Platform";
    [SerializeField] private string _backgroundKey = "map_bg_RoadTraffic";


    protected override string NormalPlatformKey => _platformKey;
    protected override string BreakPlatformKey  => _breakPlatformKey;
    protected override string FloorKey         => _floorKey;

    public override void GenerateMap()
    {
        // 1) เตรียม Pool + Pivot
        InitializeGenerators();

        // 2) ฉากพื้นหลัง
        SetupBackground();
        SetupFloor(); // floor เริ่มต้น

        // 3) เปิดระบบ Endless Platform + Floor
        InitializePlatformGeneration();

        // 4) Spawners
        var player      = FindAnyObjectByType<Player>();
        var cardManager = FindAnyObjectByType<CardManager>();
        var buffManager = FindAnyObjectByType<BuffManager>();
        var culling     = FindAnyObjectByType<DistanceCulling>();

        _enemySpawner?.InitializeSpawner(
            _objectPoolManager,
            MapType.RoadTraffic,
            player,
            _collectibleSpawner,
            cardManager,
            FindFirstObjectByType<BuffManager>()
        );

        _collectibleSpawner?.InitializeSpawner(
            _objectPoolManager,
            culling,
            cardManager,
            buffManager
        );

        // 🆕 Asset & Throwable
        _assetSpawner?.Initialize(_generationPivot);
        _throwableSpawner?.Initialize(_generationPivot, _enemySpawner);

        // 5) เริ่มระบบ Wave (Enemy) และ Collectible Loop เดิม
        SpawnEnemies();
        SpawnCollectibles();

        // 6) Wall ไล่
        WallPushSpeed = _baseWallPushSpeed;
    }

    public override void SetupBackground()
    {
        _backgroundLooper?.SetBackground(_backgroundKey);
    }

    public override void SpawnEnemies()
    {
        if (_enemySpawner == null) return;
        _enemySpawner.StartWaveRepeating();
    }

    public override void SpawnCollectibles()
    {
    // FIX: ลบ StartCoroutine(SpawnCollectiblesLoop()) ออก
    // Logic การ Spawn ถูกย้ายไปควบคุมโดย InvokeRepeating ใน CollectibleSpawner.cs
    }


    public override void SpawnAssets() { }
    public override void SpawnThrowables() { }
}

