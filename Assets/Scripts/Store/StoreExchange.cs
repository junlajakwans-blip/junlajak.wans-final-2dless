using System.Collections.Generic;
using UnityEngine;

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

    public void RenderToUI(List<SlotUI> slots, Sprite iconCoin, Sprite iconToken, Sprite iconKeyMap)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < Items.Count)
            {
                StoreItem item = Items[i];

                SlotUI slot = slots[i];

                // กำหนด ScriptableObject ให้ SlotUI
                slot.SetItemObject(item);
                slot.Init(_currency, null, null, (clickedItem) => Purchase(clickedItem));
                slot.gameObject.SetActive(true);

            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    public override bool Purchase(StoreItem item)
    {
        if (item == null || _currency == null) return false;

        // หักเงินตามชนิด
        bool success = item.SpendCurrency switch
        {
            StoreCurrency.Coin   => _currency.UseCoin(item.Price),
            StoreCurrency.Token  => _currency.UseToken(item.Price),
            StoreCurrency.KeyMap => _currency.UseKey(item.Price),
            _ => false
        };
        if (!success) return false;

        // ให้รางวัล
        switch (item.RewardCurrency)
        {
            case StoreCurrency.Coin:  _currency.AddCoin(item.RewardAmount); break;
            case StoreCurrency.Token: _currency.AddToken(item.RewardAmount); break;
            case StoreCurrency.KeyMap: _currency.AddKey(item.RewardAmount); break;
        }

        return true;
    }
}
