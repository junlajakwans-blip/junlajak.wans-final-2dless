using System.Collections.Generic;
using UnityEngine;
using DuffDuck.Stage;


public class BackgroundLooper : MonoBehaviour
{
    #region Serialized Fields

    [Header("Looping Settings")]
    [SerializeField] private float _resetPositionX = -20f;
    [SerializeField] private float _startPositionX = 20f;
    [SerializeField] private bool _isLooping = true;

    [Header("Background Type Key")]
    [SerializeField] private string _currentBackgroundKey = "default";

    [Header("Scroll Speeds")]
    [SerializeField]
    private Dictionary<string, float> _scrollSpeeds = new Dictionary<string, float>()
    {
        {"default", 2.0f},
        {"map_bg_RoadTraffic", 3.5f},
        {"map_bg_Kitchen", 1.8f},
        {"map_bg_School", 2.5f}
    };

    #endregion

    private readonly List<GameObject> _backgroundLayers = new();
    private float _speedCache;
    private Vector3 _moveCache;
    private float _difficultyScale = 1f;
    private float _timer = 0f;


    private float _wallSpeedScale = 1f;

    private void OnEnable()
    {
        WallPushController.OnWallSpeedChanged += HandleWallSpeedChanged;
    }

    private void OnDisable()
    {
        WallPushController.OnWallSpeedChanged -= HandleWallSpeedChanged;
    }

    private void HandleWallSpeedChanged(float wallSpeed)
    {
        // normalize ให้ 1 = base speed
        _wallSpeedScale = 1f + (wallSpeed * 0.05f);       
        // 0.05 คือ sensitivity จะเพิ่ม/ลดได้
    }


    private void Update()
    {
        _timer += Time.deltaTime;
        _difficultyScale = Mathf.Min(1f + (_timer / 300f), 3f); //  5 นาทีจะ ×2 speed | ค่าสูงสุด 3x

        if (_isLooping) ScrollWithoutGC();
    }

    public void SetBackground(string backgroundKey)
    {
        _currentBackgroundKey = backgroundKey;
        RegisterBackgroundFromPool(backgroundKey);
        ResetBackground();

        _speedCache = _scrollSpeeds.TryGetValue(backgroundKey, out float val) ? val : _scrollSpeeds["default"];
    }

    private void ScrollWithoutGC()
    {
        // ใช้ for-loop ป้องกัน GC
        for (int i = 0; i < _backgroundLayers.Count; i++)
        {
            var layer = _backgroundLayers[i];
            if (layer == null) continue;

            _moveCache.x = -(_speedCache * _wallSpeedScale * _difficultyScale) * Time.deltaTime;
            _moveCache.y = 0;
            _moveCache.z = 0;
            layer.transform.Translate(_moveCache);

            // teleport
            var pos = layer.transform.position;
            if (pos.x <= _resetPositionX)
            {
                pos.x = _startPositionX;
                layer.transform.position = pos;
            }
        }
    }

    public void ResetBackground()
    {
        for (int i = 0; i < _backgroundLayers.Count; i++)
        {
            var pos = _backgroundLayers[i].transform.position;
            pos.x = _startPositionX;
            _backgroundLayers[i].transform.position = pos;
        }
    }

    private void RegisterBackgroundFromPool(string backgroundKey)
    {
        // clear
        for (int i = 0; i < _backgroundLayers.Count; i++)
            if (_backgroundLayers[i] != null)
                ObjectPoolManager.Instance.ReturnToPool(backgroundKey, _backgroundLayers[i]);
        _backgroundLayers.Clear();

        // spawn 2 layers
        for (int i = 0; i < 2; i++)
        {
            var bg = ObjectPoolManager.Instance.SpawnFromPool(
                backgroundKey,
                new Vector3(_startPositionX + i * (_startPositionX - _resetPositionX), 0, 0),
                Quaternion.identity
            );

            if (bg == null)
            {
                Debug.LogError($"[BackgroundLooper] BG prefab '{backgroundKey}' not found in Pool.");
                _isLooping = false;
                return;
            }

            bg.transform.SetParent(transform);
            _backgroundLayers.Add(bg);
        }
    }
}
