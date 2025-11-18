using System;
using System.Collections.Generic;
using UnityEngine;

public class StoreMap : StoreBase
{
    public override string StoreName => "Map Unlock Portal";

    // รายการในร้าน: Key = ชื่อแมพ , Value = ราคาเป็นจำนวนกุญแจ
    public override Dictionary<string, int> StoreItems { get; } = new Dictionary<string, int>();

    private StoreManager _storeManager;
    private List<string> _unlockedMaps = new List<string>();

    private const int KEY_COST = Currency.KEYS_PER_MAP; // ค่า 3 กุญแจต่อการปลดล็อก map

    // ส่ง event บอก GameManager / UI
    public Action<string> OnMapUnlockedEvent;

    public override StoreType StoreType => StoreType.Exchange;

    private Player _playerRef;
    private CollectibleSpawner _spawnerRef;
    private CardManager _cardManagerRef;


    public override void Initialize(StoreManager manager)
    {
        _storeManager = manager;

        // แมพที่เริ่มเกมมาจะไม่ต้องซื้อ
        // School -> default unlocked

        AddMap("Road Traffic");
        AddMap("Kitchen");
    }

    public override bool Purchase(string itemName)
    {
        if (!StoreItems.ContainsKey(itemName))
        {
            Debug.LogWarning($"[StoreMap] No such map item: {itemName}");
            return false;
        }

        if (_unlockedMaps.Contains(itemName))
        {
            Debug.Log($"[StoreMap] {itemName} already unlocked.");
            return false;
        }

        // ใช้ระบบใช้กุญแจ (3 ดอก)
        if (!_storeManager.Currency.UnlockMap())
        {
            Debug.Log($"[StoreMap] Not enough keys → Need {KEY_COST}");
            return false;
        }

        UnlockMap(itemName);
        Debug.Log($"[StoreMap] Map unlocked → {itemName}");
        return true;
    }

    public override void DisplayItems()
    {
        foreach (var map in StoreItems)
        {
            bool unlocked = _unlockedMaps.Contains(map.Key);
            Debug.Log($" - {map.Key} | Cost: {KEY_COST} Keys | Status: {(unlocked ? "UNLOCKED" : "LOCKED")}");
        }
    }

    // ---------------- HELPER ----------------
    private void AddMap(string mapName)
    {
        if (!StoreItems.ContainsKey(mapName))
            StoreItems.Add(mapName, KEY_COST);
    }

    private void UnlockMap(string mapName)
    {
        if (!_unlockedMaps.Contains(mapName))
        {
            _unlockedMaps.Add(mapName);
            _storeManager.UnlockItem(mapName);
            OnMapUnlockedEvent?.Invoke(mapName);
        }
    }
}
