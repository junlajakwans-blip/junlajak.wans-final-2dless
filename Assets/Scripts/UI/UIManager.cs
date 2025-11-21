using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{

    #region Fields
    [Header("Main UI References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    [SerializeField] private ScoreUI _scoreUI;
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private MenuUI _menuUI;
    [SerializeField] private StoreUI _storeUI;
    [SerializeField] private MapSelectController _mapSelectController;

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
        _storeUI?.SetDependencies(currency, stores, storeManager);

        _mapSelectController?.SetDependencies(gm, currency);

        RefreshStoreUI();
    }

    #endregion  

    private void Awake()
    {
        Instance = this;
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
        panelMainMenu.SetActive(target == panelMainMenu);
        panelSelectMap.SetActive(target == panelSelectMap);
        panelStore.SetActive(target == panelStore || target == panelStoreExchange || target == panelStoreUpgrade);
        panelSettings.SetActive(target == panelSettings);
        
        // หน้าจอร้านค้าย่อย
        panelStoreExchange.SetActive(target == panelStoreExchange);
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
        

    #region Health UI
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