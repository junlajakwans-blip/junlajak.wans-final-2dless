using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private List<StoreItemUI> _storeItemList = new List<StoreItemUI>();
    [SerializeField] private StoreItemUI _selectedItem;
    [SerializeField] private TextMeshProUGUI _currencyText;
    #endregion

    #region Public Methods

    public void InitializeStore(List<string> items)
    {
        Debug.Log(" Initializing Store UI...");

        foreach (var itemUI in _storeItemList)
        {
            itemUI.gameObject.SetActive(false);
        }

        for (int i = 0; i < items.Count && i < _storeItemList.Count; i++)
        {
            _storeItemList[i].SetupItem(items[i]);
            _storeItemList[i].gameObject.SetActive(true);
        }
    }

    public void UpdateCurrencyDisplay(int currentCoins)
    {
        if (_currencyText != null)
            _currencyText.text = $"Coins: {currentCoins}";
    }

    public void SelectItem(StoreItemUI item)
    {
        _selectedItem = item;
        Debug.Log($" Selected item: {_selectedItem.ItemName}");
    }

    public void PurchaseSelectedItem()
    {
        if (_selectedItem == null)
        {
            Debug.LogWarning(" No item selected to purchase.");
            return;
        }

        bool result = StoreManagerLocator.Instance.PurchaseItem(_selectedItem.ItemName);
        if (result)
            Debug.Log($" Purchased: {_selectedItem.ItemName}");
        else
            Debug.Log($" Not enough coins to buy: {_selectedItem.ItemName}");
    }
    #endregion
}
