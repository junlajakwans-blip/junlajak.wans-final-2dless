using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI; // For Image And Icon

public class StoreUI : MonoBehaviour
{
#region UI References 


    [Header("Currency & Title")]
    [SerializeField] private TextMeshProUGUI _coinText;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _keyText;
    [SerializeField] private TextMeshProUGUI _storeTitleText;

    [Header("Slot Management")]
    [SerializeField] private List<SlotUI> itemSlots;
    
    [Header("UI Containers")]
    [SerializeField] private GameObject panelUpgrade;
    [SerializeField] private GameObject panelExchange;
    [SerializeField] private GameObject slotContainer;
    [SerializeField] private UpgradeUI upgradeUI;

#endregion

#region Item Icons for use inspector

    [Header("Slot Components")]
    [SerializeField] private Sprite iconWay;
    [SerializeField] private Sprite iconToken;
    [SerializeField] private Sprite iconKeyMap;
    [SerializeField] private Sprite iconUnknown;

#endregion

#region Internal State

    private StoreManager _storeManager;
    private List<StoreBase> _availableStoreFacades = new List<StoreBase>();
    private StoreBase _activeStoreFacade; 
    private string _selectedItemName = string.Empty;

    // Public Property 
    public StoreBase ActiveStore => _activeStoreFacade; 

#endregion


#region Initialization

    /// <summary>
    /// Initializes the Store UI, sets up references, and activates the first store.
    /// </summary>
    public void InitializeStore(StoreManager manager, List<StoreBase> storesToDisplay)
    {
        _storeManager = manager;
        _availableStoreFacades = storesToDisplay;

        Debug.Log($"[StoreUI] Initialized — StoreManager: {(_storeManager != null ? "READY" : "NULL")}");
        Debug.Log($"[StoreUI] Stores received = {_availableStoreFacades.Count}");

        // Set the first store as active and load its items
        SetActiveStore(_availableStoreFacades.FirstOrDefault());
        // Update the currency display
        RefreshCurrency();
    }

    /// <summary>
    /// Sets the provided store as the currently active facade and refreshes the UI.
    /// </summary>
    public void SetActiveStore(StoreBase store)
    {
        if (store == null) return;

        // Update active store reference and title text
        _activeStoreFacade = store;
        _storeTitleText.text = store.StoreName;

        // Run the logic to fill the item slots
        PopulateSlots(store);

        // Ensure currency display is current
        RefreshCurrency();
    }
#endregion


#region Core Slot Logic
    /// <summary>
    /// Populates the UI slots based on the active store's item list.
    /// This method ensures item keys are mapped to fixed slot indices.
    /// </summary>
    private void PopulateSlots(StoreBase store)
    {
        // 1. Clear/Hide all existing slots before drawing new ones
        foreach (var slot in itemSlots)
        {
            slot.gameObject.SetActive(false); 
        }

        // 2. Loop through all items available in the current store
        foreach (var kvp in store.StoreItems)
        {
            string itemName = kvp.Key;
            int price = kvp.Value;
            SlotUI slot = null;

            // 3. Map the internal item ID (key) to a specific slot index
            switch (itemName)
            {
                // --- Exchange Items ---
                case "EXCHANGE_TOKEN":
                    slot = itemSlots[0]; // Fixed Slot for Token
                    break;
                case "EXCHANGE_KEY":
                    slot = itemSlots[1]; // Fixed Slot for Key
                    break;
                case "Way": 
                case "Whey":
                    slot = itemSlots[2]; // Fixed Slot for Whey
                    break;
                    
                // --- Upgrade Items ---
                case "UPGRADE_WHEY_HP":
                    slot = itemSlots[3]; // Fixed Slot for specific Upgrade
                    break;

                default:
                    Debug.LogWarning($"[StoreUI] Unknown Item Name: {itemName} in store {store.StoreName}. Skipping.");
                    continue;
            }

            if (slot == null) continue;

            // 4. Activate the slot and display data
            slot.gameObject.SetActive(true);
            slot.priceText.text = $"{price}";
            slot.icon.sprite = GetIconForItem(itemName); // Get sprite from resolver

            // 5. Set Buy Button Listener
            slot.buyButton.onClick.RemoveAllListeners();
            // Bind the unique item name to the purchase method
            slot.buyButton.onClick.AddListener(() => PurchaseItem(itemName)); 
        }
    }


    // --- [These methods are RECOMMENDED to be placed inside the #region Core Slot Logic] ---

    /// <summary>
    /// Retrieves the correct Sprite icon based on the item's unique ID/Name.
    /// </summary>
    private Sprite GetIconForItem(string itemName)
    {
        return itemName switch
        {
            "EXCHANGE_TOKEN" => iconToken, 
            "EXCHANGE_KEY" => iconKeyMap,
            "Way" => iconWay,
            "Whey" => iconWay, 
            "UPGRADE_WHEY_HP" => iconWay, 
            
            _ => iconUnknown
        };
    }

