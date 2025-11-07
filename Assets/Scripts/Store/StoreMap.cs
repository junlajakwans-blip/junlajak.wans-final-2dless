using System.Collections.Generic;
using UnityEngine;

public class StoreMap : StoreBase
{
    #region Fields
    private List<string> _availableMaps = new List<string>();
    #endregion

    #region Override Methods
    public override void DisplayItems()
    {
        Debug.Log($" Store: {_storeName}");
        foreach (var map in _storeItems)
        {
            Debug.Log($" - {map.Key}: {map.Value} coins");
        }
    }

    public override bool Purchase(string itemName)
    {
        int price = GetItemPrice(itemName);
        if (price <= 0)
        {
            Debug.LogWarning($" Invalid price or item not found: {itemName}");
            return false;
        }

        if (_storeManager.Currency.UseCoin(price))
        {
            UnlockMap(itemName);
            Debug.Log($" Map unlocked: {itemName}");
            return true;
        }

        Debug.Log($" Not enough coins to unlock map: {itemName}");
        return false;
    }

    public void UnlockMap(string mapName)
    {
        if (!_availableMaps.Contains(mapName))
        {
            _availableMaps.Add(mapName);
            _storeManager.UnlockItem(mapName);
            Debug.Log($" Map added to unlocked list: {mapName}");
        }
        else
        {
            Debug.Log($"ℹ Map '{mapName}' already unlocked.");
        }
    }
    #endregion
}
