using System.Collections.Generic;
using UnityEngine;

public class StoreExchange : StoreBase
{
    public override string StoreName => "Currency Exchange";

    // ใช้ ID ภายใน (เพื่อป้องกันปัญหาชื่อ UI เปลี่ยนแล้วพัง)
    private const string EXCHANGE_TOKEN = "EXCHANGE_TOKEN";
    private const string EXCHANGE_KEY   = "EXCHANGE_KEY";

    // Key = internal ID / Value = ราคาเหรียญที่ต้องใช้
    public override Dictionary<string, int> StoreItems { get; } = new Dictionary<string, int>
    {
        { EXCHANGE_TOKEN, Currency.COIN_PER_TOKEN },
        { EXCHANGE_KEY,   Currency.COIN_PER_KEY }
    };

    private StoreManager _storeManager;
    private Currency _currency;

    public override void Initialize(StoreManager manager)
    {
        _storeManager = manager;
        _currency     = manager.Currency;
    }

public override bool Purchase(string itemName)
    {
        if (_currency == null)
        {
            Debug.LogError("[StoreExchange] Currency is NULL");
            return false;
        }

        int currentCoin = _currency.Coin; 
        int cost = StoreItems.ContainsKey(itemName) ? StoreItems[itemName] : -1;
        
        // นี่คือการตรวจสอบ Coin ที่ StoreExchange เห็น
        if (cost > currentCoin)
        {
            // Log นี้จะบอกคุณชัดเจนว่า StoreExchange เห็นเงินไม่พอ (แม้ UI จะเห็น)
            Debug.LogWarning($"[StoreExchange] Purchase FAILED: Insufficient funds. Have {currentCoin} Coin, Need {cost} Coin. (Check DevCheat/Save Logic)");
            return false;
        }

        switch (itemName)
        {
            case EXCHANGE_TOKEN:
                // Log นี้จะบอกผลลัพธ์สุดท้ายของการแลกเปลี่ยน
                bool tokenSuccess = _currency.ExchangeCoinToToken(1);
                Debug.Log($"[StoreExchange] TOKEN Exchange Result: {tokenSuccess}. Coin after attempt: {_currency.Coin}");
                return tokenSuccess;

            case EXCHANGE_KEY:
                // Log นี้จะบอกผลลัพธ์สุดท้ายของการแลกเปลี่ยน
                bool keySuccess = _currency.ExchangeCoinToKey(1);
                Debug.Log($"[StoreExchange] KEY Exchange Result: {keySuccess}. Coin after attempt: {_currency.Coin}");
                return keySuccess;

            default:
                Debug.LogWarning($"[StoreExchange] Unknown item: {itemName}");
                return false;
        }
    }


    public override void DisplayItems()
    {
        Debug.Log($"--- {StoreName} ---");
        foreach (var item in StoreItems)
            Debug.Log($"{item.Key} → {item.Value} coins");
    }

    public override StoreType StoreType => StoreType.Exchange; 
}