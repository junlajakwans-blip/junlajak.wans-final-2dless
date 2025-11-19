using UnityEngine;

public enum StoreCurrency
{
    Coin,
    Token,
    KeyMap
}

[CreateAssetMenu(fileName = "StoreItem_New", menuName = "DUFFDUCK/Store/Item")]
public class StoreItem : ScriptableObject
{
    [Header("Store Category")]
    public StoreType StoreType; // Exchange / Upgrade / Map

    [Header("Auto Generated Logic ID")]
    public string ID;

    [Header("Display")]
    public string DisplayName;
    public Sprite Icon;

    // ───────── Shared ─────────
    [Header("Price")]
    public StoreCurrency SpendCurrency;
    public int Price = 1;

    // ───────── Exchange Only ─────────
    [Header("Reward (Exchange Only)")]
    public StoreCurrency RewardCurrency;
    public int RewardAmount = 1;

    // ───────── Map Only ─────────
    [Header("Unlock (Map Only)")]
    public MapType mapType;
    public bool UnlockedByDefault = false;

    // ───────── Upgrade Only ─────────
    [Header("Upgrade Settings (Upgrade Only)")]
    public bool UseLevelScaling = false;
    public int MaxLevel = 5;
    public float PriceMultiplier = 1.5f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Generate ID แต่ไม่ rename asset → ป้องกัน Inspector Warning
        ID = $"{StoreType.ToString().ToUpper()}_{Mathf.Abs(GetInstanceID() % 10000)}";

        switch (StoreType)
        {
            case StoreType.Exchange:
                UseLevelScaling = false;
                UnlockedByDefault = false;
                // Exchange ใช้ได้ทุก SpendCurrency และให้ Reward
                break;

            case StoreType.Upgrade:
                SpendCurrency = StoreCurrency.Coin;
                RewardCurrency = 0;
                RewardAmount = 0;
                UnlockedByDefault = false;
                // Upgrade ใช้ coin เท่านั้น
                break;

            case StoreType.Map:
                SpendCurrency = StoreCurrency.KeyMap;
                RewardCurrency = 0;
                RewardAmount = 0;
                UseLevelScaling = false;
                // Map ใช้ KeyMap เท่านั้น
                break;
        }
    }
#endif
}
