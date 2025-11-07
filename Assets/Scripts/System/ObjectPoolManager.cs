using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectPoolManager : MonoBehaviour, IObjectPool
{
    #region Protected Fields
    [Header("Pool Settings")]
    protected Dictionary<string, Queue<GameObject>> _poolDictionary = new();
    [SerializeField] protected List<GameObject> _prefabs = new();
    [SerializeField] protected Transform _parentContainer;
    #endregion

    #region IObjectPool Implementation

    public virtual void InitializePool()
    {
        foreach (var prefab in _prefabs)
        {
            if (!_poolDictionary.ContainsKey(prefab.name))
                _poolDictionary[prefab.name] = new Queue<GameObject>();

            var obj = Instantiate(prefab, _parentContainer);
            obj.SetActive(false);
            _poolDictionary[prefab.name].Enqueue(obj);
        }

        Debug.Log($"[Pool] Initialized {_poolDictionary.Count} object pools.");
    }

    public virtual GameObject SpawnFromPool(string objectTag, Vector3 position, Quaternion rotation)
    {
        if (!_poolDictionary.ContainsKey(objectTag))
        {
            Debug.LogWarning($"[Pool] Tag '{objectTag}' not found. Expanding pool...");
            ExpandPool(objectTag, 1);
        }

        var queue = _poolDictionary[objectTag];
        var obj = queue.Count > 0 ? queue.Dequeue() : CreateNewInstance(objectTag);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    public virtual void ReturnToPool(string objectTag, GameObject obj)
    {
        if (!_poolDictionary.ContainsKey(objectTag))
        {
            Debug.LogWarning($"[Pool] Trying to return object '{objectTag}' that has no pool.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        _poolDictionary[objectTag].Enqueue(obj);
    }

    public virtual void ClearPool()
    {
        foreach (var kvp in _poolDictionary)
        {
            foreach (var obj in kvp.Value)
                Destroy(obj);
        }

        _poolDictionary.Clear();
        Debug.Log("[Pool] Cleared all object pools.");
    }
    #endregion

    #region Protected Helpers

    public virtual void ExpandPool(string objectTag, int additionalCount)
    {
        var prefab = _prefabs.Find(p => p.name == objectTag);
        if (prefab == null)
        {
            Debug.LogError($"[Pool] Prefab with tag '{objectTag}' not found!");
            return;
        }

        if (!_poolDictionary.ContainsKey(objectTag))
            _poolDictionary[objectTag] = new Queue<GameObject>();

        for (int i = 0; i < additionalCount; i++)
        {
            var obj = Instantiate(prefab, _parentContainer);
            obj.SetActive(false);
            _poolDictionary[objectTag].Enqueue(obj);
        }

        Debug.Log($"[Pool] Expanded '{objectTag}' by {additionalCount} objects.");
    }

    protected GameObject CreateNewInstance(string objectTag)
    {
        var prefab = _prefabs.Find(p => p.name == objectTag);
        if (prefab == null)
        {
            Debug.LogError($"[Pool] Missing prefab for tag '{objectTag}'!");
            return null;
        }

        var obj = Instantiate(prefab, _parentContainer);
        obj.SetActive(false);
        return obj;
    }
    #endregion
}
