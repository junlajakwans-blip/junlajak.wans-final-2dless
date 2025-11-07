using System.Collections.Generic;
using UnityEngine;

public abstract class MapGeneratorBase : MonoBehaviour
{
    #region Protected Fields
    [Header("Spawner References")]
    [SerializeField] protected PlatformSpawner _platformSpawner;
    [SerializeField] protected EnemySpawner _enemySpawner;
    [SerializeField] protected CollectibleSpawner _collectibleSpawner;
    [SerializeField] protected BackgroundLooper _backgroundLooper;

    [Header("Generation Settings")]
    [SerializeField] protected Vector2 _spawnStartPosition;
    [SerializeField] protected int _maxPlatformCount = 10;
    [SerializeField] protected float _sceneWidth = 50f;
    #endregion

    #region Initialization

    public virtual void InitializeGenerators()
    {
        Debug.Log($"{GetType().Name}: Generators initialized.");
    }
    #endregion

    #region Abstract Method
    public abstract void GenerateMap();
    #endregion

    #region Virtual Methods
    public virtual void SpawnPlatforms()
    {
        Debug.Log($"{GetType().Name}: Spawning platforms...");
    }

    public virtual void SpawnEnemies()
    {
        Debug.Log($"{GetType().Name}: Spawning enemies...");
    }

    public virtual void SpawnCollectibles()
    {
        Debug.Log($"{GetType().Name}: Spawning collectibles...");
    }

    public virtual void SetupBackground()
    {
        Debug.Log($"{GetType().Name}: Setting up background...");
    }

    public virtual void ClearAllObjects()
    {
        Debug.Log($"{GetType().Name}: Clearing all objects...");
    }
    #endregion
}
