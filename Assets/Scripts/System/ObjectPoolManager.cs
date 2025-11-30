using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour, IObjectPool
{
    #region Protected Fields
    [Header("Pool Settings")]
    protected Dictionary<string, Queue<GameObject>> _poolDictionary = new();
    [SerializeField] protected List<GameObject> _prefabs = new();
    [SerializeField] protected Transform _parentContainer;
    #endregion

    public static ObjectPoolManager Instance { get; private set; }
    public bool IsInitialized { get; private set; } = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ทำให้คงอยู่ตลอดการเปลี่ยนฉาก
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // ป้องกันการมีหลาย Instance
            Destroy(gameObject);
        }
    }

    #region IObjectPool Implementation

    public virtual void InitializePool()
    {
        if (IsInitialized) return;  // กันเรียกซ้ำ

        foreach (var prefab in _prefabs)
        {
            if (!_poolDictionary.ContainsKey(prefab.name))
                _poolDictionary[prefab.name] = new Queue<GameObject>();

            var obj = Instantiate(prefab, _parentContainer);
            obj.SetActive(false);
            _poolDictionary[prefab.name].Enqueue(obj);
        }

        IsInitialized = true;
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
        GameObject obj = null;

        // ดึงของจากคิว ถ้า null ให้วนไปหยิบใหม่
        while (queue.Count > 0 && obj == null)
        {
            obj = queue.Dequeue();
        }

        // ถ้าของในคิวถูก Destroy หมด → สร้างใหม่
        if (obj == null)
        {
            obj = CreateNewInstance(objectTag);
            if (obj == null)
            {
                Debug.LogError($"[Pool] Failed to create instance for '{objectTag}'");
                return null;
            }
        }

        // ตอนนี้ obj ปลอดภัยแล้ว
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    public virtual void ReturnToPool(string objectTag, GameObject obj)
    {
        // 1) Clean the tag BEFORE checking dictionary
        string cleanedTag = objectTag.Replace("(Clone)", "").Trim();

        // If it starts with "throw_", assume ThrowableSpawner handles its return/pooling.
        if (cleanedTag.StartsWith("throw_")) 
        {
            // Do NOT process it in the main pool manager.
            // The ThrowableSpawner script (or the object itself) must handle its own Despawn/Return.
            // For now, we destroy it to prevent it from hanging if the Despawn/Return 
            // process in ThrowableSpawner was missed.

            // Note: If ThrowableSpawner.Despawn() is the only intended method for returning throwables,
            // this `ReturnToPool` call should ideally not happen for throwables.
            // But if it does happen, destroying it here prevents the error and stops leaks.
            Debug.Log($"[Pool Manager] Skipping return for Throwable: {cleanedTag}. (Should be handled by ThrowableSpawner)");
            Destroy(obj);
            return;
        }

        // 2) Safe guard for non-throwable objects
        if (!_poolDictionary.ContainsKey(cleanedTag))
        {
            Debug.LogWarning($"❌ [POOL ERROR] Missing pool for: {cleanedTag} (Destroying instance).");
            Destroy(obj);
            return;
        }

        // 3) Reset & return for valid, non-throwable objects
        obj.SetActive(false);
        _poolDictionary[cleanedTag].Enqueue(obj);
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
        public List<string> GetAllTags()
    {
        return new List<string>(_poolDictionary.Keys);
    }
}
