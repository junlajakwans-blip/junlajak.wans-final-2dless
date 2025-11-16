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

    private Player _player;
    private GameManager _gm;

    public GameObject cheatPanel;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Update()
    {
        if (!cheatEnabled) return;

        // รอ GameManager พร้อม
        if (_gm == null)
            _gm = GameManager.Instance;
        if (_gm == null) return;

        // รอ Currency พร้อม
        Currency currency = _gm.GetCurrency();
        if (currency == null) return;

        // Player อาจยังไม่มีจนกว่าเข้าสู่เกม
        if (_player == null)
            _player = FindFirstObjectByType<Player>();

        // ============= CHEATS =============
        if (Input.GetKeyDown(addCoinKey))
        {
            currency.Coin += 999;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +999 Coin");
        }

        if (Input.GetKeyDown(addTokenKey))
        {
            currency.Token += 25;
            RefreshCurrencyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +25 Token");
        }

        if (Input.GetKeyDown(addKeyMapKey))
        {
            currency.KeyMap += 5;
            RefreshCurrencyUI();
            FindAnyObjectByType<MapSelectController>()?.RefreshKeyUI();
            Debug.Log("<color=yellow>[CHEAT]</color> +5 Keys");
        }

        if (Input.GetKeyDown(unlockAllMapsKey))
        {
            var p = _gm.GetProgressData();
            p.AddUnlockedMap("School Zone");
            p.AddUnlockedMap("City Road");
            p.AddUnlockedMap("Kitchen Mayhem");
            _gm.SaveProgress();
            Debug.Log("<color=cyan>[CHEAT]</color> All Maps Unlocked");
        }

        if (Input.GetKeyDown(godModeKey) && _player != null)
        {
            _player.Heal(99999);
            RefreshHealthUI();
            Debug.Log("<color=red>[CHEAT] GOD MODE — HP RESTORED</color>");
        }

        if (Input.GetKeyDown(addScoreKey))
        {
            _gm.AddScore(250);
            Debug.Log("<color=yellow>[CHEAT]</color> +250 Score");
        }

        // Toggle Cheat Panel
        if (Input.GetKeyDown(KeyCode.Y) && cheatPanel != null)
            cheatPanel.SetActive(!cheatPanel.activeSelf);
    }

    // ==================== UI Helper ====================
    private void RefreshCurrencyUI()
    {
        // อัปเดต UI ร้านค้า
        FindAnyObjectByType<StoreUI>()?.RefreshCurrency();

        // อัปเดต UI เลือกแมพ ถ้าหน้า Select Map เปิดอยู่
        FindAnyObjectByType<MapSelectController>()?.RefreshKeyUI();
    }

    private void RefreshHealthUI()
    {
        // ถ้ามี Health UI ใน Scene
        UIManager.Instance?.UpdateHealth(_player.CurrentHealth);
    }
}
