using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StoreUpgrade – Handles permanent player stat upgrades.
/// Handles level scaling, price scaling, saving to ProgressData,
/// and communication with StoreUI / UpgradeUI.
/// </summary>
public class StoreUpgrade : StoreBase
{
    // Display name for UI
    public override string StoreName => "Permanent Upgrades";

    // Tell StoreUI this store uses Upgrade panel
    public override StoreType StoreType => StoreType.Upgrade;

    // Items shown in the shop UI (auto-filled by RefreshStorePrice)
    public override Dictionary<string, int> StoreItems { get; } = new Dictionary<string, int>();


    // Internal upgrade ID (for saving and logic)
    private const string UPGRADE_ID_WHEY = "UPGRADE_WHEY_HP";

    // UI item name (visible to player + must match SlotUI purchase call)
    private const string UI_NAME_WHEY = "Whey";

    // Upgrade configuration
    private const int BASE_PRICE = 500;
    private const float PRICE_MULTIPLIER = 1.5f;
    private const int HP_BONUS_PER_LEVEL = 10;
    private const int MAX_LEVEL = 5;

    private StoreManager _storeManager;
    private Dictionary<string, int> _upgradeLevels = new Dictionary<string, int>();


    /// <summary>
    /// Called when the StoreManager is created.
    /// </summary>
    public override void Initialize(StoreManager manager)
    {
        _storeManager = manager;
        RefreshStorePrice();
    }


    /// <summary>
    /// Called by StoreUI when the user presses BUY.
    /// </summary>
    public override bool Purchase(string itemName)
    {
        // Must match UI_NAME_WHEY from SlotUI
        if (itemName != UI_NAME_WHEY)
            return false;

        int level = GetLevel();

        if (level >= MAX_LEVEL)
        {
            Debug.Log("[StoreUpgrade] Already at MAX level.");
            return false;
        }

        int price = StoreItems[UI_NAME_WHEY];

        // Try spending coins
        if (!_storeManager.Currency.UseCoin(price))
        {
            Debug.Log("[StoreUpgrade] Not enough coins.");
            return false;
        }

        ApplyUpgrade();
        RefreshStorePrice();
        return true;
    }


    /// <summary>
    /// Actually applies the stat upgrade and saves progress data.
    /// </summary>
    private void ApplyUpgrade()
    {
        if (!_upgradeLevels.ContainsKey(UPGRADE_ID_WHEY))
            _upgradeLevels[UPGRADE_ID_WHEY] = 0;

        _upgradeLevels[UPGRADE_ID_WHEY]++;

        int level = _upgradeLevels[UPGRADE_ID_WHEY];

        // Save to progress
        _storeManager.ProgressData.PermanentHPUpgradeLevel = level;

        Debug.Log($"[UPGRADE] HP +{HP_BONUS_PER_LEVEL}  (Lv {level}/{MAX_LEVEL})");
    }


    /// <summary>
    /// Returns current upgrade level (0–5).
    /// </summary>
    private int GetLevel()
    {
        return _upgradeLevels.ContainsKey(UPGRADE_ID_WHEY)
            ? _upgradeLevels[UPGRADE_ID_WHEY]
            : 0;
    }

    /// <summary>
    /// Used by UpgradeUI to display glowing icons.
    /// </summary>
    public int GetWheyLevelForUI() => GetLevel();


    /// <summary>
    /// Rebuilds StoreItems with correct name and price.
    /// Called after Initialize() and after every purchase.
    /// </summary>
    private void RefreshStorePrice()
    {
        StoreItems.Clear();
        int level = GetLevel();

        // Max level = price is disabled (UI should gray out)
        if (level >= MAX_LEVEL)
        {
            StoreItems.Add(UI_NAME_WHEY, int.MaxValue);
            return;
        }

        int nextPrice = Mathf.RoundToInt(BASE_PRICE * Mathf.Pow(PRICE_MULTIPLIER, level));
        StoreItems.Add(UI_NAME_WHEY, nextPrice);
    }

    public override void DisplayItems()
    {
        // This store does not rely on console-printing anymore.
        // StoreUI handles all item display logic.
        foreach (var item in StoreItems)
        {
            int level = GetWheyLevelForUI();
            Debug.Log($"[StoreUpgrade] {item.Key} : {item.Value} coins (Lv {level}/{MAX_LEVEL})");
        }
    }
}
