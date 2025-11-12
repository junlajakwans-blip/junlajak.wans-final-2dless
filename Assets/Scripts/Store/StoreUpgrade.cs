using System.Collections.Generic;
using UnityEngine;

public class StoreUpgrade : StoreBase
{
    #region Fields
    [Header("Upgrade Data")]
    private Dictionary<string, int> _upgradeLevels = new Dictionary<string, int>();
    [SerializeField] private int _maxUpgradeLevel = 5;
    
    // New Fields for Dynamic Pricing (Based on 200 Coin base and 1.5x multiplier)
    private const float UPGRADE_MULTIPLIER = 1.5f;
    private const string WHEY_PROTEIN = "Whey Protein";
    private const int HP_BONUS_PER_LEVEL = 10;
    #endregion

    #region Initialization
    public override void Initialize(StoreManager storeManager)
    {
        base.Initialize(storeManager);
        _storeName = "Permanent Upgrades";
        
        // Initialize the base price for Whey Protein (Level 1 price = 200 Coin)
        _storeItems.Add(WHEY_PROTEIN, 200); 
    }
    #endregion

    #region Override Methods
    public override void DisplayItems()
    {
        Debug.Log($" Store: {_storeName}");
        foreach (var upgrade in _storeItems)
        {
            int level = GetUpgradeLevel(upgrade.Key);
            int nextPrice = GetItemPrice(upgrade.Key);

            Debug.Log($" - {upgrade.Key}: {nextPrice} coins (Level {level}/{_maxUpgradeLevel})");
        }
    }

    /// <summary>
    /// Purchase method handles dynamic price calculation for the next level.
    /// </summary>
    public override bool Purchase(string itemName)
    {
        int price = GetItemPrice(itemName); // Gets price for the *next* level
        
        if (price <= 0)
        {
            Debug.LogWarning($" Invalid upgrade item: {itemName}");
            return false;
        }

        int currentLevel = GetUpgradeLevel(itemName);

        if (currentLevel >= _maxUpgradeLevel)
        {
            Debug.Log($" {itemName} is already at max level.");
            return false;
        }

        if (_storeManager.Currency.UseCoin(price))
        {
            ApplyUpgrade(itemName);
            Debug.Log($" Upgrade purchased: {itemName} (Level {GetUpgradeLevel(itemName)}/{_maxUpgradeLevel})");
            return true;
        }

        Debug.Log($" Not enough coins for upgrade: {itemName}");
        return false;
    }

    /// <summary>
    /// Calculates the price for the NEXT level based on the current level and multiplier (1.5x).
    /// </summary>
    public override int GetItemPrice(string itemName)
    {
        if (!_storeItems.ContainsKey(itemName))
        {
            Debug.LogWarning($" Item '{itemName}' not found in {_storeName} store.");
            return -1;
        }

        int basePrice = _storeItems[itemName]; // Base is 200
        int currentLevel = GetUpgradeLevel(itemName);
        
        // Price for next level = Base Price * 1.5 ^ (Current Level)
        float price = basePrice * Mathf.Pow(UPGRADE_MULTIPLIER, currentLevel); 
        
        // Round up to nearest whole number for clean prices (e.g., 675, 1012)
        return Mathf.RoundToInt(price);
    }
    #endregion

    #region Upgrade Logic
    public void ApplyUpgrade(string itemName)
    {
        if (!_upgradeLevels.ContainsKey(itemName))
            _upgradeLevels[itemName] = 0;

        _upgradeLevels[itemName]++;
        _storeManager.UnlockItem(itemName);

        string effectLog = "";
        int newLevel = _upgradeLevels[itemName];

        //  Whey Protein Logic: +10 Max HP per level
        if (itemName.Equals(WHEY_PROTEIN, System.StringComparison.OrdinalIgnoreCase))
        {
            // NOTE: External system (GameManager/PlayerData) must read this level 
            // and apply the corresponding HP multiplier (Level * 10).
            effectLog = $" → Permanent Max HP +{HP_BONUS_PER_LEVEL} (Total Bonus: {HP_BONUS_PER_LEVEL * newLevel})";
        }
        
        Debug.Log($" Applied upgrade for {itemName}{effectLog} (New Level: {newLevel}/{_maxUpgradeLevel})");
    }

    public int GetUpgradeLevel(string itemName)
    {
        // Ensures the item exists in the levels tracker, defaulting to 0
        return _upgradeLevels.ContainsKey(itemName) ? _upgradeLevels[itemName] : 0;
    }
    #endregion
}