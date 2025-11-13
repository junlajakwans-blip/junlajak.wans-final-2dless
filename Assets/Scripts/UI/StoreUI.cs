using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class StoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _currencyText;
    [SerializeField] private TextMeshProUGUI _tokenKeyText; 
    [SerializeField] private TextMeshProUGUI _storeTitleText;
    
    // ✅ FIX: ใช้ List<TextMeshProUGUI> เพื่อเป็นตัวแทนของ Slots 
    [SerializeField] private List<TextMeshProUGUI> _itemSlots = new List<TextMeshProUGUI>(); 
    
    // Runtime References to Logic
    private StoreManager _storeManager; 
    private List<StoreBase> _availableStoreFacades = new List<StoreBase>();
    private StoreBase _activeStoreFacade; 
    private string _selectedItemName = string.Empty; // Tracks the currently selected item
    #endregion

    #region Public Methods

    public void InitializeStore(StoreManager manager, List<StoreBase> storesToDisplay) 
    {
        _storeManager = manager;
        _availableStoreFacades = storesToDisplay;
        Debug.Log($" Store UI Initialized. Manager Status: {(_storeManager != null ? "Ready" : "MISSING")}");

        // Default to displaying the Upgrade Store items
        SetActiveStore(_availableStoreFacades.FirstOrDefault(s => s.StoreName == "Permanent Upgrades"));

        UpdateCurrencyDisplay();
    }
    
    public void SetActiveStore(StoreBase store)
    {
        if (store == null) return;

        _activeStoreFacade = store;
        
        if (_storeTitleText != null)
            _storeTitleText.text = store.StoreName;

        // Redraw Items
        int i = 0;
        foreach (var itemKVP in store.StoreItems)
        {
            if (i < _itemSlots.Count)
            {
               int price = store.GetItemPrice(itemKVP.Key);
               string currencyType = store is StoreRandomCard ? "Token" : (store is StoreMap ? "Keys" : "Coin");
               
               _itemSlots[i].text = $"{itemKVP.Key} | Cost: {price} {currencyType}";
               _itemSlots[i].gameObject.SetActive(true);
            }
            i++;
        }
        // Hide unused slots
        for (int j = i; j < _itemSlots.Count; j++)
        {
            _itemSlots[j].gameObject.SetActive(false);
        }
    }


    public void UpdateCurrencyDisplay()
    {
        if (_storeManager == null || _currencyText == null) return;
        
        int coins = _storeManager.Currency.Coin;
        int tokens = _storeManager.Currency.Token;
        int keys = _storeManager.Currency.KeyMap;
        
        // Assuming _currencyText shows Coin and _tokenKeyText shows Tokens/Keys
        _currencyText.text = $"Coins: {coins}";
        if (_tokenKeyText != null)
            _tokenKeyText.text = $"Tokens: {tokens} | Keys: {keys}";
    }
    
    // ⚠️ New method to accept all currency types from UIManager
    public void UpdateCurrencyDisplay(int currentCoins, int currentTokens, int currentKeys)
    {
        if (_currencyText != null)
            _currencyText.text = $"Coins: {currentCoins}";
        
        if (_tokenKeyText != null)
            _tokenKeyText.text = $"Tokens: {currentTokens} | Keys: {currentKeys}";
    }


    // Called by a button click (e.g., UI slot button).
    public void SelectItem(string itemName) 
    {
        _selectedItemName = itemName;
        Debug.Log($" Selected item for purchase: {itemName}");
    }

    public void PurchaseSelectedItem()
    {
        if (string.IsNullOrEmpty(_selectedItemName) || _activeStoreFacade == null)
        {
            Debug.LogWarning(" No item selected or no store active.");
            return;
        }

        StoreBase targetStore = _activeStoreFacade; // Purchase from the currently active store
        
        bool result = targetStore.Purchase(_selectedItemName); 
        
        if (result)
        {
            Debug.Log($" Purchased: {_selectedItemName} successful!");
            SetActiveStore(_activeStoreFacade); 
            UpdateCurrencyDisplay(); // Reflect currency change
        }
    }
    #endregion
}