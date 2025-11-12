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

    // Removed unused fields that caused CS0414 warnings
    // private float _accumulatedTime = 0f;
    // private int _frameCount = 0;
    // private float _timeLeft;
    
    // NEW: Fields needed for averaged FPS calculation
    private float _timeSinceLastUpdate = 0f;
    private int _framesSinceLastUpdate = 0;
    #endregion

    #region Unity Methods
    private void Start()
    {
        // Start Coroutine immediately
        StartCoroutine(UpdatePerformanceData());
    }
    
    // Use Update to track frames for averaged FPS
    private void Update()
    {
        _timeSinceLastUpdate += Time.unscaledDeltaTime;
        _framesSinceLastUpdate++;
    }
    #endregion

    #region Coroutines

    private IEnumerator UpdatePerformanceData()
    {
        while (true)
        {
            // Yield *first* to ensure the Update() method tracks at least one frame
            yield return new WaitForSeconds(_fpsUpdateInterval);
            
            UpdateFPS();
            UpdateMemoryUsage();
            LogPerformanceData();
            
            // Reset counters after logging
            _timeSinceLastUpdate = 0f;
            _framesSinceLastUpdate = 0;
        }
    }
    #endregion

    #region Performance Calculations
    /// <summary>
    /// Calculates FPS based on the time and frames accumulated since the last log.
    /// </summary>
    private void UpdateFPS()
    {
        if (_timeSinceLastUpdate > 0 && _framesSinceLastUpdate > 0)
        {
            _currentFps = _framesSinceLastUpdate / _timeSinceLastUpdate;
        }
        else
        {
            _currentFps = 0;
        }
    }

    private void UpdateMemoryUsage()
    {
        // Unity Profiler API requires the Profiler namespace
        #if ENABLE_PROFILER
            _memoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        #else
            _memoryUsageMB = 0;
        #endif
    }
    #endregion

    #region Public Methods

    public int GetTotalPooledObjects()
    {
        if (_objectPoolManager == null) return 0;
        
        // The childCount logic was incorrect. Assuming ObjectPoolManager
        // will expose the total pooled count via a property/method later.
        // For now, return the child count of the pool container (a reasonable proxy).
        return _objectPoolManager.transform.childCount;
    }

    public int GetActiveEnemyCount()
    {
        // Uses GetSpawnCount() which we confirmed exists in EnemySpawner.cs
        return _enemySpawner != null ? _enemySpawner.GetSpawnCount() : 0;
    }

    public int GetActiveCollectibleCount()
    {
        // Uses GetSpawnCount() which we confirmed exists in CollectibleSpawner.cs
        return _collectibleSpawner != null ? _collectibleSpawner.GetSpawnCount() : 0;
    }

    public void LogPerformanceData()
    {
        if (!_showDebugLog) return;

        Debug.Log(
            $"[Performance]\n" +
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