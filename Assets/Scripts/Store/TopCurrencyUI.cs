using UnityEngine;
using TMPro;

public class TopCurrencyUI : MonoBehaviour
{
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI tokenText;
    private TextMeshProUGUI keyText;

    private Currency _currencyRef;


    #region Dependencies
    
    /// <summary>
    /// [FIX 1] Injects the Currency data object.
    /// </summary>
    public void SetDependencies(Currency currency)
        {
            // 1. Defensive Unsubscribe (เพื่อป้องกันการ Subscribe ซ้ำซ้อน)
            if (_currencyRef != null)
            {
                // ใช้ static event ที่ประกาศไว้ใน Currency.cs
                Currency.OnCurrencyChanged -= Refresh; 
            }

            // 2. Set the dependency
            _currencyRef = currency;

            if (_currencyRef != null)
            {
                // 3. Subscribe to the static event
                Currency.OnCurrencyChanged += Refresh;
                
                // 4. Perform initial refresh now that the reference is set
                Refresh(); 
            }
        }
    
    #endregion


    private void Awake()
    {
        coinText  = transform.Find("Icon_Coin/Text_Currency").GetComponent<TextMeshProUGUI>();
        tokenText = transform.Find("Icon_Token/Text_Token").GetComponent<TextMeshProUGUI>();
        keyText   = transform.Find("Icon_KeyMap/Text_KeyMap").GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (_currencyRef != null)
        {
            Refresh();
        }
        // ถ้า _currencyRef เป็น null แสดงว่ายังไม่พร้อม ก็ไม่ต้องทำอะไร (ปล่อยให้ SetDependencies จัดการ)
    }

    private void OnDisable()
    {

    }

    private void OnDestroy()
    {
        Currency.OnCurrencyChanged -= Refresh;
    }
    
    public void Refresh()
    {
        var cur = _currencyRef;
        if (cur == null) 
        {
            return;
        }

        coinText.text  = $"x{cur.Coin}";
        tokenText.text = $"x{cur.Token}";
        keyText.text   = $"x{cur.KeyMap}";
    }
}
