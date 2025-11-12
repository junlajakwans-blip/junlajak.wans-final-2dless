using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class MapGeneratorBase : MonoBehaviour
{
    #region Protected Fields
    [Header("Spawner References")]
    [SerializeField] protected EnemySpawner _enemySpawner;
    [SerializeField] protected CollectibleSpawner _collectibleSpawner;
    [SerializeField] protected BackgroundLooper _backgroundLooper;

    [Header("Generation Settings")]
    [SerializeField] protected Vector2 _spawnStartPosition;
    [SerializeField] protected int _maxPlatformCount = 10;
    [SerializeField] protected float _sceneWidth = 50f;

    [Header("Wall Control")]
    [SerializeField] protected Transform _endlessWall; // Reference to the actual Wall GameObject
    [SerializeField] protected float _baseWallPushSpeed = 1.0f; // Default push speed

    [Header("Asset Catalogs")]
    [SerializeField] protected Dictionary<string, GameObject> _assetCatalog = new();
    
    [Header("Platform Management")]
    [SerializeField] protected List<GameObject> _activePlatforms = new();
    #endregion

    #region Wall and Scene Control (NEW)

    /// <summary>
    /// Gets or sets the current pushing speed of the endless wall.
    /// Used by the WallUpdate loop and modified by career BuffMap effects.
    /// </summary>
    public float WallPushSpeed { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether platforms can be broken (false if FireFighter BuffMap is active).
    /// </summary>
    public bool IsPlatformBreakable { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the wall is currently pushing.
    /// Used by ChefDuck/ProgrammerDuck BuffMap logic to stop/slow the wall.
    /// </summary>
    public bool IsWallPushEnabled { get; set; }

    /// <summary>
    /// Handles the continuous movement of the Endless Wall by applying lateral translation.
    /// This method should be called from the Manager's Update loop.
    /// </summary>
    public virtual void WallUpdate()
    {
        float currentSpeed = WallPushSpeed;

        if (_endlessWall != null)
        {
            _endlessWall.Translate(Vector3.left * currentSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Breaks the furthest platform to the right (Used by FireFighterDuck Skill).
    /// </summary>
    public virtual void BreakRightmostPlatform()
    {
        
        if (!IsPlatformBreakable || _activePlatforms.Count == 0)
        {
            Debug.Log($"[{GetType().Name}] Cannot break platform: Breakable={IsPlatformBreakable}");
            return;
        }

        // Find the platform with the maximum X position (rightmost)
        var rightmostPlatform = _activePlatforms
            .OrderByDescending(p => p.transform.position.x)
            .FirstOrDefault();

        if (rightmostPlatform != null)
        {
            _activePlatforms.Remove(rightmostPlatform);
            Destroy(rightmostPlatform); // Assuming platforms are simple GameObjects that can be destroyed
            Debug.Log($"[{GetType().Name}] Rightmost platform broken successfully.");
        }
    }
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes base generator settings, sets default wall speeds, and applies initial buff states.
    /// </summary>
    public virtual void InitializeGenerators()
    {
        // Set initial state for the wall and buffs
        WallPushSpeed = _baseWallPushSpeed;
        IsPlatformBreakable = true;
        IsWallPushEnabled = true;
        
        Debug.Log($"{GetType().Name}: Generators initialized.");
    }
    #endregion

    #region Abstract Method
    /// <summary>
    /// Abstract method that must be implemented by derived classes to define the map generation sequence.
    /// </summary>
    public abstract void GenerateMap();
    #endregion

    #region Virtual Methods
    /// <summary>
    /// Initiates the spawning process for platforms specific to the derived map type.
    /// </summary>
    public virtual void SpawnPlatforms()
    {
        Debug.Log($"{GetType().Name}: Spawning platforms...");
    }

    /// <summary>
    /// Initiates the spawning process for enemies specific to the derived map type.
    /// </summary>
    public virtual void SpawnEnemies()
    {
        Debug.Log($"{GetType().Name}: Spawning enemies...");
    }

    /// <summary>
    /// Initiates the spawning process for collectibles specific to the derived map type.
    /// </summary>
    public virtual void SpawnCollectibles()
    {
        Debug.Log($"{GetType().Name}: Spawning collectibles...");
    }

    /// <summary>
    /// Sets up the background visuals, often by communicating with the BackgroundLooper.
    /// </summary>
    public virtual void SetupBackground()
    {
        Debug.Log($"{GetType().Name}: Setting up background...");
    }

    /// <summary>
    /// Clears and resets all generated objects in the current map instance.
    /// </summary>
    public virtual void ClearAllObjects()
    {
        // Clear all active platforms
        foreach (var platform in _activePlatforms)
        {
            if (platform != null)
                Destroy(platform);
        }
        _activePlatforms.Clear();

        Debug.Log($"{GetType().Name}: Clearing all objects...");
    }
    #endregion
}