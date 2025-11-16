using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    public TextMeshProUGUI priceText;
    public Image icon;
    public Button buyButton;
    public Image currencyIcon;


    public void SetVisible(bool isVisible)
{
    gameObject.SetActive(true); 
    priceText.gameObject.SetActive(isVisible);
    icon.gameObject.SetActive(isVisible);
    buyButton.gameObject.SetActive(isVisible);
}
}
