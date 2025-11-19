using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Store/Store Map")]
public class StoreMap : StoreBase
{
    public override string StoreName => "Map Unlock Portal";
    public override StoreType StoreType => StoreType.Map;

    private Currency _currency;
    private GameProgressData _progress;

    // Track unlocked maps during runtime
    private HashSet<string> unlocked = new();

    public Action<string> OnMapUnlockedEvent;

    public override void Initialize(StoreManager manager, List<StoreItem> itemList)
    {
        base.Initialize(manager, itemList);

        _manager = manager;
        _currency = manager.Currency;
        _progress = manager.ProgressData;

        // Load previously unlocked maps from save data
        foreach (string id in _progress.UnlockedMaps)
            unlocked.Add(id);
    }

    public override bool Purchase(StoreItem item)
    {
        // ถ้าปลดแล้ว
        if (unlocked.Contains(item.ID))
        {
            Debug.Log($"[StoreMap] {item.DisplayName} already unlocked.");
            return false;
        }

        // ต้องใช้ Key ในการปลดแมพ
        if (item.SpendCurrency != StoreCurrency.KeyMap)
        {
            Debug.LogError($"[StoreMap] {item.DisplayName} configured wrong! Map should spend KeyMap.");
            return false;
        }

        // เช็คจำนวน Key ต้องใช้ item.Price
        if (!_currency.UseKey(item.Price))
        {
            Debug.LogWarning($"[StoreMap] Not enough keys → Need {item.Price}");
            return false;
        }

        // ปลดล็อกสำเร็จ
        Unlock(item.ID);
        Debug.Log($"[StoreMap] UNLOCK SUCCESS → {item.DisplayName}");
        return true;
    }


    public override bool IsUnlocked(StoreItem item)
        => unlocked.Contains(item.ID);


    private void Unlock(string id)
    {
        unlocked.Add(id);

        // Save to Progress
        if (!_progress.UnlockedMaps.Contains(id))
            _progress.UnlockedMaps.Add(id);

        OnMapUnlockedEvent?.Invoke(id);
    }
}
