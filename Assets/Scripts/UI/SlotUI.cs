using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image icon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI priceText;
    public Image currencyIcon;
    public Button iconButton;

    [Header("Buy Button Sprites")]
    [SerializeField] private Sprite buyButtonExchange;
    [SerializeField] private Sprite buyButtonUpgrade;
    [SerializeField] private Sprite buyButtonMap;

    private StoreItem _item;

    public void Setup(StoreItem item, int price, Sprite currencySprite, bool unlocked, System.Action<StoreItem> onClickPurchase)
    {
        _item = item;
        gameObject.SetActive(true);

        // ---- Basic ----
        icon.sprite = item.Icon;
        titleText.text = item.DisplayName;

        // ---- Currency Icon ----
        if (currencyIcon != null)
            currencyIcon.sprite = currencySprite;

        // ---- Price Logic ----
        if (item.StoreType == StoreType.Exchange)
        {
            // Exchange = ซื้อได้เรื่อย ๆ
            priceText.text = price.ToString();
            priceText.color = Color.white;
            iconButton.interactable = true;
        }
        else
        {
            // Map + Upgrade = ซื้อครั้งเดียว
            if (unlocked)
            {
                priceText.text = "UNLOCKED";
                priceText.color = Color.green;
                iconButton.interactable = false;
            }
            else
            {
                priceText.text = price.ToString();
                priceText.color = Color.white;
                iconButton.interactable = true;
            }
        }

        // ---- Change Button Sprite by StoreType ----
        if (iconButton != null)
        {
            var btnImage = iconButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = item.StoreType switch
                {
                    StoreType.Exchange => buyButtonExchange,
                    StoreType.Upgrade  => buyButtonUpgrade,
                    StoreType.Map      => buyButtonMap,
                    _ => btnImage.sprite
                };
            }
        }

        // ---- Click Event ----
        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(() => onClickPurchase?.Invoke(item));
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
