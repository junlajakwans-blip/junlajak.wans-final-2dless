using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Store/Store Exchange")]
public class StoreExchange : StoreBase
{
    public override string StoreName => "Currency Exchange";
    public override StoreType StoreType => StoreType.Exchange;

    private Currency _currency;

    public override void Initialize(StoreManager manager, List<StoreItem> itemList)
    {
        base.Initialize(manager, itemList);
        _currency = manager.Currency;
    }

    public override bool Purchase(StoreItem item)
    {
        if (_currency == null || item == null)
        {
            Debug.LogError("[StoreExchange] Currency or item is NULL");
            return false;
        }

        // ราคาที่ต้องจ่าย (SpendCurrency = Token / Key / Coin?)
        int price = item.Price;

        // ใช้สกุลเงินตามที่ StoreItem กำหนด
        bool success = item.SpendCurrency switch
        {
            StoreCurrency.Coin  => _currency.UseCoin(price),
            StoreCurrency.Token => _currency.UseToken(price),
            StoreCurrency.KeyMap => _currency.UseKey(price),
            _ => false
        };

        if (!success)
        {
            Debug.LogWarning($"[StoreExchange] Not enough {item.SpendCurrency} → Need {price}");
            return false;
        }

        // รับผลตอบแทนตาม RewardCurrency
        switch (item.RewardCurrency)
        {
            case StoreCurrency.Coin:
                _currency.AddCoin(item.RewardAmount);
                break;

            case StoreCurrency.Token:
                _currency.AddToken(item.RewardAmount);
                break;

            case StoreCurrency.KeyMap:
                _currency.AddKey(item.RewardAmount);
                break;

            default:
                Debug.LogWarning($"[StoreExchange] Unsupported reward type: {item.RewardCurrency}");
                return false;
        }

        Debug.Log($"[StoreExchange] SUCCESS → {item.DisplayName} purchased");
        return true;
    }
}
