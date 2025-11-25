using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    #region Fields
    [Header("HUD References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    [SerializeField] private ScoreUI _scoreUI;
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private MenuUI _menuUI;
    [SerializeField] public GameObject panelHUDMain;

    [Header("Card Selection")]
    [SerializeField] private GameObject _cardSelectionPanel;

    [Header("Main UI References")]
    [SerializeField] private StoreUI _storeUI;
    [SerializeField] private MapSelectController _mapSelectController;

    [Header("Throwable Interact")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;

    [Header("System References")]
    [SerializeField] private TextMeshProUGUI storeTitleText;
    [SerializeField] private TopCurrencyUI[] topCurrencies;

    



    
    #endregion

    public GameObject panelMainMenu;
    public GameObject panelSelectMap;
    public GameObject panelStore;
    public GameObject panelSettings;
    public GameObject panelStoreExchange;
    public GameObject panelStoreUpgrade; 
    public UpgradeUI upgradeUI;     
    public static UIManager Instance { get; private set; }



    //  References สำหรับ Dependency Injection
    private GameManager _gameManagerRef;
    private Currency _currencyRef;
    private StoreManager _storeManagerRef;
    private List<StoreBase> _storesRef;
    public ScoreUI GetScoreUI() => _scoreUI;
    

    #region Dependencies
    
    /// <summary>
    /// Injects runtime dependencies from the main Initializer (GameManager).
    /// </summary>
    public void SetDependencies(GameManager gm, Currency currency, StoreManager storeManager, List<StoreBase> stores)
    {
        _gameManagerRef = gm;
        _currencyRef = currency;
        _storeManagerRef = storeManager;
        _storesRef = stores;
        
        // Register Currency for every TopCurrency UI
        if (topCurrencies != null)
        {
            foreach (var ui in topCurrencies)
            {
                if (ui == null) continue;
                ui.SetDependencies(currency);
                Debug.Log("[UIManager] TopCurrencyUI registered");
            }
        }

        // Register Store UI
        if (_storeUI != null)
        _storeUI.SetDependencies(currency, stores, storeManager);

        _mapSelectController?.SetDependencies(gm, currency);

        RefreshStoreUI();
    }

    #endregion  

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ทำลายตัวเอง ถ้ามีตัวอื่นอยู่แล้ว
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    #region Main Menu

    public void ShowMainMenu()
    {
        Debug.Log(">> OPEN MAIN MENU");
        if (panelHUDMain != null)
        {
            panelHUDMain.SetActive(false);
        }
        SetPanel(panelMainMenu);
    }

    public void ShowSelectMap()
    {
        Debug.Log(">> OPEN SELECT MAP");
        SetPanel(panelSelectMap);
    }

    public void ShowStoreMenu(bool isActive)
    {
        _menuUI?.ShowStoreMenu(isActive);
        if (!isActive) return;

        SetPanel(panelStore); // เปิด panel จริง
        RefreshStoreUI();     // <— REFRESH ทุกครั้ง
    }


    private void SetPanel(GameObject target)
    {
        //ควบคุม Panel in MainMenu Scene
        if (panelMainMenu != null)
        if (panelMainMenu != null)
            panelMainMenu.SetActive(target == panelMainMenu);
            
        if (panelSelectMap != null)
            panelSelectMap.SetActive(target == panelSelectMap);
            
        if (panelStore != null)
            panelStore.SetActive(target == panelStore || target == panelStoreExchange || target == panelStoreUpgrade);
            
        if (panelSettings != null)
            panelSettings.SetActive(target == panelSettings);
        
        // หน้าจอร้านค้าย่อย
        if (panelStoreExchange != null)
            panelStoreExchange.SetActive(target == panelStoreExchange);
            
        if (panelStoreUpgrade != null)
            panelStoreUpgrade.SetActive(target == panelStoreUpgrade);

        // รีค่าเงิน
        foreach (var ui in topCurrencies)
        ui?.Refresh();
    }

    public void SwitchStorePanel(StoreType type)
    {
        if (type == StoreType.Exchange)
            SetPanel(panelStoreExchange);
        else if (type == StoreType.Upgrade)
            SetPanel(panelStoreUpgrade);
    }

    public void ShowStoreBase()
    {
        SetPanel(panelStore);
    }
    #endregion
        
    #region Throwable Interact UI
        public void ShowPrompt(string message)
        {
            if (promptUI == null || promptText == null) return;

            promptText.text = message;
            promptUI.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptUI == null) return;
            promptUI.SetActive(false);
        }
    #endregion

    #region Health UI

    /// <summary>
    /// Provides the persistent HealthBarUI component reference.
    /// </summary>
    public HealthBarUI GetPlayerHealthBarUI()
    {
        // คืนค่า HealthBarUI ที่ถูก Serialize ไว้ใน UIManager
        return _healthBarUI; 
    }

    public void InitializeHealth(int maxHP)
    {
        _healthBarUI?.InitializeHealth(maxHP);
    }

    public void UpdateHealth(int currentHP)
    {
        _healthBarUI?.UpdateHealth(currentHP);
    }

    public void ShowDamageEffect()
    {
        _healthBarUI?.AnimateDamageEffect();
    }

    public void ShowHealEffect()
    {
        _healthBarUI?.AnimateHealEffect();
    }
    
    #endregion

    #region Score UI
    public void InitializeScore(int startScore)
    {
        _scoreUI?.InitializeScore(startScore);
    }

    public void UpdateScore(int newScore)
    {
        _scoreUI?.UpdateScore(newScore);
    }

    public void ShowComboEffect(int combo)
    {
        _scoreUI?.ShowComboEffect(combo);
    }

    public void DisplayHighScore(int score)
    {
        _scoreUI?.DisplayHighScore(score);
    }
    #endregion

    #region Card UI
    public void UpdateCardSlots(System.Collections.Generic.List<Card> cards)
    {
        _cardSlotUI?.UpdateSlots(cards);
    }

    public void HighlightCard(int index)
    {
        _cardSlotUI?.HighlightSlot(index);
    }

    public void ResetCardSlots()
    {
        _cardSlotUI?.ResetAllSlots();
    }

    /// <summary> Shows/Hides the Card Selection/Starter Panel. </summary>
    public void ShowCardSelectionPanel(bool isActive)
    {
        //  เพิ่มเมธอดควบคุม Panel_Card
        if (_cardSelectionPanel != null)
        {
            _cardSelectionPanel.SetActive(isActive);
            Debug.Log($"[UIManager] Card Selection Panel set to: {isActive}");
        }
        Time.timeScale = isActive ? 0f : 1f;
    }
    #endregion


    #region Menu UI During Gameplay
    public void ShowPauseMenu(bool isActive)
    {
        _menuUI?.ShowPauseMenu(isActive);
    }

    public void ShowResultMenu(int score, int coins)
    {
        _menuUI?.ShowResultMenu(score, coins);
    }

    public void CloseAllMenus()
    {
        _menuUI?.CloseAllPanels();
    }

    public bool IsAnyMenuOpen()
    {
        return _menuUI != null && _menuUI.IsAnyPanelActive();
    }

    public void ShowGameplayHUD()
    {
        // 1. เปิด HUD หลัก
        if (panelHUDMain != null)
            panelHUDMain.SetActive(true);
        
        // 2. ปิด Menu ที่อาจค้างอยู่ (Pause/Result)
        CloseAllMenus(); 

        // FIX 1: ปิด Panels ทั้งหมดของ Main Menu ที่เป็น Persistent/DDoL
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelSelectMap != null) panelSelectMap.SetActive(false);
        
        // FIX 2: ปิด STORE CONTAINERS ทั้งหมด (นี่คือตัวที่บังหน้าจอ)
        if (panelStore != null) panelStore.SetActive(false);
        if (panelSettings != null) panelSettings.SetActive(false);
        if (panelStoreExchange != null) panelStoreExchange.SetActive(false);
        if (panelStoreUpgrade != null) panelStoreUpgrade.SetActive(false);
        
        if (_cardSelectionPanel != null) _cardSelectionPanel.SetActive(true);
        var cardPanel = GameObject.Find("Panel_Card");
        if (cardPanel != null)
            cardPanel.SetActive(true);
    }
    #endregion

    #region Store UI
    public void OpenStore()
    {
        // เปิดหน้าร้านรวม (ไม่ใช่ Exchange / Upgrade โดยตรง)
        MenuUI.Instance.ShowStoreMenu(true);

        if (_storeUI != null && _storesRef != null)
        {
            // ร้านแรกที่จะแสดงค่าเริ่มต้น (Exchange)
            var firstStore = _storesRef.Find(s => s.StoreType == StoreType.Exchange);
            _storeUI.SetActiveStore(firstStore);
        }

        RefreshStoreUI();
    }

    public void ShowStoreExchange()
    {
        MenuUI.Instance.ShowStoreMenu(true);

        if (_storeUI != null)
            _storeUI.SwitchStore(StoreType.Exchange);
    }

    public void ShowStoreUpgrade()
    {
        MenuUI.Instance.ShowStoreMenu(true);

        if (_storeUI != null)
            _storeUI.SwitchStore(StoreType.Upgrade);
    }

    public void ShowStoreMap()
    {
        MenuUI.Instance.ShowStoreMenu(true);

        if (_storeUI != null)
            _storeUI.SwitchStore(StoreType.Map);
    }

    public void RefreshStoreUI()
    {
        foreach (var ui in topCurrencies)
            ui?.Refresh();          // เงินด้านบน

        upgradeUI?.Refresh();       // ค่า LV / MAX Upgrade

        if (_storeUI != null)
        _storeUI.RefreshActiveSlots();
    }
    #endregion

}