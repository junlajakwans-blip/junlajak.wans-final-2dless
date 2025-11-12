using System; // Required for Action
using System.Collections.Generic;
using UnityEngine;

public class StoreMap : StoreBase
{
    #region Fields
    private List<string> _unlockedMaps = new List<string>();
    
    // Alias for Keys Required from Currency.cs
    private const int KEY_COST = Currency.KEYS_PER_MAP; 

    // NEW/FIXED: Event to notify external managers (GameManager/SceneManager)
    public Action<string> OnMapUnlockedEvent; 
    #endregion

    #region Initialization
    public override void Initialize(StoreManager storeManager)
    {
        base.Initialize(storeManager);
        _storeName = "Map Unlock Portal";

        // Add maps available for purchase 
        AddMapToList(MapType.RoadTraffic.ToFriendlyString(), KEY_COST);
        AddMapToList(MapType.Kitchen.ToFriendlyString(), KEY_COST);

        // NOTE: School (Default) is not listed for purchase.
    }
    #endregion

    #region Override Methods
    public override void DisplayItems()
    {
        Debug.Log($" Store: {_storeName} (Keys: {_storeManager.Currency.KeyMap})");
        
        foreach (var map in _storeItems)
        {
            // Display cost in Keys
            Debug.Log($" - {map.Key}: {KEY_COST} Keys (Current Status: {(IsMapUnlocked(map.Key) ? "UNLOCKED" : "LOCKED")})");
        }
    }

    /// <summary>
    /// Purchases the map unlock service using the required number of Keys.
    /// </summary>
    public override bool Purchase(string itemName)
    {
        if (!IsMapPurchasable(itemName)) return false;

        if (IsMapUnlocked(itemName))
        {
            Debug.Log($"ℹ Map '{itemName}' already unlocked.");
            return false;
        }

        // 3. Attempt to unlock the map using the Currency system (which consumes 3 Keys).
        if (_storeManager.Currency.UnlockMap())
        {
            UnlockMap(itemName); // Calls the core logic below
            Debug.Log($" Map unlocked: {itemName}. Keys remaining: {_storeManager.Currency.KeyMap}");
            return true;
        }

        // Log failure due to insufficient keys (handled inside Currency.UnlockMap)
        Debug.Log($" Not enough keys to unlock map! Required: {KEY_COST} Keys.");
        return false;
    }

    /// <summary>
    /// Returns the Key cost, not the Coin cost.
    /// </summary>
    public override int GetItemPrice(string itemName)
    {
        // For maps, the 'price' returned is the KEY cost.
        if (_storeItems.ContainsKey(itemName))
            return KEY_COST; 
        
        Debug.LogWarning($" Map '{itemName}' not found in store catalog.");
        return 0;
    }
    #endregion

    #region Map Logic (Core Unlocking)
    private void AddMapToList(string mapName, int price)
    {
        if (!_storeItems.ContainsKey(mapName))
        {
            _storeItems.Add(mapName, price);
        }
    }

    private bool IsMapPurchasable(string mapName)
    {
        return _storeItems.ContainsKey(mapName);
    }

    private bool IsMapUnlocked(string mapName)
    {
        return _unlockedMaps.Contains(mapName);
    }
    
    public void UnlockMap(string mapName)
    {
        if (!IsMapUnlocked(mapName))
        {
            _unlockedMaps.Add(mapName);
            _storeManager.UnlockItem(mapName);

            // 🎯 Event Trigger
            OnMapUnlockedEvent?.Invoke(mapName);

            Debug.Log($" Map added to unlocked list: {mapName}");
        }
    }
    #endregion
}