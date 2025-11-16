using UnityEngine;
using TMPro;

public class TopCurrencyUI : MonoBehaviour
{
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI tokenText;
    private TextMeshProUGUI keyText;

    private void Awake()
    {
        coinText  = transform.Find("Icon_Coin/Text_Currency").GetComponent<TextMeshProUGUI>();
        tokenText = transform.Find("Icon_Token/Text_Token").GetComponent<TextMeshProUGUI>();
        keyText   = transform.Find("Icon_KeyMap/Text_KeyMap").GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        Refresh();
        Currency.OnCurrencyChanged += Refresh;
    }

    private void OnDisable()
    {
        Currency.OnCurrencyChanged -= Refresh;
    }

    public void Refresh()
    {
        var cur = GameManager.Instance?.GetCurrency();
        if (cur == null) return;

        coinText.text  = $"x{cur.Coin}";
        tokenText.text = $"x{cur.Token}";
        keyText.text   = $"x{cur.KeyMap}";
    }
}
