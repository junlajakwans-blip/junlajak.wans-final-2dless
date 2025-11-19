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
    public Button buyButton;

    private StoreItem _item;

    /// <summary>
    /// Setup slot visual & enable interaction
    /// </summary>
    public void Setup(StoreItem item, int price, Sprite currencySprite, bool unlocked, System.Action<StoreItem> onClickPurchase)
    {
        _item = item;

        gameObject.SetActive(true);

        icon.sprite = item.Icon;
        titleText.text = item.DisplayName;

        if (unlocked)
        {
            priceText.text = "UNLOCKED";
            priceText.color = Color.green;
            buyButton.interactable = false;
        }
        else
        {
            priceText.text = price.ToString();
            priceText.color = Color.white;
            buyButton.interactable = true;
        }

        if (currencyIcon != null)
            currencyIcon.sprite = currencySprite;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onClickPurchase?.Invoke(item));
    }

    /// <summary>
    /// Hide whole slot
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
