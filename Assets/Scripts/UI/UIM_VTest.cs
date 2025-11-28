/*using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class UIM : MonoBehaviour
{

    #region Fields
    [Header("HUD References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    [SerializeField] private ScoreUI _scoreUI;
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private MenuUI_Main _menuUI; // <--- ใช้ Field นี้โดยตรง
    [SerializeField] private MenuUI_HUD _menuUIHUD;
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

    private T FindInAllChildren<T>(string name) where T : Component
    {
        foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (!canvas.gameObject.scene.isLoaded) continue; // กัน Prefab ที่อยู่นอก Scene

            var trs = canvas.GetComponentsInChildren<T>(true);
            foreach (var t in trs)
                if (t.name == name)
                    return t;
        }
        return null;
    }

    public void AutoBindMainMenuUI()
    {
        panelMainMenu      = FindInAllChildren<Transform>("Panel_MainMenu")?.gameObject;
        panelSelectMap     = FindInAllChildren<Transform>("Panel_SelectMap")?.gameObject;
        panelStore         = FindInAllChildren<Transform>("Panel_Store")?.gameObject;
        panelStoreExchange = FindInAllChildren<Transform>("Panel_Store_Exchange")?.gameObject;
        panelStoreUpgrade  = FindInAllChildren<Transform>("Panel_Store_Upgrade")?.gameObject;
        panelSettings      = FindInAllChildren<Transform>("Panel_Settings")?.gameObject;

        _storeUI = FindInAllChildren<StoreUI>("Panel_Store");
        _mapSelectController = FindInAllChildren<MapSelectController>("Panel_SelectMap");

        // FIX: ควร Bind _menuUI ในนี้ด้วย เผื่อไม่ได้กำหนดใน Inspector
        _menuUI = FindFirstObjectByType<MenuUI_Main>();

        upgradeUI = FindInAllChildren<UpgradeUI>("Icon_MaxUpgrade");

        storeTitleText ??= FindInAllChildren<TextMeshProUGUI>("Text_ShopTitle");

        // Currency bar
        topCurrencies = FindObjectsByType<TopCurrencyUI>(FindObjectsSortMode.None);

        Debug.Log("<color=yellow>[UIManager] AutoBindMainMenuUI completed (deep scan) ✔</color>");
    }

    public void TryBindHUD()
    {

        if (_healthBarUI != null && _scoreUI != null && _cardSlotUI != null && panelHUDMain != null)
        {
            Debug.Log("[UIManager] Skip bind — HUD already bound ✔");
            return;
        }

        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Debug.Log($"[UIManager] Found {canvases.Length} Canvas objects in deep scan.");

        foreach (var canvas in canvases)
        {
            Debug.Log($"[UIManager] Check canvas: {canvas.name}");

            if (canvas.name != "Canvas_HUD") 
                continue;

            _healthBarUI = canvas.GetComponentInChildren<HealthBarUI>(true);
            _scoreUI     = canvas.GetComponentInChildren<ScoreUI>(true);
            _cardSlotUI  = canvas.GetComponentInChildren<CardSlotUI>(true);
            _menuUIHUD = canvas.GetComponentInChildren<MenuUI_HUD>(true);
            panelHUDMain = canvas.transform.Find("Panel_HUD")?.gameObject;

            Debug.Log("<color=cyan>[UIManager] HUD deep scan & bind (component-based) ✔</color>");
            return;
        }

        Debug.LogWarning("[UIManager] HUD deep scan failed (Canvas_HUD not found)");
    }

    
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

        //------------------------------------------
        // Currency UI
        //------------------------------------------
        if (topCurrencies != null)
        {
            foreach (var ui in topCurrencies)
                ui?.SetDependencies(currency);
        }

        //------------------------------------------
        // Store UI
        //------------------------------------------
        if (_storeUI != null)
            _storeUI.SetDependencies(currency, stores, storeManager);

        //------------------------------------------
        // Map Select
        //------------------------------------------
        _mapSelectController?.SetDependencies(gm, currency);

        // =============================
        // HUD / Gameplay UI dependencies
        // =============================
        if (_scoreUI != null)
        {
            _scoreUI.InitializeScore(0); // เริ่มคะแนนใหม่
        }

        if (_healthBarUI != null && gm != null && gm.PlayerRef != null)
        {
            gm.PlayerRef.SetHealthBarUI(_healthBarUI);
        }

        if (_cardSlotUI != null && CardManager.Instance != null)
        {
            _cardSlotUI.SetManager(CardManager.Instance);
        }

        Debug.Log("<color=green>[UIManager] HUD SetDependencies linked successfully ✔</color>");

        //------------------------------------------
        // Refresh visible UI state
        //------------------------------------------
        RefreshStoreUI();
    }

    #endregion  

    //private void Awake()
    //{
        //if (Instance != null && Instance != this)
        //{
        //    Destroy(gameObject); // ทำลายตัวเอง ถ้ามีตัวอื่นอยู่แล้ว
       //     return;
      //  }
     //   Instance = this;
     //   DontDestroyOnLoad(gameObject); 
    //}

    #region Main Menu

public void ShowMainMenu()
    {
        Debug.Log(">> OPEN MAIN MENU");

        // 1. ซ่อน HUD หลัก (Health, Score, etc.) เพื่อไม่ให้บัง
        if (panelHUDMain != null)
        {
            panelHUDMain.SetActive(false);
        }

        // 2. ปิดเมนูที่อาจค้างอยู่ (Result, Pause, Store) จาก MenuUI
        CloseAllMenus();

        // 3. ปิด Card Selection Panel (ถ้าเปิดค้างไว้)
        if (_cardSelectionPanel != null)
        {
            _cardSelectionPanel.SetActive(false);
        }
        
        // 4. แสดงหน้า Main Menu
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
        _scoreUI?.DisplaySavedHighScore(score);
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
        // Gameplay Menu
        _menuUIHUD?.ShowPauseMenu(isActive);
    }

    public void ShowResultMenu()
    {
        // Gameplay Result Panel
        _menuUIHUD?.ShowResultMenu();
    }

    public void CloseAllMenus()
    {
        _menuUI?.CloseAllPanels();   // MainMenu
        _menuUIHUD?.CloseAllPanels(); // Gameplay
    }

    public bool IsAnyMenuOpen()
    {
        return _menuUI != null && _menuUI.IsAnyPanelActive();
    }

    public void ShowGameplayHUD()
    {   
        //Call HUD
        TryBindHUD();

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

        // กัน HUD spawn ช้า (Mobile / WebGL)
        Invoke(nameof(TryBindHUD), 0.1f);
    }
    #endregion

    #region Store UI
    public void OpenStore()
    {
        // ใช้ _menuUI ที่ Bind ไว้แทนการค้นหาซ้ำ
        if (_menuUI == null)
        {
            Debug.LogWarning("[UIManager] ⚠ Cannot open Store — _menuUI reference is missing (Please check Inspector/AutoBind)");
            // สำรอง: ลองค้นหาหนึ่งครั้งเผื่อ AutoBind ยังไม่ทำงาน
            _menuUI = FindFirstObjectByType<MenuUI_Main>();
            if (_menuUI == null) 
            {
                 Debug.LogWarning("[UIManager] ❌ Cannot open Store — MenuUI_Main not found in scene.");
                 return;
            }
        }

        _menuUI.ShowStoreMenu(true);

        if (_storeUI != null && _storesRef != null)
        {
            var firstStore = _storesRef.Find(s => s.StoreType == StoreType.Exchange);
            // ป้องกัน Null Ref ถ้าไม่มี Store Exchange
            if (firstStore != null)
            {
                _storeUI.SetActiveStore(firstStore);
            }
        }

        RefreshStoreUI();
    }

    public void ShowStoreExchange()
    {
        // ใช้ _menuUI ที่ Bind ไว้แทน
        if (_menuUI == null)
        {
            Debug.LogWarning("[UIManager] ⚠ Cannot show Store Exchange — _menuUI reference is missing.");
            return;
        }

        _menuUI.ShowStoreMenu(true);
        _storeUI?.SwitchStore(StoreType.Exchange);
    }

    public void ShowStoreUpgrade()
    {
        // ใช้ _menuUI ที่ Bind ไว้แทน
        if (_menuUI == null)
        {
            Debug.LogWarning("[UIManager] ⚠ Cannot show Store Upgrade — _menuUI reference is missing.");
            return;
        }

        _menuUI.ShowStoreMenu(true);
        _storeUI?.SwitchStore(StoreType.Upgrade);
    }

    public void ShowStoreMap()
    {
        // ใช้ _menuUI ที่ Bind ไว้แทน
        if (_menuUI == null)
        {
            Debug.LogWarning("[UIManager] ⚠ Cannot show Store Map — _menuUI reference is missing.");
            return;
        }

        _menuUI.ShowStoreMenu(true);
        _storeUI?.SwitchStore(StoreType.Map);
    }


    public void RefreshStoreUI()
    {
        if (topCurrencies != null)
            foreach (var ui in topCurrencies)
                ui?.Refresh();

        upgradeUI?.Refresh();
        _storeUI?.RefreshActiveSlots();
    }

    #endregion

}
*/