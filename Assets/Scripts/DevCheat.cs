using UnityEngine;

public class DevCheat : MonoBehaviour
{
    [Header("Cheat Mode")]
    public bool cheatEnabled = true;

    [Header("Cheat Keys")]
    public KeyCode addCoinKey = KeyCode.F1;
    public KeyCode addTokenKey = KeyCode.F2;
    public KeyCode addKeyMapKey = KeyCode.F3;
    public KeyCode unlockAllMapsKey = KeyCode.F4;
    public KeyCode godModeKey = KeyCode.F5;
    public KeyCode addScoreKey = KeyCode.F6;
    public KeyCode resetSaveKey = KeyCode.F7;
    public KeyCode addRandomCardKey = KeyCode.F8;




 // Injected Dependencies
    private Player _playerRef;
    private GameManager _gmRef;
    private Currency _currencyRef;
    private MapSelectController _mapSelectControllerRef; // For RefreshKeyUI
    private StoreUI _storeUIRef;                        // For RefreshCurrency
    private UIManager _uiManagerRef;                    // For RefreshHealthUI

    public GameObject cheatPanel;

    private void Awake()
    {
        // NOTE: DontDestroyOnLoad ควรถูกจัดการโดย GameManager (ถ้า DevCheat เป็นลูก) 
        // หรือคงไว้ถ้าเป็น Root Object อิสระ
        DontDestroyOnLoad(this.gameObject);
    }
    
    #region Dependencies
    /// <summary>
    /// Injects all necessary runtime systems. Called by GameManager.
    /// </summary>
    public void InitializeCheat(GameManager gm, Player player, Currency currency, MapSelectController mapSelect, StoreUI storeUI, UIManager uiManager)
    {
        _gmRef = gm;
        _playerRef = player;
        _currencyRef = currency;
        _mapSelectControllerRef = mapSelect;
        _storeUIRef = storeUI;
        _uiManagerRef = uiManager;
        
        Debug.Log("[DevCheat] Dependencies initialized.");
    }
    #endregion

    private void Update()
    {
        if (!cheatEnabled) return;

        if (_storeUIRef == null)
        _storeUIRef = FindFirstObjectByType<StoreUI>();

    if (_mapSelectControllerRef == null)
        _mapSelectControllerRef = FindFirstObjectByType<MapSelectController>();


        // FIX 1: ใช้ References ที่ถูก Inject ทันที
        if (_gmRef == null || _currencyRef == null) return;
        
        // ============= CHEATS =============
        
        // F1: Add Coin
        if (Input.GetKeyDown(addCoinKey))
        {
            _currencyRef.Coin += 999;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +999 Coin");
        }

        // F2: Add Token
        if (Input.GetKeyDown(addTokenKey))
        {
            _currencyRef.Token += 25;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +25 Token");
        }

        // F3: Add Key Map
        if (Input.GetKeyDown(addKeyMapKey))
        {
            _currencyRef.KeyMap += 5;
            RefreshCurrencyUI(); 
            Debug.Log("<color=yellow>[CHEAT]</color> +5 Keys");
        }

        // F4: Unlock All Maps
        if (Input.GetKeyDown(unlockAllMapsKey))
        {
            var p = _gmRef.GetProgressData();
            // การ AddUnlockedMap ใช้วิธีเดิม
            p.AddUnlockedMap("School Zone");
            p.AddUnlockedMap("City Road");
            p.AddUnlockedMap("Kitchen Mayhem");
            _gmRef.SaveProgress();
            Debug.Log("<color=cyan>[CHEAT]</color> All Maps Unlocked");
        }

        // F5: God Mode
        if (Input.GetKeyDown(godModeKey) && _playerRef != null) // ✅ FIX 3: ใช้ _playerRef
        {
            _playerRef.Heal(99999);
            RefreshHealthUI();
            Debug.Log("<color=red>[CHEAT] GOD MODE — HP RESTORED</color>");
        }

        // F6: Add Score
        if (Input.GetKeyDown(addScoreKey))
        {
            _gmRef.AddScore(250);
            Debug.Log("<color=yellow>[CHEAT]</color> +250 Score");
        }

        // F7: Reset Save Data (New Functionality)
        if (Input.GetKeyDown(resetSaveKey))
        {
            // ใช้ GameManager เพื่อเรียก SaveSystem.ResetData()
            _gmRef.ResetGameProgress(); 
            Debug.Log("<color=red>[CHEAT] ALL SAVE DATA HAS BEEN RESET!</color>");
        }

        // Toggle Cheat Panel
        if (Input.GetKeyDown(KeyCode.Y) && cheatPanel != null)
            cheatPanel.SetActive(!cheatPanel.activeSelf);

        
        // F8 → Force Drop Career Card (Only if CardManager & CareerDatabase are ready)
        if (Input.GetKeyDown(KeyCode.F8))
        {
            var cm = FindFirstObjectByType<CardManager>();
            if (cm == null)
            {
                Debug.LogError("[CHEAT] ❌ CardManager not found — cannot spawn Career Card");
                return;
            }

            if (!cm.IsReady) // ต้องมี property นี้ใน CardManager
            {
                Debug.LogError("[CHEAT] ❌ CardManager not ready — database not initialized yet");
                return;
            }

            cm.AddCareerCard();
            Debug.Log("<color=lime>[CHEAT]</color> Forced drop 1 Career Card");
        }


        // Reset All
        if (Input.GetKeyDown(KeyCode.F9))
        {
            _gmRef.DeleteSaveAndRestart();
            Debug.Log("<color=red>[CHEAT] DELETE SAVE + Restart Game</color>");
        }

    }


    

    // ==================== UI Helper ====================
    private void RefreshCurrencyUI()
    {
        // ใช้ References ที่ถูก Inject แทน FindAnyObjectByType
        _storeUIRef?.RefreshCurrency();
    }

    private void RefreshHealthUI()
    {
        //ใช้ _uiManagerRef ที่ถูก Inject แทน UIManager.Instance
        if (_uiManagerRef != null && _playerRef != null)
            _uiManagerRef.UpdateHealth(_playerRef.CurrentHealth);
    }
}