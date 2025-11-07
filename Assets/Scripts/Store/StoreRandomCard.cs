using System.Collections.Generic;
using UnityEngine;

public class StoreRandomCard : StoreBase
{
    #region Fields
    [SerializeField] private List<string> _cardPool = new List<string>();
    [SerializeField] private int _drawPrice = 50;
    [SerializeField, Range(0f, 1f)] private float _rareChance = 0.1f;
    private System.Random _rng = new System.Random();
    #endregion

    #region Override Methods
    public override void DisplayItems()
    {
        Debug.Log($" Store: {_storeName}");
        Debug.Log($"Card draw price: {_drawPrice} coins");
        Debug.Log($"Rare chance: {_rareChance * 100}%");
    }

    public override bool Purchase(string itemName)
    {
        return DrawRandomCard();
    }
    #endregion

    #region Random Draw Logic
    public bool DrawRandomCard()
    {
        if (_cardPool == null || _cardPool.Count == 0)
        {
            Debug.LogWarning(" Card pool is empty, cannot draw.");
            return false;
        }

        if (!_storeManager.Currency.UseCoin(_drawPrice))
        {
            Debug.Log(" Not enough coins to draw a card.");
            return false;
        }

        string drawnCard = GetRandomCard();
        _storeManager.UnlockItem(drawnCard);

        Debug.Log($" You received card: {drawnCard}");
        return true;
    }

    private string GetRandomCard()
    {
        int index = _rng.Next(0, _cardPool.Count);
        string card = _cardPool[index];

        if (_rng.NextDouble() < _rareChance)
        {
            card = card.ToUpper() + " ★RARE★";
        }

        return card;
    }
    #endregion
}
