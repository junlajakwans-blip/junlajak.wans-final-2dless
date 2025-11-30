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
    // NOTE: Field นี้ถูกกำหนดใน Inspector (Manual)
    [SerializeField] private GameObject _cardSelectionPanel;

    [Header("Main UI References")]
    // NOTE: References in this section may be set in Inspector, but MUST be re-bound if null after scene load.
    [SerializeField] private StoreUI _storeUI;
    [SerializeField] private MapSelectController _mapSelectController;

    [Header("Throwable Interact")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;

    [Header("System References")]
    [SerializeField] private TextMeshProUGUI storeTitleText;
    [SerializeField] private TopCurrencyUI[] topCurrencies;


    
    #endregion

    // NOTE: These public fields are assumed to be NON-DDoL and must be re-bound.
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
    

    #region AutoBind Utility (สำหรับ Non-DDoL UI)
    
    /// <summary>
    /// Helper to find a component in the currently active scene, useful for persistent managers.
    /// </summary>
    private T FindComponentInActiveScene<T>(string name = null) where T : Component
    {
        // FindObjectsByType will search all active objects including newly loaded ones
        T[] objects = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            // Must be in a loaded scene (not a prefab template)
            if (!obj.gameObject.scene.isLoaded) continue;

            if (name == null || obj.name == name)
            {
                return obj;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds and re-assigns Main Menu specific references when the Main Menu scene is loaded again.
    /// It only binds fields that are currently null.
    /// </summary>
    public void AutoBindMainMenuUI()
    {
        // 1. Re-bind Panel GameObjects (Only if currently null)
        if (panelMainMenu == null) panelMainMenu = GameObject.Find("Panel_MainMenu")?.gameObject;
        if (panelSelectMap == null) panelSelectMap = GameObject.Find("Panel_SelectMap")?.gameObject;
        if (panelStore == null) panelStore = GameObject.Find("Panel_Store")?.gameObject;
        if (panelSettings == null) panelSettings = GameObject.Find("Panel_Settings")?.gameObject;
        
        // Panels ย่อยของร้านค้า
        if (panelStoreExchange == null) panelStoreExchange = GameObject.Find("Panel_Store_Exchange")?.gameObject;
        if (panelStoreUpgrade == null) panelStoreUpgrade = GameObject.Find("Panel_Store_Upgrade")?.gameObject;

        // 2. Re-bind Components (Only if currently null)
        if (_storeUI == null) _storeUI = FindComponentInActiveScene<StoreUI>("Panel_Store");
        if (_mapSelectController == null) _mapSelectController = FindComponentInActiveScene<MapSelectController>("Panel_SelectMap");
        if (_menuUI == null) _menuUI = FindComponentInActiveScene<MenuUI>(); 
        if (upgradeUI == null) upgradeUI = FindComponentInActiveScene<UpgradeUI>(); 
        
        // Re-bind currency bar components (Arrays are usually re-assigned entirely as they are scene-specific)
        topCurrencies = FindObjectsByType<TopCurrencyUI>(FindObjectsSortMode.None);
        
        Debug.Log("<color=yellow>[UIManager] AutoBindMainMenuUI completed (Re-linked Main Menu UI) ✔</color>");

        // 3. Re-link Dependencies to newly found components
        if (_currencyRef != null)
        {
             // Pass Dependencies down again to newly found components
            _storeUI?.SetDependencies(_currencyRef, _storesRef, _storeManagerRef);
            _mapSelectController?.SetDependencies(_gameManagerRef, _currencyRef);

            // Re-bind currency dependency for top currencies
            if (topCurrencies != null)
            {
                foreach (var ui in topCurrencies)
                {
                    if (ui == null) continue;
                    ui.SetDependencies(_currencyRef);
                }
            }
        }
    }
    
    #endregion

    #region Scene Event Handling
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        // ตรวจสอบว่า Scene ที่โหลดคือ Main Menu 
        if (scene.name == "MainMenu")
        {
            // Main Menu UI ถูกโหลดใหม่ → ต้อง AutoBind และแสดงหน้าจอหลัก
            AutoBindMainMenuUI();
            ShowMainMenu(); 
        }
        
    }
    #endregion

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

        if (_scoreUI != null)
        {
            _scoreUI.InitializeScore(0); // เริ่มคะแนนใหม่
        }

        if (_healthBarUI != null && gm != null && gm.PlayerRef != null)
        {
            // 1. ส่ง Reference ของ Health Bar ไปให้ Player เพื่อให้ Player ใช้ Update ค่าได้
            gm.PlayerRef.SetHealthBarUI(_healthBarUI);
            
            // 2. ตั้งค่า Health Bar สูงสุดและค่าปัจจุบันตาม Player (MaxHealth = 500)
            InitializeHealth(gm.PlayerRef.MaxHealth); 
            UpdateHealth(gm.PlayerRef.CurrentHealth);
            
            Debug.Log($"[UIManager] Health Bar Initialized with Max HP: {gm.PlayerRef.MaxHealth}");
        }

        if (_cardSlotUI != null && CardManager.Instance != null)
        {
            _cardSlotUI.SetManager(CardManager.Instance);
        }

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
        
        // **สำคัญ:** สมัคร Event Scene Loaded เพื่อ AutoBind
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // **สำคัญ:** ยกเลิก Event เมื่อ UIManager ถูกทำลาย
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

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
        
        // FIX: เพิ่มการตรวจสอบ MenuUI เพื่อป้องกัน Null เมื่อ CloseAllMenus() ถูกเรียก
        if (_menuUI != null)
        {
            // ไม่ต้องทำอะไรเพิ่ม เชื่อว่า CloseAllMenus ทำงานถูกต้อง
        }

        // 3. ปิด Card Selection Panel (ถ้าเปิดค้างไว้)
        if (_cardSelectionPanel != null)
        {
            _cardSelectionPanel.SetActive(false);
        }
        
        // 4. แสดงหน้า Main Menu
        if (panelMainMenu == null)
        {
            Debug.LogError("<color=red>[UIManager] Cannot SHOW MainMenu: panelMainMenu is NULL. AutoBind failed?</color>");
            return;
        }
        
        // 🔥 FIX: แก้ปัญหาที่ ShowSelectMap() เปิดมั่วเมื่อกลับมา
        // ให้ SetPanel สั่งเปิด panelMainMenu เท่านั้น
        SetPanel(panelMainMenu);
    }

    public void ShowSelectMap()
    {
        Debug.Log(">> OPEN SELECT MAP");
        
        // NOTE: เมื่อเรียก SetPanel(panelSelectMap) มันจะสั่งปิด panelMainMenu
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
        // 🔥 Logic ดั้งเดิมของคุณ (ที่ทำให้ Panel อื่นๆ ปิด/เปิดตามเงื่อนไข Target)
        
        //ควบคุม Panel in MainMenu Scene
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
        // 1. สั่งให้ StoreUI สลับเนื้อหา/Store ที่ Active
        if (_storeUI != null)
            _storeUI.SwitchStore(type);

        // 2. เปิด Menu Store หลัก (ซึ่งจะเรียก SetPanel(panelStore))
        ShowStoreMenu(true); 

        // 3. สลับ Panel ย่อยที่ถูกต้อง (ใช้ SetPanel เพื่อเปิด Panel ย่อย และ SetPanel จะจัดการเปิด Panel Store หลักให้เอง)
        if (type == StoreType.Exchange)
            SetPanel(panelStoreExchange);
        else if (type == StoreType.Upgrade)
            SetPanel(panelStoreUpgrade);
    }

    public void ShowStoreBase()
    {
        // ใช้ SetPanel(panelStore) แบบเดิม
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
        
        // 🔥 FIX: นำ Time.timeScale กลับมาควบคุมที่นี่ (เพื่อแก้ปัญหาปุ่ม Summon กดไม่ได้)
        Time.timeScale = isActive ? 0f : 1f; 
    }
    #endregion


    #region Menu UI During Gameplay
    public void ShowPauseMenu(bool isActive)
    {
        _menuUI?.ShowPauseMenu(isActive);
    }

    public void ShowResultMenu()
    {
        // 🔥 FIX: ตรวจสอบ null ก่อนเรียก ShowFinalResult() (อาจเป็นสาเหตุของบั๊ก)
        if (_scoreUI != null)
        {
            _scoreUI.ShowFinalResult();
        }
        _menuUI?.ShowResultMenu();    // เปิด Panel หน้า Result
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
        // var cardPanel = GameObject.Find("Panel_Card"); // <-- ลบโค้ด Find นี้ออก
        // if (cardPanel != null)
        // cardPanel.SetActive(true); // <-- ลบโค้ด SetActive นี้ออก
    }
    #endregion

    #region Store UI
    public void OpenStore()
    {
        // เปิดหน้าร้านรวม (ไม่ใช่ Exchange / Upgrade โดยตรง)
        _menuUI?.ShowStoreMenu(true);

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
        _menuUI?.ShowStoreMenu(true);

        if (_storeUI != null)
            _storeUI.SwitchStore(StoreType.Exchange);
    }

    public void ShowStoreUpgrade()
    {
        _menuUI?.ShowStoreMenu(true);

        if (_storeUI != null)
            _storeUI.SwitchStore(StoreType.Upgrade);
    }

    public void ShowStoreMap()
    {
        _menuUI?.ShowStoreMenu(true);

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