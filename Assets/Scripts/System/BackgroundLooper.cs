using System.Collections.Generic;
using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    #region Fields
    [Header("Background Settings")]
    [SerializeField] private List<GameObject> _backgroundLayers = new(); 
    [SerializeField] private float _resetPositionX = -20f;   
    [SerializeField] private float _startPositionX = 20f;     
    [SerializeField] private bool _isLooping = true;

    [Header("Background Type")]
    [SerializeField] private string _currentBackgroundKey = "default";
    

    [SerializeField] private Dictionary<string, float> _scrollSpeeds = new Dictionary<string, float>()
    {
        {"default", 2.0f}, // Base/Default speed
        {"map_bg_RoadTraffic", 3.5f}, // Example: Faster traffic background (matching RoadTraffic Mon logic)
        {"map_bg_Kitchen", 1.8f},     // Example: Slower kitchen background
        {"map_bg_School", 2.5f}      // Example: Standard school speed
    };
    #endregion

    #region Unity Methods
    private void Update()
    {
        if (!_isLooping) return;
        ScrollBackground();
    }
    #endregion

    #region Public Methods

    public void SetBackground(string backgroundKey)
    {
        _currentBackgroundKey = backgroundKey;
        Debug.Log($"[BackgroundLooper] Set background to '{backgroundKey}'.");
    }

    public void ToggleLoop(bool enable)
    {
        _isLooping = enable;
    }

    public void ResetBackground()
    {
        foreach (var layer in _backgroundLayers)
        {
            if (layer != null)
            {
                Vector3 pos = layer.transform.position;
                pos.x = _startPositionX;
                layer.transform.position = pos;
            }
        }
    }
    #endregion

    #region Private Methods
    private void ScrollBackground()
    {
        
        float _scrollSpeed = 2f; // Default scroll speed
        
        // Try to get speed based on the current key, otherwise use the default (2f)
        if (_scrollSpeeds.TryGetValue(_currentBackgroundKey, out float speed))
        {
             _scrollSpeed = speed;
        }
        
        foreach (var layer in _backgroundLayers)
        {
            if (layer == null) continue;

            layer.transform.Translate(Vector3.left * _scrollSpeed * Time.deltaTime);

            if (layer.transform.position.x <= _resetPositionX)
            {
                Vector3 newPos = layer.transform.position;
                newPos.x = _startPositionX;
                layer.transform.position = newPos;
            }
        }
    }
    #endregion
}