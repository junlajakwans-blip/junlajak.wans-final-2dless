using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class RandomStarterCard : MonoBehaviour
{
    #region References
    private CardManager _cardManagerRef;
    private GameManager _gmRef;
    private bool _alreadyClaimed = false;
    private static bool _globalSummonLock = false; // prevents duplicate listeners from consuming extra tokens
    #endregion

    [Header("UI Components")]
    [SerializeField] private Button buttonSummon;
    [SerializeField] private Button buttonCancel;
    [SerializeField] private TextMeshProUGUI textTokenDisplay;
    [SerializeField] private GameObject panelRandomCard; // Panel ที่เปิด/ปิด
    private bool _lockInput = false;

    private void Start()
    {
        // Bind UI events
        if (buttonSummon != null)
        {
            buttonSummon.onClick.RemoveListener(OnClickSummon);
            buttonSummon.onClick.AddListener(OnClickSummon);
        }

        if (buttonCancel != null)
        {
            buttonCancel.onClick.RemoveListener(ClosePanel);
            buttonCancel.onClick.AddListener(ClosePanel);
        }
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
        buttonSummon.interactable = false;
        Currency.OnCurrencyChanged += UpdateTokenText;
        GameManager.OnCurrencyReady += EnableStarterUI;
        if (_gmRef != null && _gmRef.GetCurrency() != null)
            EnableStarterUI();
        }

    private void OnDisable()
    {
        Currency.OnCurrencyChanged -= UpdateTokenText;
        GameManager.OnCurrencyReady -= EnableStarterUI;
    }

    private void EnableStarterUI()
    {
        UpdateTokenText();
        buttonSummon.interactable = true;
    }

    /// <summary>
    /// เปิด Panel และอัปเดตจำนวน Token
    /// </summary>
    public void OpenPanel()
    {
        if (panelRandomCard != null)
            panelRandomCard.SetActive(true);

        // inject ให้ตรงนี้ทุกครั้งที่เปิด panel
        _gmRef = GameManager.Instance;
        _cardManagerRef = GameManager.Instance?.CardManager;

        if (_cardManagerRef == null)
            Debug.LogError("[StarterCard] CardManager NOT FOUND during OpenPanel()");

        Time.timeScale = 0f;
        UpdateTokenText();
        EnableStarterUI();
    }



    public void ClosePanel()
    {
        if (panelRandomCard != null)
            panelRandomCard.SetActive(false);

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
        Debug.Log($"[Check Duplicate] Called by Object: {gameObject.name}, Script Instance ID: {this.GetInstanceID()}");
        if (_lockInput || _globalSummonLock) return;
        _lockInput = true;
        _globalSummonLock = true;

        // ปิด event ของปุ่มทันที กัน double click 
        buttonSummon.enabled = false;

        bool success = TrySummonCard();

        if (success)
        {
            _alreadyClaimed = true;
            ClosePanel();
            UpdateTokenText();
            _globalSummonLock = false;
        }
        else
        {
            // restore ปุ่มเฉพาะตอน fail
            buttonSummon.enabled = true;
            buttonSummon.interactable = true;
            _lockInput = false;
            _globalSummonLock = false;
        }
    }


    private IEnumerator CloseNextFrame()
    {
        yield return null; // รอ 1 frame ให้ Unity ประมวลผล onClick เสร็จ
        ClosePanel();
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

        if (_gmRef == null)
            _gmRef = GameManager.Instance;

        if (_gmRef == null)
        {
            Debug.LogError("[StarterCard] GameManager missing.");
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
            _cardManagerRef = FindFirstObjectByType<CardManager>();
            if (_cardManagerRef == null)
            {
                Debug.LogError("[StarterCard] CardManager missing — even after FindFirstObjectByType.");
                return false;
            }
        }


        _cardManagerRef.AddStarterCard(); // สุ่มเฉพาะ Career ถูกต้อง

        _alreadyClaimed = true;

        Debug.Log("<color=yellow>[StarterCard]</color> Summon Successful!");
        return true;
    }

    public void ResetForNewGame()
    {
        _alreadyClaimed = false;
        if (buttonSummon != null) buttonSummon.interactable = true;
        _lockInput = false;

    }


}

