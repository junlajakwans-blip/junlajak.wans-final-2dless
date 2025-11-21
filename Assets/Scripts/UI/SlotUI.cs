using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [Header("Assign Scriptable StoreItem")]
    [SerializeField] private StoreItem itemObject;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private Image buyButton;

    private Currency currency;
    private StoreUpgrade upgradeRef;
    private StoreMap mapRef;
    private System.Action<StoreItem> onBuy;

    /// <summary>
    /// Initializing slot
    /// </summary>
    public void Init(
        Currency currencyRef,
        StoreUpgrade upgradeStore,
        StoreMap mapStore,
        System.Action<StoreItem> onBuyCallback)
    {
        onBuy = onBuyCallback;
        currency = currencyRef;
        upgradeRef = upgradeStore;
        mapRef = mapStore;

        // Slot ไม่มี itemObject ไม่ควร Spawn
        if (itemObject == null)
        {
            Debug.LogError($"[SlotUI] itemObject is missing on {gameObject.name}");
            gameObject.SetActive(false);
            return;
        }

        // ป้องกัน UI ไม่ครบใน Inspector
        if (priceText == null) Debug.LogError($"[SlotUI] priceText missing on {gameObject.name}");
        if (currencyIcon == null) Debug.LogError($"[SlotUI] currencyIcon missing on {gameObject.name}");
        if (buyButton == null) Debug.LogError($"[SlotUI] buyButton missing on {gameObject.name}");

        // 🔹 Refresh หลังเช็ค itemObject แล้วเท่านั้น
        Refresh();

        //  รองรับ Image ที่ไม่มี Button -> auto add
        Button btn = buyButton.GetComponent<Button>();
        if (btn == null)
            btn = buyButton.gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            onBuy?.Invoke(itemObject);

            if (itemObject.StoreType == StoreType.Upgrade)
            {
                UIManager.Instance?.upgradeUI?.Init(upgradeRef, itemObject);
            }
        });


        gameObject.SetActive(true);
    }

    /// <summary>
    /// Updating UI when price or unlock condition changes
    /// </summary>
    public void Refresh()
    {
        Debug.Log($"[SlotUI Refresh] {name} | Item='{itemObject.DisplayName}' | Type={itemObject.StoreType} | currencyRef={(currency!=null)} upgradeRef={(upgradeRef!=null)} mapRef={(mapRef!=null)}");
        if (itemObject == null) return;
        if (currencyIcon == null || buyButton == null || priceText == null) return;

        currencyIcon.sprite = StoreUI.GetGlobalCurrencyIcon(itemObject.SpendCurrency);
        Button btn = buyButton.GetComponent<Button>();

        switch (itemObject.StoreType)
        {
            case StoreType.Exchange:
                priceText.text = itemObject.Price.ToString();
                priceText.color = Color.white;
                btn.interactable = true;
                break;

            case StoreType.Upgrade:
                if (upgradeRef == null)
                {
                    Debug.LogError($"[SlotUI] UpgradeRef missing on {gameObject.name}");
                    return;
                }
                int lv = upgradeRef.GetLevel(itemObject);
                if (lv >= itemObject.MaxLevel)
                {
                    priceText.text = "MAX";
                    priceText.color = Color.yellow;
                    btn.interactable = false;
                }
                else
                {
                    priceText.text = upgradeRef.GetPrice(itemObject).ToString();
                    priceText.color = Color.white;
                    btn.interactable = true;
                }
                break;

            case StoreType.Map:
                if (mapRef == null)
                {
                    Debug.LogError($"[SlotUI] MapRef missing on {gameObject.name}");
                    return;
                }

                bool unlocked = mapRef.IsUnlocked(itemObject);
                int have = currency.KeyMap;
                int need = itemObject.Price;

                if (unlocked)
                {
                    priceText.text = "UNLOCKED";
                    priceText.color = Color.green;
                    btn.interactable = false;
                }
                else
                {
                    priceText.text = $"{have}/{need}";
                    priceText.color = (have >= need) ? Color.green : Color.red;
                    btn.interactable = (have >= need);
                }
                break;
        }
    }

    public StoreType ItemType => itemObject.StoreType;

    public void SetItemObject(StoreItem obj) => itemObject = obj;
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public StoreItem CurrentItem => itemObject;

    public bool HasItem(string id)
    {
        bool result = itemObject != null && itemObject.ID == id;
        Debug.Log($"[SlotUI] HasItem? slot={name} itemObj={itemObject?.ID} check={id} → {result}");
        return result;
    }

    public bool HasType(StoreType type)
    {
        return itemObject != null && itemObject.StoreType == type;
    }

}
