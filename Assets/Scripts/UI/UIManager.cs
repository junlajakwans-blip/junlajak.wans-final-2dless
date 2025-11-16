using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);


    }
    #endregion

    private void OnEnable()
        {
            GameManager.OnCurrencyReady += HandleStoreReady;
        }

        private void OnDisable()
        {
            GameManager.OnCurrencyReady -= HandleStoreReady;
        }

    private void HandleStoreReady()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var stores = gm.GetStoreList();
        var storeManager = gm.GetStoreManager();
        

        if (stores != null && storeManager != null)
            InitializeStore(stores, storeManager);
    }



    #region Fields
    [Header("Main UI References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    [SerializeField] private ScoreUI _scoreUI;
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private MenuUI _menuUI;
    [SerializeField] private StoreUI _storeUI;

    [Header("System References")]
    [SerializeField] private StoreManager _storeManager;
    [SerializeField] private TextMeshProUGUI storeTitleText;

    
    #endregion

    public GameObject panelMainMenu;
    public GameObject panelSelectMap;
    public GameObject panelStore;
    public GameObject panelSettings;
    public GameObject panelStoreExchange;
    public GameObject panelStoreUpgrade; 
    public TopCurrencyUI currencyUI;   
    public UpgradeUI upgradeUI;     



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

        var gm = GameManager.Instance;
        if (gm == null) return;

        var stores = gm.GetStoreList();
        var storeManager = gm.GetStoreManager();

        if (stores != null && storeManager != null)
        {
            _storeUI.InitializeStore(storeManager, stores); // <<< สำคัญ
            _storeUI.SwitchStore(StoreType.Exchange);       // <<< เปิดร้านเริ่มต้นที่ Exchange
        }

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
        ShowStoreBase(); 
        
        panelStoreExchange.SetActive(true);
        panelStoreUpgrade.SetActive(false);

        storeTitleText.text = "Store: Exchange";
        _storeUI.SwitchStore(StoreType.Exchange);
        currencyUI.Refresh();
    }

    public void ShowStoreUpgrade()
    {
        ShowStoreBase(); 

        panelStoreExchange.SetActive(false);
        panelStoreUpgrade.SetActive(true);

        storeTitleText.text = "Store: Upgrade";
        _storeUI.SwitchStore(StoreType.Upgrade);
        currencyUI.Refresh();
    }

    public void InitializeStore(List<StoreBase> stores, StoreManager manager)
    {
            if (_storeUI != null)
                _storeUI.InitializeStore(manager, stores);
    }


    public void UpdateStoreCurrency(int coins, int tokens, int keys)
     {
            _storeUI?.RefreshCurrency(coins, tokens, keys);
    }


    public void RefreshStoreUI()
    {
        currencyUI?.Refresh();
        upgradeUI?.Refresh();    
    }

    #endregion
}