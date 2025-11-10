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
        //TODO: Adjust scroll speed based on background type
        
        float _scrollSpeed = 2f; // Default scroll speed
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
