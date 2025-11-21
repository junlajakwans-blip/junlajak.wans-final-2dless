using UnityEngine;
using TMPro;

public class TopCurrencyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI tokenText;
    [SerializeField] private TextMeshProUGUI keyText;


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

    private void OnEnable()
    {
        if (_currencyRef != null)
        {
            Currency.OnCurrencyChanged -= Refresh;
            Currency.OnCurrencyChanged += Refresh;
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
        Debug.Log($"[TopCurrencyUI] Refresh → Coin:{_currencyRef?.Coin} Token:{_currencyRef?.Token} Key:{_currencyRef?.KeyMap}");

        if (_currencyRef == null || coinText == null || tokenText == null || keyText == null)
            return;

        coinText.text  = $"x{_currencyRef.Coin}";
        tokenText.text = $"x{_currencyRef.Token}";
        keyText.text   = $"x{_currencyRef.KeyMap}";

        
    }
}
