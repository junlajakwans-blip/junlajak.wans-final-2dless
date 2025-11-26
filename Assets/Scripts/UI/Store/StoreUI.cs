using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class StoreUI : MonoBehaviour
{
    [Header("Currency & Title")]
    [SerializeField] private TextMeshProUGUI _coinText;
    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _keyText;
    [SerializeField] private TextMeshProUGUI _storeTitleText;

    [Header("Slot Lists by Store Type")]
    [SerializeField] private List<SlotUI> exchangeSlots;
    [SerializeField] private List<SlotUI> mapSlots;
    [SerializeField] private List<SlotUI> upgradeSlots;

    [Header("Panels")]
    [SerializeField] private GameObject panelExchange;
    [SerializeField] private GameObject panelMap;
    [SerializeField] private GameObject panelUpgrade;

    [Header("Icons")]
    [SerializeField] private Sprite iconCoin;
    [SerializeField] private Sprite iconToken;
    [SerializeField] private Sprite iconKeyMap;

    private List<StoreBase> _stores = new();
    private StoreBase _activeStore;
    private Currency _currency;
    private StoreUpgrade _storeUpgrade;
    private StoreMap _storeMap;


    public StoreMap StoreMapRef => _storeMap;
    public IEnumerable<SlotUI> MapSlots => mapSlots;

    public static Sprite CoinIcon;
    public static Sprite TokenIcon;
    public static Sprite KeyMapIcon;
    public static bool IconsInitialized;
    
    private System.Action _onAfterPurchase;

    private void Awake()
    {

        if (panelExchange != null) panelExchange.SetActive(false);
        if (panelMap != null) panelMap.SetActive(false);
        if (panelUpgrade != null) panelUpgrade.SetActive(false);
    }


    public void SetDependencies(Currency currency, List<StoreBase> stores, StoreManager manager)
    {
        _currency = currency;
        _stores = stores;

        _storeUpgrade = stores.Find(s => s is StoreUpgrade) as StoreUpgrade;
        _storeMap = stores.Find(s => s is StoreMap) as StoreMap;

        // ย้ายจาก Awake มาไว้ตรงนี้ เพื่อให้ initialize ทันก่อน SlotUI ใช้งาน
        if (!IconsInitialized)
        {
            CoinIcon = iconCoin;
            TokenIcon = iconToken;
            KeyMapIcon = iconKeyMap;
            IconsInitialized = true;
            Debug.Log("[StoreUI] Currency Icons Initialized in SetDependencies()");
        }

        if (panelExchange != null) panelExchange.SetActive(false);
        if (panelMap != null) panelMap.SetActive(false);
        if (panelUpgrade != null) panelUpgrade.SetActive(false);
    }


    public void SetActiveStore(StoreBase store)
    {
        Debug.Log($"[STORE UI] SET ACTIVE STORE: {store.StoreName} | TYPE = {store.StoreType}");
        if (store == null) return;

        _activeStore = store;
        _storeTitleText.text = store.StoreName;

        panelExchange.SetActive(store is StoreExchange);
        panelMap.SetActive(store is StoreMap);
        panelUpgrade.SetActive(store is StoreUpgrade);

        PopulateSlots(store);
        RefreshCurrency();
    }

    private void HideAllSlots()
    {
        foreach (var s in exchangeSlots) if (s != null) s.Hide();
        foreach (var s in mapSlots) if (s != null) s.Hide();
        foreach (var s in upgradeSlots) if (s != null) s.Hide();
    }

    private List<SlotUI> GetSlotListByType(StoreType type)
    {
        return type switch
        {
            StoreType.Exchange => exchangeSlots,
            StoreType.Map => mapSlots,
            StoreType.Upgrade => upgradeSlots,
            _ => null
        };
    }

    public void PopulateSlots(StoreBase store)
    {
        Debug.Log("\n\n==============================");
        Debug.Log($"[STORE UI] >>> OPEN STORE: {store.StoreName} | TYPE = {store.StoreType}");
        Debug.Log($"Items in store = {store.Items.Count}");
        Debug.Log("==============================");

        HideAllSlots();

        if (store is StoreUpgrade su)
        su.SyncWithSave();


        List<SlotUI> targetSlotList = GetSlotListByType(store.StoreType);
        if (targetSlotList == null)
        {
            Debug.LogError($"[STORE UI] ❌ NO SLOT LIST for StoreType: {store.StoreType}");
            return;
        }

        for (int i = 0; i < store.Items.Count && i < targetSlotList.Count; i++)
        {
            var item = store.Items[i];
            var slot = targetSlotList[i];

            Debug.Log($"[STORE UI] Slot '{slot.name}' ← ITEM '{item.DisplayName}' | TYPE = {item.StoreType} | PRICE = {item.Price}");

            slot.SetItemObject(item);
            Debug.Log("[STORE UI] slot.SetItemObject OK");

            slot.Init(_currency, _storeUpgrade, _storeMap, OnClickPurchase);
            Debug.Log($"[STORE UI] slot.Init OK  → currency={_currency != null} upgradeRef={_storeUpgrade != null} mapRef={_storeMap != null}");

            slot.Show();
            Debug.Log($"[STORE UI] Slot '{slot.name}' → SHOWN");
        }
        
        Debug.Log("============================== END POPULATE ================\n");
        if (store is StoreUpgrade)
        StartCoroutine(RefreshUpgradeUI_NextFrame());

    }

    private void OnClickPurchase(StoreItem item)
    {
        Debug.Log($"\n==== PURCHASE '{item.DisplayName}' from {item.StoreType} ====");

        bool success = _activeStore.Purchase(item);
        Debug.Log($"Purchase Result = {success}");

        RefreshCurrency();
        UIManager.Instance?.RefreshStoreUI();

        if (item.StoreType == StoreType.Upgrade)
        {
            UIManager.Instance?.upgradeUI?.Init(_storeUpgrade, item);
        }

        // ห้าม refresh map slots เพื่อป้องกัน Map UI คืนค่าสถานะ lock
        foreach (var slot in exchangeSlots.Concat(mapSlots))
        {
            if (slot.gameObject.activeSelf)
                slot.Refresh();
        }

        // ⬇ UpgradeSlots ไม่ต้อง refresh เพราะจะทำให้ UI ดับลง
        if (item.StoreType == StoreType.Upgrade)
        {
            UIManager.Instance?.upgradeUI?.Init(_storeUpgrade, item);
        }

    }

    public void SwitchStore(StoreType type)
    {
        var nextStore = _stores.Find(s => s.StoreType == type);
        if (nextStore == null)
        {
            Debug.LogError($"[StoreUI] Store type {type} NOT FOUND");
            return;
        }
        if (nextStore is StoreUpgrade su)
        su.SyncWithSave();
        Debug.Log($"[StoreUI] SwitchStore → {nextStore.StoreName} | Items = {nextStore.Items.Count}");
        SetActiveStore(nextStore);
        if (type == StoreType.Upgrade)
        StartCoroutine(RefreshUpgradeUI_NextFrame());

    }

    public void RefreshCurrency()
    {
        _coinText.text = $"x{_currency.Coin}";
        _tokenText.text = $"x{_currency.Token}";
        _keyText.text = $"x{_currency.KeyMap}";
    }

    public static Sprite GetGlobalCurrencyIcon(StoreCurrency currency)
    {
        // Safety Init — ถ้ายังไม่ได้โหลด icon จาก Awake()
        if (!IconsInitialized)
        {
            Debug.LogWarning("[StoreUI] Currency icons requested before initialization.");
            return null;
        }

        return currency switch
        {
            StoreCurrency.Coin   => CoinIcon,
            StoreCurrency.Token  => TokenIcon,
            StoreCurrency.KeyMap => KeyMapIcon,
            _ => null
        };
    }

    public void RefreshActiveSlots()
    {
        if (_activeStore == null) return;

        List<SlotUI> targetSlots = _activeStore.StoreType switch
        {
            StoreType.Exchange => exchangeSlots,
            StoreType.Map => mapSlots,
            StoreType.Upgrade => upgradeSlots,
            _ => null
        };

        if (targetSlots == null) return;

        foreach (var slot in targetSlots)
        {
            if (slot.gameObject.activeSelf)
                slot.Refresh();
        }
    }


    public void OpenExchange() => SwitchStore(StoreType.Exchange);
    public void OpenUpgrade() => SwitchStore(StoreType.Upgrade);
    public void OpenMap() => SwitchStore(StoreType.Map);


    public void HighlightItem(string itemID)
    {
        foreach (var slot in mapSlots) // หรือ exchangeSlots / upgradeSlots ถ้าต้องการรองรับทุก store
        {
            if (slot.HasItem(itemID))   // เช็คว่า slot นี้ถือสินค้า ID นี้ไหม
            {
                slot.transform.SetAsLastSibling(); // หรือ scroll to view / outline effect
                break;
            }
        }
    }

    private System.Collections.IEnumerator RefreshUpgradeUI_NextFrame()
    {
        yield return null; // ← รอ 1 เฟรมให้ SlotUI instantiate เสร็จ

        if (UIManager.Instance?.upgradeUI != null && _storeUpgrade != null)
        {
            UIManager.Instance.upgradeUI.Init(_storeUpgrade, null);
        }
    }

}
