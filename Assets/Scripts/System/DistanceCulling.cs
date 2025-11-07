using System.Collections.Generic;
using UnityEngine;

public class DistanceCulling : MonoBehaviour
{
    #region Fields
    [Header("Culling Settings")]
    [SerializeField] private Transform _cameraTransform;    
    [SerializeField] private float _cullingDistance = 30f;   
    [SerializeField] private bool _enableCulling = true;

    [Header("Runtime")]
    [SerializeField] private List<GameObject> _targetObjects = new();
    private float _sqrCullingDistance;
    #endregion

    #region Unity Methods
    private void Start()
    {
        if (_cameraTransform == null)
            _cameraTransform = Camera.main?.transform;

        _sqrCullingDistance = _cullingDistance * _cullingDistance;
    }

    private void Update()
    {
        if (!_enableCulling || _cameraTransform == null) return;
        CullObjects();
    }
    #endregion

    #region Public Methods

    public void ToggleCulling(bool enable)
    {
        _enableCulling = enable;
        if (!enable)
        {
            foreach (var obj in _targetObjects)
                if (obj != null) obj.SetActive(true);
        }
    }

    public void RegisterObject(GameObject obj)
    {
        if (obj != null && !_targetObjects.Contains(obj))
            _targetObjects.Add(obj);
    }

    public void UnregisterObject(GameObject obj)
    {
        if (obj != null && _targetObjects.Contains(obj))
            _targetObjects.Remove(obj);
    }

    public void ClearAllTargets()
    {
        _targetObjects.Clear();
    }
    #endregion

    #region Private Methods

    private void CullObjects()
    {
        Vector3 camPos = _cameraTransform.position;

        foreach (var obj in _targetObjects)
        {
            if (obj == null) continue;

            float sqrDist = (obj.transform.position - camPos).sqrMagnitude;
            bool shouldBeActive = sqrDist <= _sqrCullingDistance;

            if (obj.activeSelf != shouldBeActive)
                obj.SetActive(shouldBeActive);
        }
    }
    #endregion
}
