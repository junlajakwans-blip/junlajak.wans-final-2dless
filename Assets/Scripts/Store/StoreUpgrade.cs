using UnityEngine;
using System.Collections.Generic;


public class StoreUpgrade : StoreBase
{
    public override string StoreName => "Permanent Upgrades";
    public override StoreType StoreType => StoreType.Upgrade;

    private Dictionary<string, int> levels = new();

    public override void Initialize(StoreManager manager, List<StoreItem> injectedItems)
    {
        base.Initialize(manager, injectedItems);
        _manager = manager;

        levels.Clear();

        foreach (var item in Items)
        {
            int savedLevel = _manager.ProgressData.GetUpgradeLevel(item.ID);
            levels[item.ID] = savedLevel;
        }
    }

    public void RenderToUI(List<SlotUI> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < Items.Count)
            {
                var item = Items[i];
                var slot = slots[i];

                slot.SetItemObject(item);
                slot.Init(_manager.Currency, this, null, clickedItem => Purchase(clickedItem));
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
        if (_manager == null || item == null)
            return false;

        int currentLevel = GetLevel(item);
        if (currentLevel >= item.MaxLevel)
        {
            Debug.Log($"[StoreUpgrade] {item.DisplayName} already MAX");
            return false;
        }

        int price = GetPrice(item);
        if (!_manager.Currency.UseCoin(price))
        {
            Debug.Log($"[StoreUpgrade] Not enough Coins → need {price}");
            return false;
        }

        // level up
        int newLevel = currentLevel + 1;
        levels[item.ID] = newLevel;
        _manager.ProgressData.UpgradeLevels[item.ID] = newLevel;

        Debug.Log($"[StoreUpgrade] {item.DisplayName} → Lv {newLevel}/{item.MaxLevel}");
        return true;
    }


    public override bool IsUnlocked(StoreItem item)
        => GetLevel(item) > 0;

    public int GetLevel(StoreItem item)
        => levels.TryGetValue(item.ID, out int lv) ? lv : 0;

    public int GetPrice(StoreItem item)
    {
        int lv = GetLevel(item);
        if (lv >= item.MaxLevel) return int.MaxValue;

        return Mathf.RoundToInt(item.Price * Mathf.Pow(item.PriceMultiplier, lv));
    }

    public int GetTotalHPBonus()
    {
        int sum = 0;
        foreach (var item in Items)
        {
            int lv = GetLevel(item);
            if (lv > 0 && item.StoreType == StoreType.Upgrade)
            {
                // ตอนนี้ให้ HP ขึ้น 10 ต่อ Level
                sum += lv * 10;
            }
        }
        return sum;
    }

}
