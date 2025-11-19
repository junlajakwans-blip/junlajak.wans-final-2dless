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

    private readonly Dictionary<string, Sprite> _iconLookup = new Dictionary<string, Sprite>();

    // Currency Reference ที่ถูก Inject
    private Currency _currencyRef; 
    private string _selectedItemName = string.Empty;

    // Public Property 
    public StoreBase ActiveStore => _activeStoreFacade; 

#endregion


#region  Dependencies
    public void SetDependencies(Currency currency, List<StoreBase> stores, StoreManager manager)
    {
        // ป้องกัน event ซ้ำหรือ memory leak
        Currency.OnCurrencyChanged -= RefreshCurrency;
        Currency.OnCurrencyChanged += RefreshCurrency;


        _currencyRef = currency;
        _storeManager = manager;
        _availableStoreFacades = stores;

        if (_availableStoreFacades != null && _availableStoreFacades.Any())
        {
            _activeStoreFacade = _availableStoreFacades
                .FirstOrDefault(s => s.StoreType == StoreType.Exchange)
                ?? _availableStoreFacades[0];

            // แสดงร้านแรกทันทีหลัง DI พร้อม
            SetActiveStore(_activeStoreFacade);
        }

        RefreshCurrency(); // ไม่เป็นไรเพราะเช็ค null แล้ว
        Debug.Log($"[DEBUG] Inject Store Count = {_availableStoreFacades?.Count}");

        if (_availableStoreFacades != null)
        {
            foreach (var s in _availableStoreFacades)
                Debug.Log($" → {s.StoreName} | Items = {s.Items?.Count}");
        }

    }

#endregion


#region Initialization

    /// <summary>
    /// Initializes the Store UI, sets up references, and activates the first store.
    /// </summary>
    public void InitializeStore(StoreManager manager, List<StoreBase> storesToDisplay, Currency currency)
    {
        _storeManager = manager;
        _availableStoreFacades = storesToDisplay;
        _currencyRef = currency;

        Debug.Log($"[StoreUI] Initialized — StoreManager: {(_storeManager != null ? "READY" : "NULL")}");

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
        RefreshCurrency(); 
    }
#endregion


    // --- [These methods are RECOMMENDED to be placed inside the #region Core Slot Logic] ---

    /// <summary>
    /// Retrieves the correct Sprite icon based on the item's unique ID/Name.
    /// </summary>


    private void Awake()
    {
        _iconLookup["EXCHANGE_TOKEN"] = iconToken;
        _iconLookup["EXCHANGE_KEY"] = iconKeyMap;
        _iconLookup["Way"] = iconWay;
        _iconLookup["Whey"] = iconWay;
        _iconLookup["UPGRADE_WHEY_HP"] = iconWay;
    }

    private Sprite GetIconForItem(string itemName)
    {
        return _iconLookup.TryGetValue(itemName, out var sprite) ? sprite : iconUnknown;
    }




#region Core Slot Logic
    /// <summary>
    /// Populates the UI slots based on the active store's item list.
    /// This method ensures item keys are mapped to fixed slot indices.
    /// </summary>
    private void PopulateSlots(StoreBase store)
    {
        for (int i = 0; i < itemSlots.Count; i++)
            itemSlots[i].Hide();

        int index = 0;
        foreach (StoreItem item in store.Items)
        {
            if (index >= itemSlots.Count) break;

            bool unlocked = store.IsUnlocked(item);
            int price = item.UseLevelScaling
                ? (store as StoreUpgrade)?.GetPrice(item) ?? item.Price
                : item.Price;

            Sprite icon = item.Icon;
            Sprite currencyIcon = GetCurrencyIcon(item.SpendCurrency);

            itemSlots[index].Setup(
                item,
                price,
                currencyIcon,
                unlocked,
                OnClickPurchase
            );

            index++;
        }
    }


    /// <summary>
    /// Executes the purchase logic for a specific item in the active store.
    /// This is typically bound directly to the buy button listener in PopulateSlots.
    /// </summary>
    private void OnClickPurchase(StoreItem item)
    {
        if (_activeStoreFacade == null) return;

        bool success = _activeStoreFacade.Purchase(item);

        if (success)
        {
            PopulateSlots(_activeStoreFacade);
            RefreshCurrency();
            upgradeUI?.Refresh();
        }
    }


    private Sprite GetCurrencyIcon(StoreCurrency currency)
    {
        return currency switch
        {
            StoreCurrency.Coin => iconWay,
            StoreCurrency.Token => iconToken,
            StoreCurrency.KeyMap => iconKeyMap,
            _ => iconUnknown
        };
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
    public void SelectItem(string itemName) => _selectedItemName = itemName;

    /// <summary>
    /// Executes the purchase of the currently selected item.
    /// </summary>
    private void PurchaseSelectedItem()
    {
        if (string.IsNullOrEmpty(_selectedItemName) || _activeStoreFacade == null)
            return;

        StoreItem item = _activeStoreFacade.GetItem(_selectedItemName);
        if (item == null)
        {
            Debug.LogError($"[StoreUI] Item not found → {_selectedItemName}");
            return;
        }

        bool purchased = _activeStoreFacade.Purchase(item);

        if (purchased)
        {
            Debug.Log($"[StoreUI] Purchase Success → {_selectedItemName}");
            SetActiveStore(_activeStoreFacade); 
            RefreshCurrency();
            upgradeUI?.Refresh();
        }
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
        var currency = _currencyRef;
        if (currency == null) return;

        if (_coinText) _coinText.text = $"x{currency.Coin}";
        if (_tokenText) _tokenText.text = $"x{currency.Token}";
        if (_keyText) _keyText.text = $"x{currency.KeyMap}";
    }

#endregion


private void OnDestroy()
{
    Currency.OnCurrencyChanged -= RefreshCurrency;
}
}