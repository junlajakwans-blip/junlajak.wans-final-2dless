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

        SetPanel(panelStore);

    }


    private void SetPanel(GameObject target)
    {
        // ควบคุม Panel HUD หลัก
        if (panelHUDMain != null)
        {
            // เปิด HUD ถ้า target ไม่ใช่ MainMenu หรือ SelectMap (คือเป็น Gameplay)
            bool isGameplay = target != panelMainMenu && target != panelSelectMap;
            panelHUDMain.SetActive(isGameplay);
        }

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

    public void ShowGameplayHUD()
    {
        // เรียก SetPanel ด้วยค่าที่ไม่ใช่ MainMenu/SelectMap เพื่อเปิด HUD
        // และปิด Menu หลักทั้งหมด
        SetPanel(null); // หรือใช้ Panel_Player_Attack ก็ได้ หากเป็น Panel ที่ Active เฉพาะใน Gameplay
        CloseAllMenus(); // ปิด Pause Menu/Result Menu ที่อาจค้างอยู่
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

    public HealthBarUI GetHealthBarUI() => _healthBarUI;

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

    #endregion

#region  Store UI    
    
    public void ShowStoreExchange()
    {
        ShowStoreMenu(true);

        panelStoreExchange.SetActive(true);
        panelStoreUpgrade.SetActive(false);

        storeTitleText.text = "Store: Exchange";

        // ⬇ สั่งหลัง panel เปิดแล้ว
        if (_storeUI != null && _storesRef != null && _storesRef.Count > 0)
            _storeUI.SwitchStore(StoreType.Exchange);

    }

    public void ShowStoreUpgrade()
    {
        ShowStoreMenu(true);

        panelStoreExchange.SetActive(false);
        panelStoreUpgrade.SetActive(true);

        storeTitleText.text = "Store: Upgrade";

        // ⬇ สั่งหลัง panel เปิดแล้ว
        if (_storeUI != null && _storesRef != null && _storesRef.Count > 0)
            _storeUI.SwitchStore(StoreType.Upgrade);

        
    }

    public void RefreshStoreUI()
    {
        foreach (var ui in topCurrencies)
        ui?.Refresh();
        upgradeUI?.Refresh(); 
    }

    #endregion
}