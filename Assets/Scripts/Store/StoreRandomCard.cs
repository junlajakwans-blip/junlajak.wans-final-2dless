using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StoreRandomCard – Handles the exchange of 1 Token for a random Career Card (Tier B-A).
/// </summary>
public class StoreRandomCard : StoreBase
{
    #region Fields
    [Header("Card Summon Settings")]
    private CardManager _cardManager;
    private const string SUMMON_NAME = "Random Summon";
    private const int TOKEN_COST = 1;

    #endregion

    #region Initialization
    public override void Initialize(StoreManager storeManager)
    {
        base.Initialize(storeManager);
        _storeName = "Card Exchange (Tier B-A)";
        
        // Add the service item (cost is tracked by Token, not Coin price in _storeItems)
        _storeItems.Add(SUMMON_NAME, TOKEN_COST); 
    }
    #endregion

    #region Override Methods
    public override void DisplayItems()
    {
        Debug.Log($" Store: {_storeName}");
        Debug.Log($" - {SUMMON_NAME}: {TOKEN_COST} Token");
    }

    /// <summary>
    /// Purchases the random card summon service using 1 Token.
    /// </summary>
    public override bool Purchase(string itemName)
    {
        if (itemName != SUMMON_NAME)
        {
            Debug.LogWarning($" Invalid item for this store: {itemName}. Must be '{SUMMON_NAME}'.");
            return false;
        }

        // Check currency (uses Token)
        if (_storeManager.Currency.UseToken(TOKEN_COST))
        {
            SummonRandomCard();
            Debug.Log($" Summon successful: 1 Card received.");
            return true;
        }

        Debug.Log($" Not enough tokens to summon card: Need {TOKEN_COST} Token.");
        return false;
    }
    #endregion

    #region Summon Logic
    /// <summary>
    /// Calls the CardManager to add a random Tier B-A card to the inventory.
    /// </summary>
    public void SummonRandomCard()
    {
        if (_cardManager == null)
        {
            Debug.LogError("[StoreRandomCard] Cannot summon card; CardManager is missing.");
            return;
        }

        // Delegate the complex drop rate and card creation logic to CardManager
        _cardManager.AddCareerCard(); 
        
        // Note: CardManager.AddCareerCard() already handles the B-A-S tier drop rate logic.
    }
    #endregion
}