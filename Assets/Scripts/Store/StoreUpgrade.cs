using System.Collections.Generic;
using UnityEngine;

public class StoreUpgrade : StoreBase
{
    #region Fields
    private Dictionary<string, int> _upgradeLevels = new Dictionary<string, int>();
    private int _maxUpgradeLevel = 5;
    #endregion

    #region Override Methods
    public override void DisplayItems()
    {
        Debug.Log($" Store: {_storeName}");
        foreach (var upgrade in _storeItems)
        {
            int level = _upgradeLevels.ContainsKey(upgrade.Key) ? _upgradeLevels[upgrade.Key] : 0;
            Debug.Log($" - {upgrade.Key}: {upgrade.Value} coins (Level {level}/{_maxUpgradeLevel})");
        }
    }

    public override bool Purchase(string itemName)
    {
        int price = GetItemPrice(itemName);
        if (price <= 0)
        {
            Debug.LogWarning($" Invalid upgrade item: {itemName}");
            return false;
        }

        if (!_upgradeLevels.ContainsKey(itemName))
            _upgradeLevels[itemName] = 0;

        if (_upgradeLevels[itemName] >= _maxUpgradeLevel)
        {
            Debug.Log($" {itemName} is already at max level.");
            return false;
        }

        if (_storeManager.Currency.UseCoin(price))
        {
            ApplyUpgrade(itemName);
            Debug.Log($" Upgrade purchased: {itemName} (Level {_upgradeLevels[itemName]})");
            return true;
        }

        Debug.Log($" Not enough coins for upgrade: {itemName}");
        return false;
    }
    #endregion

    #region Upgrade Logic
    public void ApplyUpgrade(string itemName)
    {
        if (!_upgradeLevels.ContainsKey(itemName))
            _upgradeLevels[itemName] = 0;

        _upgradeLevels[itemName]++;
        _storeManager.UnlockItem(itemName);

        Debug.Log($" Applied upgrade for {itemName} → Level {_upgradeLevels[itemName]}");
    }

    public int GetUpgradeLevel(string itemName)
    {
        return _upgradeLevels.ContainsKey(itemName) ? _upgradeLevels[itemName] : 0;
    }
    #endregion
}
