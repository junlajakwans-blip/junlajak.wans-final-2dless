using System.Collections.Generic;
using UnityEngine;

public abstract class StoreBase
{
    #region Protected Fields
    protected string _storeName;
    protected Dictionary<string, int> _storeItems = new Dictionary<string, int>();
    protected StoreManager _storeManager;

    #endregion

    #region Properties
    public string StoreName => _storeName;
    public Dictionary<string, int> StoreItems => _storeItems;
    #endregion

    #region Virtual / Abstract Methods
    public virtual void Initialize(StoreManager storeManager)
    {
        _storeManager = storeManager;
        Debug.Log($" {_storeName} initialized with {storeManager.AvailableItems.Count} available items.");
    }

    public abstract void DisplayItems();
    public abstract bool Purchase(string itemName);

    public virtual int GetItemPrice(string itemName)
    {
        if (_storeItems.ContainsKey(itemName))
            return _storeItems[itemName];

        Debug.LogWarning($" Item '{itemName}' not found in {_storeName} store.");
        return -1;
    }
    #endregion
}
