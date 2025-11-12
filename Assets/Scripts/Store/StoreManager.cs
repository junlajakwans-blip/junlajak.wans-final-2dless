using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StoreManager
{
    #region Fields
    [SerializeField] private Currency _currency;
    [SerializeField] private Dictionary<string, int> _availableItems = new Dictionary<string, int>();
    [SerializeField] private List<string> _unlockedItems = new List<string>();
    private CardManager _cardManager;
    #endregion

    #region Properties

    public Currency Currency => _currency;
    public CardManager CardManager => _cardManager;
    public Dictionary<string, int> AvailableItems => _availableItems;
    public List<string> UnlockedItems => _unlockedItems;
    #endregion

    public StoreManager(Currency currency)
    {
        _currency = currency;
    }

    #region Methods
    public bool CanAfford(string itemName)
    {
        if (!_availableItems.ContainsKey(itemName)) return false;
        int price = _availableItems[itemName];
        return _currency.Coin >= price;
    }

    public bool PurchaseItem(string itemName)
    {
        if (!CanAfford(itemName)) return false;
        int price = _availableItems[itemName];
        bool success = _currency.UseCoin(price);

        if (success)
        {
            UnlockItem(itemName);
            Debug.Log($" Purchased: {itemName} for {price} coins");
            return true;
        }

        Debug.LogWarning($" Cannot purchase item: {itemName}");
        return false;
    }

    public void UnlockItem(string itemName)
    {
        if (!_unlockedItems.Contains(itemName))
        {
            _unlockedItems.Add(itemName);
            Debug.Log($" Item unlocked: {itemName}");
        }
    }

    public void AddNewItem(string itemName, int price)
    {
        if (!_availableItems.ContainsKey(itemName))
        {
            _availableItems.Add(itemName, Mathf.Max(0, price));
            Debug.Log($" Added new item: {itemName} (price {price})");
        }
    }

    public int GetItemPrice(string itemName)
    {
        if (_availableItems.ContainsKey(itemName))
            return _availableItems[itemName];
        Debug.LogWarning($" Item not found: {itemName}");
        return -1;
    }
    #endregion

    public void SetCardManager(CardManager manager)
    {
        _cardManager = manager;
        Debug.Log("[StoreManager] CardManager dependency set.");
    }
}
