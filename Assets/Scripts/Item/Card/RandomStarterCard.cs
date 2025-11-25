using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RandomStarterCard : MonoBehaviour
{
    #region References
    private CardManager _cardManagerRef;
    private GameManager _gmRef;
    private bool _alreadyClaimed = false;
    #endregion

    [Header("UI Components")]
    [SerializeField] private Button buttonSummon;
    [SerializeField] private Button buttonCancel;
    [SerializeField] private TextMeshProUGUI textTokenDisplay;
    [SerializeField] private GameObject panelRandomCard; // Panel ที่เปิด/ปิด

    private void Start()
    {
        // Bind UI events
        if (buttonSummon != null)
            buttonSummon.onClick.AddListener(OnClickSummon);

        if (buttonCancel != null)
            buttonCancel.onClick.AddListener(ClosePanel);
    }

    // Inject Reference
    public void SetDependencies(CardManager manager, GameManager gm)
    {
        _cardManagerRef = manager;
        _gmRef = gm;
         Debug.Log($"[StarterCard] SetDependencies called — GM = {gm != null}, CM = {manager != null}");

    }

    private void OnEnable()
    {
        Currency.OnCurrencyChanged += UpdateTokenText;
        UpdateTokenText();
    }
    private void OnDisable()
    {
        Currency.OnCurrencyChanged -= UpdateTokenText;
    }

    /// <summary>
    /// เปิด Panel และอัปเดตจำนวน Token
    /// </summary>
    public void OpenPanel()
    {
        if (panelRandomCard != null)
            panelRandomCard.SetActive(true);

        Time.timeScale = 0f;
        UpdateTokenText();
    }

    public void ClosePanel()
    {
        if (panelRandomCard != null)
            panelRandomCard.SetActive(false);

        UIManager.Instance.ShowCardSelectionPanel(false);
        Time.timeScale = 1f;
    }

    private void UpdateTokenText()
    {
        if (textTokenDisplay == null) return;

        var currency = _gmRef?.GetCurrency();
        textTokenDisplay.text = currency != null
            ? currency.Token.ToString()
            : "0";
    }

    private void OnClickSummon()
    {
        if (TrySummonCard())
        {
            UpdateTokenText();   // ลด Token แล้วต้อง refresh UI
            ClosePanel();

            UIManager.Instance.ShowCardSelectionPanel(false);
            Time.timeScale = 1f;
        }
    }
    

    /// <summary>
    /// ใช้ Token 1 → สุ่มการ์ด Career 1 ใบ → ให้แค่ 1 ครั้ง/รอบ
    /// </summary>
    public bool TrySummonCard()
    {
        if (_alreadyClaimed)
        {
            Debug.Log("<color=orange>[StarterCard]</color> Already summoned this round.");
            return false;
        }

        var currency = _gmRef?.GetCurrency();
        if (currency == null)
        {
            Debug.LogError("[StarterCard] Currency missing or GameManager not initialized.");
            return false;
        }

        if (!currency.UseToken(1))
        {
            Debug.Log("<color=red>[StarterCard]</color> Not enough Token. (Need 1)");
            return false;
        }

        if (_cardManagerRef == null)
        {
            Debug.LogError("[StarterCard] CardManager missing.");
            return false;
        }

        _cardManagerRef.AddStarterCard(); // สุ่มเฉพาะ Career ถูกต้อง

        _alreadyClaimed = true;
        Debug.Log("<color=yellow>[StarterCard]</color> Summon Successful!");
        return true;
    }

    public void ResetForNewGame()
    {
        _alreadyClaimed = false;
    }
}
