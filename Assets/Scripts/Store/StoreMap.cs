using UnityEngine;
using System;
using System.Collections.Generic;

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
        if (unlocked.Contains(item.ID))
        {
            Debug.Log($"[StoreMap] {item.DisplayName} already unlocked.");
            return false;
        }

        // ต้องใช้ KeyMap
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

        // ปลดแมพ
        Unlock(item);
        Debug.Log($"[StoreMap] UNLOCK SUCCESS → {item.DisplayName}");
        return true;
    }

    private void Unlock(StoreItem item)
    {
        // 1) Runtime unlocked
        unlocked.Add(item.ID);

        // 2) Save to progress
        if (!_progress.UnlockedMaps.Contains(item.ID))
            _progress.UnlockedMaps.Add(item.ID);

        // 3) Notify systems
        // ส่งทั้ง ID และ MapType ออกไป
        OnMapUnlockedEvent?.Invoke(item.mapType.ToString());

        Debug.Log($"[StoreMap] UNLOCKED MAP → {item.mapType} | ID = {item.ID}");
    }

    public string GetSceneName(StoreItem item)
    {
        return item.mapType.ToSceneName();
    }


}