    /// <summary>
    /// Executes the purchase logic for a specific item in the active store.
    /// This is typically bound directly to the buy button listener in PopulateSlots.
    /// </summary>
    private void PurchaseItem(string itemName)
    {
        if (_activeStoreFacade == null) return; 

        bool result = _activeStoreFacade.Purchase(itemName);

        if (result)
        {
            RefreshCurrency();
            // Refresh slot UI status after a successful purchase
            PopulateSlots(_activeStoreFacade); 
        }
    }
#endregion


#region Store Navigation
    /// <summary>
    /// Switches the active store view (Exchange or Upgrade) by finding the correct store facade
    /// and calling the appropriate display method.
    /// </summary>
    public void SwitchStore(StoreType type)
    {
        if (_availableStoreFacades == null || _availableStoreFacades.Count == 0)
        {
            Debug.LogWarning($"[StoreUI] Store list is not yet initialized or is empty. Cannot switch to {type}.");
            return;
        }

        foreach (var store in _availableStoreFacades)
        {
            // If the target is Exchange
            if (store is StoreExchange && type == StoreType.Exchange)
            {
                SetActiveStore(store);
                DisplayStoreItems(store); // Activates the Exchange panel view
                return;
            }

            // If the target is Upgrade
            if (store is StoreUpgrade && type == StoreType.Upgrade)
            {
                SetActiveStore(store);
                DisplayUpgrade(store); // Activates the Upgrade panel view
                return;
            }
        }

        Debug.LogWarning($"[StoreUI] No store found for type {type}");
    }
#endregion


#region UI Interaction Handlers
    
    /// <summary>
    /// Records the name of the item currently selected by the player.
    /// Used primarily when there's a two-step selection/purchase process.
    /// </summary>
    public void SelectItem(string itemName)
    {
        _selectedItemName = itemName;
        Debug.Log($"[StoreUI] Selected → {itemName}");
    }

    /// <summary>
    /// Executes the purchase of the currently selected item.
    /// </summary>
    public void PurchaseSelectedItem()
    {
        // Guard check for selection/active store
        if (string.IsNullOrEmpty(_selectedItemName) || _activeStoreFacade == null)
        return;

        bool purchased = _activeStoreFacade.Purchase(_selectedItemName); 

        if (purchased)
        {
            Debug.Log($"[StoreUI] Purchase Success → {_selectedItemName}");
            // Reload the active store (which calls PopulateSlots) to refresh item status
            SetActiveStore(_activeStoreFacade); 
            RefreshCurrency();
            
            upgradeUI?.Refresh(); // Refresh associated upgrade UI if needed
        }
        else
        {
             int price = _activeStoreFacade.StoreItems.ContainsKey(_selectedItemName) ? _activeStoreFacade.StoreItems[_selectedItemName] : -1;
             Debug.LogWarning($"[StoreUI] Purchase FAILED for {_selectedItemName}. Price: {price}. Check currency or max level.");
        }

        // Notify UIManager to refresh overall store view (if necessary)
        UIManager.Instance.RefreshStoreUI();
    }
    
    // NOTE: GetIconForItem and PurchaseItem are moved to the #region Core Slot Logic
    // to separate private core logic from public event handlers.
    
#endregion


#region Panel Control (Private & Public)
    
    /// <summary>
    /// Activates the Upgrade panel and deactivates the Exchange panel. 
    /// Finds and sets the Upgrade store as active.
    /// </summary>
    public void ShowUpgrade()
    {
        panelUpgrade.SetActive(true);
        panelExchange.SetActive(false);
        SetActiveStore(_availableStoreFacades.Find(s => s is StoreUpgrade));
    }

    /// <summary>
    /// Activates the Exchange panel and deactivates the Upgrade panel.
    /// Finds and sets the Exchange store as active.
    /// </summary>
    public void ShowExchange()
    {
        panelUpgrade.SetActive(false);
        panelExchange.SetActive(true);
        SetActiveStore(_availableStoreFacades.Find(s => s is StoreExchange));
    }

    /// <summary>
    /// Configures the UI to display Exchange items within the slot container.
    /// </summary>
    private void DisplayStoreItems(StoreBase store)
    {
        if(slotContainer) slotContainer.SetActive(true);
        if(upgradeUI) upgradeUI.gameObject.SetActive(false);

        // Populate the slots with the current store's items
        PopulateSlots(store); 

        RefreshCurrency();
    }

    /// <summary>
    /// Configures the UI to display Upgrade items within the slot container and refreshes UpgradeUI details.
    /// </summary>
    private void DisplayUpgrade(StoreBase store)
    {
        // Panel setup: Show slots, hide the main upgrade panel component (if used elsewhere)
        if(slotContainer) slotContainer.SetActive(true);
        if(upgradeUI) upgradeUI.gameObject.SetActive(false); 

        // Load upgrade items into the generic slots
        PopulateSlots(store);

        // Refresh specific upgrade details (if the upgradeUI component is used for stats/levels)
        upgradeUI.Refresh(); 
        RefreshCurrency();
    }

#endregion


#region Currency Management

    /// <summary>
    /// Refreshes the currency display (Coin, Token, Key) by fetching the current values from GameManager.
    /// </summary>
    public void RefreshCurrency()
    {
        var currency = GameManager.Instance?.GetCurrency();
        if (currency == null) return;

        if (_coinText) _coinText.text = $"x{currency.Coin}";
        if (_tokenText) _tokenText.text = $"x{currency.Token}";
        if (_keyText) _keyText.text = $"x{currency.KeyMap}";
    }

    /// <summary>
    /// Refreshes the currency display with specific provided values (Overload).
    /// </summary>
    public void RefreshCurrency(int coin, int token, int key)
    {
        if (_coinText) _coinText.text = $"x{coin}";
        if (_tokenText) _tokenText.text = $"x{token}";
        if (_keyText) _keyText.text = $"x{key}";
    }

#endregion


}