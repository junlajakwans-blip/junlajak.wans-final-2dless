using System.Collections.Generic;
using UnityEngine;

public abstract class StoreBase
{
    protected StoreManager _manager;
    public abstract string StoreName { get; }
    public abstract Dictionary<string, int> StoreItems { get; }
    public abstract bool Purchase(string itemName);
    public abstract void DisplayItems();
    public abstract StoreType StoreType { get; }

    public virtual void Initialize(StoreManager manager)
    {
        _manager = manager;
        manager.Stores.Add(this);
    }

}
