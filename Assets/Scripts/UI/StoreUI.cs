using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class StoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _coinText;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _keyText;
    [SerializeField] private TextMeshProUGUI _storeTitleText;

    [SerializeField] private List<TextMeshProUGUI> _itemSlots = new List<TextMeshProUGUI>();

    private StoreManager _storeManager;
    private List<StoreBase> _availableStoreFacades = new List<StoreBase>();
    private StoreBase _activeStoreFacade;
    private string _selectedItemName = string.Empty;
    #endregion

    #region Public API

    public void InitializeStore(StoreManager manager, List<StoreBase> storesToDisplay)
    {
        _storeManager = manager;
        _availableStoreFacades = storesToDisplay;

        Debug.Log($"[StoreUI] Initialized — StoreManager: {(_storeManager != null ? "READY" : "NULL")}");

        // Default open Upgrade store
        SetActiveStore(_availableStoreFacades.FirstOrDefault());
        RefreshCurrency();
    }

    public void SetActiveStore(StoreBase store)
    {
        if (store == null) return;

        _activeStoreFacade = store;
        _storeTitleText.text = store.StoreName;

        // Fill Slot Texts
        int i = 0;
        foreach (var kvp in store.StoreItems)
        {
            if (i >= _itemSlots.Count) break;

            int price = store.GetItemPrice(kvp.Key);
            string currencyType =
                store is StoreRandomCard ? "Token" :
                store is StoreMap ? "Key" :
                "Coin";

            _itemSlots[i].text = $"{kvp.Key} — {price} {currencyType}";
            _itemSlots[i].gameObject.SetActive(true);
            i++;
        }

        // Hide unused slots
        for (; i < _itemSlots.Count; i++)
            _itemSlots[i].gameObject.SetActive(false);

        RefreshCurrency();
    }

    public void SelectItem(string itemName)
    {
        _selectedItemName = itemName;
        Debug.Log($"[StoreUI] Selected → {itemName}");
    }

    public void PurchaseSelectedItem()
    {
        if (string.IsNullOrEmpty(_selectedItemName) || _activeStoreFacade == null)
        {
            Debug.LogWarning("[StoreUI] Purchase failed → No item selected.");
            return;
        }

        bool purchased = _activeStoreFacade.Purchase(_selectedItemName);

        if (purchased)
        {
            Debug.Log($"[StoreUI] Purchase Success → {_selectedItemName}");
            SetActiveStore(_activeStoreFacade);
            RefreshCurrency();
        }
        else
        {
            Debug.LogWarning($"[StoreUI] Purchase failed → {_selectedItemName}");
        }
    }

    #endregion

    #region Currency

    public void RefreshCurrency()
    {
        var currency = GameManager.Instance?.GetCurrency();
        if (currency == null) return;

        if (_coinText)  _coinText.text  = $"x{currency.Coin}";
        if (_tokenText) _tokenText.text = $"x{currency.Token}";
        if (_keyText)   _keyText.text   = $"x{currency.KeyMap}";
    }

    /// <summary>
    /// UIManager / DevCheat can call this directly
    /// </summary>
    public void RefreshCurrency(int coin, int token, int key)
    {
        if (_coinText)  _coinText.text  = $"x{coin}";
        if (_tokenText) _tokenText.text = $"x{token}";
        if (_keyText)   _keyText.text   = $"x{key}";
    }

    #endregion
}
