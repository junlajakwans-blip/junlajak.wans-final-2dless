using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StoreManager
{
    [SerializeField] private Currency _currency;
    [SerializeField] private GameProgressData _progressData;

    // ดึงจาก Inspector (หรือ Resources.LoadAll ก็ได้)
    [SerializeField] private List<StoreItem> _allItems = new List<StoreItem>();

    public List<StoreBase> Stores { get; private set; } = new List<StoreBase>();

    public Currency Currency => _currency;
    public GameProgressData ProgressData => _progressData;

    public StoreManager(Currency currency, GameProgressData progressData)
    {
        _currency = currency;
        _progressData = progressData;

        // โหลด StoreItem ทั้งหมดจาก Resources
        _allItems = new List<StoreItem>();
        _allItems.AddRange(Resources.LoadAll<StoreItem>("StoreItems/Exchange"));
        _allItems.AddRange(Resources.LoadAll<StoreItem>("StoreItems/Upgrade"));
        _allItems.AddRange(Resources.LoadAll<StoreItem>("StoreItems/Map"));

        Debug.Log($"[StoreManager] Loaded total items = {_allItems.Count}");
    }


    /// <summary>
    /// Register shop facade (StoreExchange / StoreUpgrade / StoreMap)
    /// and inject its item list from _allItems automatically.
    /// </summary>
    public void RegisterStore(StoreBase store)
    {
        if (!Stores.Contains(store))
            Stores.Add(store);

        List<StoreItem> itemsForThisStore = GetItemsForStore(store.StoreType);
        store.Initialize(this, itemsForThisStore);

        Debug.Log($"[StoreManager] Registered {store.StoreName} | {itemsForThisStore.Count} items");
    }

    /// <summary>
    /// Filters StoreItem list by StoreType (Exchange / Upgrade / Map ...)
    /// </summary>
    private List<StoreItem> GetItemsForStore(StoreType type)
    {
        return _allItems.FindAll(i => i != null && i.StoreType == type);
    }

    /// <summary>
    /// Purchase item universally by ID (Used by UI, DevCheat, Quest, Rewards)
    /// </summary>
    public bool Purchase(string itemID)
    {
        foreach (var store in Stores)
        {
            StoreItem item = store.GetItem(itemID);
            if (item == null)
                continue;

            bool success = store.Purchase(item);
            Debug.Log(success
                ? $"[StoreManager] Purchase SUCCESS → {item.DisplayName}"
                : $"[StoreManager] Purchase FAILED → {item.DisplayName}");

            return success;
        }

        Debug.LogError($"[StoreManager] Item not found in ANY store → {itemID}");
        return false;
    }
}
