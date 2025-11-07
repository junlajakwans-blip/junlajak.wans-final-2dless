using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class PerformanceManager : MonoBehaviour
{
    #region Singleton
    public static PerformanceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Fields
    [Header("Performance Metrics")]
    [SerializeField] private float _fpsUpdateInterval = 0.5f;
    [SerializeField] private float _currentFps;
    [SerializeField] private float _memoryUsageMB;

    [Header("References")]
    [SerializeField] private ObjectPoolManager _objectPoolManager;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private CollectibleSpawner _collectibleSpawner;

    [Header("Debug Info")]
    [SerializeField] private bool _showDebugLog = true;

    private float _accumulatedTime = 0f;
    private int _frameCount = 0;
    private float _timeLeft;
    #endregion

    #region Unity Methods
    private void Start()
    {
        _timeLeft = _fpsUpdateInterval;
        StartCoroutine(UpdatePerformanceData());
    }
    #endregion

    #region Coroutines

    private IEnumerator UpdatePerformanceData()
    {
        while (true)
        {
            yield return new WaitForSeconds(_fpsUpdateInterval);
            UpdateFPS();
            UpdateMemoryUsage();
            LogPerformanceData();
        }
    }
    #endregion

    #region Performance Calculations
    private void UpdateFPS()
    {
        _currentFps = 1.0f / Time.deltaTime;
    }

    private void UpdateMemoryUsage()
    {
        _memoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    }
    #endregion

    #region Public Methods

    public int GetTotalPooledObjects()
    {
        if (_objectPoolManager == null) return 0;
        return _objectPoolManager != null
            ? _objectPoolManager.transform.childCount
            : 0;
    }

    public int GetActiveEnemyCount()
    {
        return _enemySpawner != null ? _enemySpawner.GetSpawnCount() : 0;
    }

    public int GetActiveCollectibleCount()
    {
        return _collectibleSpawner != null ? _collectibleSpawner.GetSpawnCount() : 0;
    }

    public void LogPerformanceData()
    {
        if (!_showDebugLog) return;

        Debug.Log(
            $"📊 [Performance]\n" +
            $"- FPS: {_currentFps:F1}\n" +
            $"- Memory: {_memoryUsageMB:F2} MB\n" +
            $"- Pooled Objects: {GetTotalPooledObjects()}\n" +
            $"- Enemies: {GetActiveEnemyCount()}\n" +
            $"- Collectibles: {GetActiveCollectibleCount()}"
        );
    }

    public void ToggleDebugLog(bool enable)
    {
        _showDebugLog = enable;
    }
    #endregion
}
