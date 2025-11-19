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
    public string ID; // สร้างอัตโนมัติ เช่น EXCHANGE_001

    [Header("Display")]
    public string DisplayName;
    public Sprite Icon;

    [Header("Price Settings")]
    public StoreCurrency SpendCurrency; 
    public int Price = 1;

    [Header("Reward (ซื้อแล้วได้อะไร)")]
    public StoreCurrency RewardCurrency;
    public int RewardAmount = 1;

    [Header("Unlock Item (Map / Single Purchase Item)")]
    public bool UnlockedByDefault = false;

    [Header("Upgrade Settings")]
    public bool UseLevelScaling = false;
    public int MaxLevel = 5;
    public float PriceMultiplier = 1.5f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        string prefix = StoreType.ToString().ToUpper();
        int index = Mathf.Abs(GetInstanceID() % 10000);
        ID = $"{prefix}_{index}";
        name = $"[{StoreType}] {DisplayName}";
    }
#endif
}
