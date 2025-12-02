using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // 1. Static Instance
    public static UIManager Instance { get; private set; }

    #region Fields (Serialized References)

    [Header("HUD References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    [SerializeField] private ScoreUI _scoreUI;
    [SerializeField] private CardSlotUI _cardSlotUI;
    [SerializeField] private MenuUI _menuUI;
    [SerializeField] public GameObject panelHUDMain;

    [Header("Card Selection")]
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

    // NOTE: These public fields are assumed to be NON-DDoL and must be re-bound.
    public GameObject panelMainMenu;
    public GameObject panelSelectMap;
    public GameObject panelStore;
    public GameObject panelSettings;
    public GameObject panelStoreExchange;
    public GameObject panelStoreUpgrade; 
    public UpgradeUI upgradeUI;

    public void MapNext()  => _mapSelectController?.NextMap();
    public void MapPrev()  => _mapSelectController?.PrevMap();
    public void PlaySelectedMap()  => _mapSelectController?.TryPlaySelectedMap();


    #endregion

    #region Dependencies (Private References for Dependency Injection)

    // References สำหรับ Dependency Injection
    private GameManager _gameManagerRef;
    private Currency _currencyRef;
    private StoreManager _storeManagerRef;
    private List<StoreBase> _storesRef;

    #endregion

    // สถานะเพื่อป้องกันการ Bind ซ้ำสำหรับ Main Menu
    private bool _isMainMenuBound = false;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ทำลายตัวเอง ถ้ามีตัวอื่นอยู่แล้ว
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        //  สมัคร Event Scene Loaded เพื่อ AutoBind
        // FIX: ใช้ชื่อเต็มของ SceneManager ของ Unity เพื่อแก้ไข Conflict
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        //  ยกเลิก Event เมื่อ UIManager ถูกทำลาย
        // FIX: ใช้ชื่อเต็มของ SceneManager ของ Unity เพื่อแก้ไข Conflict
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Scene Event Handling

    // FIX: ใช้ชื่อเต็มของ Type ใน Function Signature เพื่อแก้ไข Conflict
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // ตรวจสอบว่า Scene ที่โหลดคือ Main Menu 
        if (scene.name == "MainMenu")
        {
            // FIX: รีเซ็ตสถานะการ Bound เสมอเมื่อโหลด Main Menu ใหม่ 
            // เนื่องจาก Panels ถูกทำลายและสร้างใหม่เมื่อโหลด Scene 
            _isMainMenuBound = false; 
            
            // เริ่ม Coroutine
            StartCoroutine(DelayBindAndShowMainMenu());
        }
        else if (scene.name != "MainMenu")
        {
            // เมื่อโหลด Scene อื่นที่ไม่ใช่ Main Menu ให้ยกเลิกสถานะ Bound ของ Main Menu 
            _isMainMenuBound = false;
        }
    }

    private IEnumerator DelayBindAndShowMainMenu()
    {
        // ป้องกันการรันซ้ำซ้อนใน Coroutine (กรณีถูกเรียกซ้ำในเฟรมเดียวกัน)
        if (_isMainMenuBound)
        {
             yield break; 
        }

        // รอ 1 เฟรมเพื่อให้แน่ใจว่า UI ได้ถูก Instantiate ลงใน Scene แล้ว
        yield return null; 
        // รออีก 1 เฟรม เผื่อมี Script อื่นที่ต้อง Active/Disable ใน LateUpdate หรือ Animation
        yield return null; 

        // ตรวจสอบสถานะอีกครั้งเพื่อความปลอดภัย (อาจถูก Bind โดย Coroutine อื่นที่เร็วกว่า)
        if (_isMainMenuBound)
        {
             yield break; 
        }

        AutoBindMainMenuUI();
        
        // ตรวจสอบว่า panelMainMenu ถูก Bind สำเร็จแล้วก่อนเรียก ShowMainMenu
        if (panelMainMenu != null)
        {
            ShowMainMenu();
            _isMainMenuBound = true; // ตั้งค่าเป็น Bound แล้วเมื่อสำเร็จ
        }
        else
        {
             Debug.LogError("[UIManager] AutoBind failed to find panelMainMenu. Skipping ShowMainMenu.");
        }
    
    }
    #endregion

    #region Dependencies (Injection and Getters)
    
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
    
    public ScoreUI GetScoreUI() => _scoreUI;
    public HealthBarUI GetPlayerHealthBarUI()
    {
        // คืนค่า HealthBarUI ที่ถูก Serialize ไว้ใน UIManager
        return _healthBarUI; 
    }

    #endregion 

    #region AutoBind Utility (สำหรับ Non-DDoL UI)
    
    /// <summary>
    /// Helper method to log the binding status with a checkmark or error.
    /// </summary>
    private void LogBindStatus(Object obj, string name)
    {
        if (obj != null)
        {
            Debug.Log($"<color=green>[AutoBind] ✔ Found: {name}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[AutoBind] ❌ FAILED: {name} is NULL. Check Hierarchy name or Active status.</color>");
        }
    }

    /// <summary>
    /// [OPTIMIZED] ค้นหา Component/GameObject ที่ถูกปิดอยู่ (Inactive) โดยการวนลูปหา Canvas ใน Scene
    /// </summary>
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
    /// <summary>
    /// Finds and re-assigns Main Menu specific references when the Main Menu scene is loaded again.
    /// It only binds fields that are currently null.
    /// </summary>
    public void AutoBindMainMenuUI()
    {
        // FIX: 1. แก้ไขการเรียกใช้ SceneManager.GetActiveScene() และ GetSceneByName() เพื่อป้องกันความกำกวม
        Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        
        // Fallback/Check: ถ้า Scene Active ไม่ใช่ MainMenu ให้ลองดึงตามชื่อ
        if (!scene.isLoaded || scene.name != "MainMenu")
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainMenu");
            
            if (!scene.isLoaded)
            {
                Debug.LogError("[UIManager] AutoBind failed: MainMenu scene is not loaded or found.");
                return;
            }
        }

        // 2. Binding GameObjects (Panels) โดยใช้ FindInAllChildren<Transform> 
        // NOTE: การใช้ Transform เพื่อค้นหา GameObjects จะน่าเชื่อถือที่สุด
        panelMainMenu      = FindInAllChildren<Transform>("Panel_MainMenu")?.gameObject;
        panelSelectMap     = FindInAllChildren<Transform>("Panel_SelectMap")?.gameObject;
        panelStore         = FindInAllChildren<Transform>("Panel_Store")?.gameObject;
        panelStoreExchange = FindInAllChildren<Transform>("Panel_Store_Exchange")?.gameObject;
        panelStoreUpgrade  = FindInAllChildren<Transform>("Panel_Store_Upgrade")?.gameObject;
        panelSettings      = FindInAllChildren<Transform>("Panel_Settings")?.gameObject;

        // 3. Binding Components Scripts โดยใช้ GameObject ที่หาเจอแล้ว
        // ใช้วิธี GetComponent<T>() แทนการค้นหาซ้ำด้วย FindInSceneRoot ซึ่งเร็วกว่าและปลอดภัยกว่า
        _storeUI           ??= panelStore?.GetComponent<StoreUI>(); 
        _mapSelectController ??= panelSelectMap?.GetComponent<MapSelectController>(); 
        
        // NOTE: MenuUI มักจะอยู่ที่ Root ของ Canvas จึงใช้ FindFirstObjectByType (ซึ่งเร็วกว่า) เป็น Fallback
        if (_menuUI == null) 
        {
            // ใช้ Object.FindFirstObjectByType เพื่อค้นหาเร็วที่สุดและไม่ต้องผูกกับ Scene
            _menuUI = Object.FindFirstObjectByType<MenuUI>(FindObjectsInactive.Include);
        }
        
        // ยังคงใช้ FindInAllChildren สำหรับ UpgradeUI
        upgradeUI = FindInAllChildren<UpgradeUI>("Icon_MaxUpgrade");


        // 4. Binding TopCurrencyUI (ยังคงใช้ FindObjectsByType ทั่วโลก แต่กรอง Scene)
        var list = new List<TopCurrencyUI>();
        // ใช้ Object.FindObjectsByType เพื่อให้ครอบคลุมและง่ายต่อการกรอง Scene
        TopCurrencyUI[] allTopCurrencies = Object.FindObjectsByType<TopCurrencyUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var ui in allTopCurrencies)
        {
            // ตรวจสอบว่าอยู่ใน Scene ที่โหลดอยู่เท่านั้น
            if (ui.gameObject.scene.isLoaded && ui.gameObject.scene.name == scene.name)
            {
                list.Add(ui);
            }
        }
        topCurrencies = list.ToArray();
        
        // --- ADDED DEBUGGING LOGS ---
        Debug.Log("--- AutoBind Status Report (Optimized Logic) ---");
        LogBindStatus(panelMainMenu, "Panel_MainMenu (GameObject)");
        LogBindStatus(panelSelectMap, "Panel_SelectMap (GameObject)");
        LogBindStatus(panelStore, "Panel_Store (GameObject)");
        LogBindStatus(panelSettings, "Panel_Settings (GameObject)");
        LogBindStatus(panelStoreExchange, "Panel_Store_Exchange (GameObject)");
        LogBindStatus(panelStoreUpgrade, "Panel_Store_Upgrade (GameObject)");
        
        LogBindStatus(_storeUI, "StoreUI (Component) [Bound from Panel_Store]");
        LogBindStatus(_mapSelectController, "MapSelectController (Component) [Bound from Panel_SelectMap]");
        LogBindStatus(_menuUI, "MenuUI (Component)");
        LogBindStatus(upgradeUI, "UpgradeUI (Component)");
        // --- END ADDED DEBUGGING LOGS ---


        Debug.Log("<color=yellow>[UIManager] AutoBindMainMenuUI finished ✔ (Captured all MainMenu UI)</color>");

        // Re-link dependencies if ref already exists
        if (_currencyRef != null)
        {
            // Re-link Dependencies to newly found components (Original Logic)
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
    
    // START is removed to prevent double bind (previous fix)
    // private void Start() { } 

    #endregion


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
        
        //  แก้ปัญหาที่ ShowSelectMap() เปิดมั่วเมื่อกลับมา
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


    public void SetPanel(GameObject target)
    {
        // Logic ดั้งเดิมของคุณ (ที่ทำให้ Panel อื่นๆ ปิด/เปิดตามเงื่อนไข Target)
        
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
            ui?.Refresh();        // เงินด้านบน

        upgradeUI?.Refresh();     // ค่า LV / MAX Upgrade

        if (_storeUI != null)
        _storeUI.RefreshActiveSlots();
    }
    #endregion

    #region Menu UI During Gameplay
    public void ShowPauseMenu(bool isActive)
    {
        _menuUI?.ShowPauseMenu(isActive);
    }

    public void ShowResultMenu()
    {
        //  ตรวจสอบ null ก่อนเรียก ShowFinalResult() (อาจเป็นสาเหตุของบั๊ก)
        if (_scoreUI != null)
        {
            _scoreUI.ShowFinalResult();
        }
        _menuUI?.ShowResultMenu();     // เปิด Panel หน้า Result
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
        //  เพิ่มเมธอดควบคุม Panel_Card
        if (_cardSelectionPanel != null)
        {
            _cardSelectionPanel.SetActive(isActive);
            Debug.Log($"[UIManager] Card Selection Panel set to: {isActive}");
        }
        
        //  นำ Time.timeScale กลับมาควบคุมที่นี่ (เพื่อแก้ปัญหาปุ่ม Summon กดไม่ได้)
        Time.timeScale = isActive ? 0f : 1f; 
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

}