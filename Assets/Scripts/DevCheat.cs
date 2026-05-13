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

    private Player _playerRef;
    private GameManager _gmRef;
    private Currency _currencyRef;
    private MapSelectController _mapSelectControllerRef;
    private StoreUI _storeUIRef;
    private UIManager _uiManagerRef;

    public GameObject cheatPanel;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// Inject by GameManager
    /// </summary>
    public void InitializeCheat(GameManager gm, Player player, Currency currency,
                                MapSelectController mapSelect, StoreUI storeUI, UIManager uiManager)
    {
        _gmRef = gm;
        _playerRef = player;
        _currencyRef = currency;
        _mapSelectControllerRef = mapSelect;
        _storeUIRef = storeUI;
        _uiManagerRef = uiManager;

        Debug.Log("[DevCheat] Dependencies initialized.");
    }

    private void Update()
    {
        if (!cheatEnabled) return;
        if (_gmRef == null || _currencyRef == null) return; // ป้องกันตอนเกมยังโหลดไม่เสร็จ

        // เติมระบบ UI หากโหลด Scene ใหม่
        if (_storeUIRef == null) _storeUIRef = FindFirstObjectByType<StoreUI>();
        if (_mapSelectControllerRef == null) _mapSelectControllerRef = FindFirstObjectByType<MapSelectController>();

        // ----- CHEATS -----

        if (Input.GetKeyDown(addCoinKey))
        {
            _currencyRef.Coin += 999;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +999 Coin");
        }

        if (Input.GetKeyDown(addTokenKey))
        {
            _currencyRef.Token += 25;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +25 Token");
        }

        if (Input.GetKeyDown(addKeyMapKey))
        {
            _currencyRef.KeyMap += 5;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +5 Keys");
        }

        if (Input.GetKeyDown(unlockAllMapsKey))
        {
            var p = _gmRef.GetProgressData();
            p.AddUnlockedMap("School Zone");
            p.AddUnlockedMap("City Road");
            p.AddUnlockedMap("Kitchen Mayhem");
            _gmRef.SaveProgress();
            Debug.Log("<color=cyan>[CHEAT]</color> All Maps Unlocked");
        }

        if (Input.GetKeyDown(godModeKey) && _playerRef != null)
        {
            _playerRef.Heal(99999);
            RefreshHealthUI();
            Debug.Log("<color=red>[CHEAT] GOD MODE — HP RESTORED</color>");
        }

        if (Input.GetKeyDown(addScoreKey))
        {
            Debug.Log("[DevCheat] F6 pressed (addScoreKey)");

            // Try preferred route: PlayerManager -> Player1
            var pm = FindFirstObjectByType<PlayerManager>();
            if (pm != null && pm.Player1 != null)
            {
                pm.Player1.AddScore(250);
                Debug.Log("<color=yellow>[CHEAT]</color> +250 Score to Player1 (via PlayerManager)");
                // Ensure UI reflects the change immediately
                if (UIManager.Instance != null)
                {
                    var p1Score = pm.Player1 != null && pm.Player1.Data != null ? pm.Player1.Data.Score : 0;
                    var p2Score = (pm.Player2 != null && pm.Player2.Data != null) ? pm.Player2.Data.Score : 0;
                    UIManager.Instance.UpdateCompetitionScores(p1Score, p2Score);
                }
                return;
            }

            // Fallback: try any Player in scene
            var anyPlayer = FindFirstObjectByType<Player>();
            if (anyPlayer != null)
            {
                anyPlayer.AddScore(250);
                Debug.Log("<color=yellow>[CHEAT]</color> +250 Score to first Player found");
                if (UIManager.Instance != null)
                {
                    // Try to update per-player UI if available, otherwise force central score refresh
                    var pmInst = PlayerManager.Instance;
                    if (pmInst != null)
                    {
                        var p1 = pmInst.Player1 != null && pmInst.Player1.Data != null ? pmInst.Player1.Data.Score : 0;
                        var p2 = pmInst.Player2 != null && pmInst.Player2.Data != null ? pmInst.Player2.Data.Score : 0;
                        UIManager.Instance.UpdateCompetitionScores(p1, p2);
                    }
                    else
                    {
                        UIManager.Instance.UpdateScore(_gmRef != null ? _gmRef.Score : GameManager.Instance?.Score ?? 0);
                    }
                }
                return;
            }

            // Last resort: global GameManager (may only update central UI)
            if (_gmRef != null)
            {
                _gmRef.AddScore(250);
                Debug.Log("<color=yellow>[CHEAT]</color> +250 Score (global GameManager fallback)");
                // Force central UI refresh as last resort
                if (UIManager.Instance != null)
                    UIManager.Instance.UpdateScore(_gmRef.Score);
            }
        }

        if (Input.GetKeyDown(resetSaveKey))
        {
            _gmRef.ResetGameProgress();
            Debug.Log("<color=red>[CHEAT] ALL SAVE DATA RESET</color>");
        }

        if (Input.GetKeyDown(KeyCode.Y) && cheatPanel != null)
            cheatPanel.SetActive(!cheatPanel.activeSelf);

        if (Input.GetKeyDown(addRandomCardKey))
        {
            var cm = FindFirstObjectByType<CardManager>();
            if (cm != null) cm.AddCareerCard();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            _gmRef.DeleteSaveAndRestart();
            Debug.Log("<color=red>[CHEAT] DELETE SAVE + RESTART</color>");
        }
    }

    private void RefreshCurrencyUI() => _storeUIRef?.RefreshCurrency();
    private void RefreshHealthUI() => _uiManagerRef?.UpdateHealth(_playerRef.CurrentHealth);
}
