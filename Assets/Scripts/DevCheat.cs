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
    private Currency _currency;
    private GameManager _gm;

    public GameObject cheatPanel;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Update()
    {
        if (!cheatEnabled) return;

        // รอระบบให้พร้อม — ป้องกัน Error
        if (_gm == null)
            _gm = GameManager.Instance;
        if (_gm == null) return;

        if (_currency == null)
            _currency = _gm.GetCurrency();
        if (_currency == null) return;

        if (_player == null)
            _player = FindFirstObjectByType<Player>();

        // ---------- CHEATS ----------
        if (Input.GetKeyDown(addCoinKey))
        {
            _currency.Coin += 999;
            UIManager.Instance?.UpdateStoreCurrency(_currency.Coin, _currency.Token, _currency.KeyMap);
            FindFirstObjectByType<MapSelectController>()?.RefreshUI();
            Debug.Log("<color=yellow>[CHEAT]</color> Coin +999");
        }

        if (Input.GetKeyDown(addTokenKey))
        {
            _currency.Token += 25;
            UIManager.Instance?.UpdateStoreCurrency(_currency.Coin, _currency.Token, _currency.KeyMap);
            FindFirstObjectByType<MapSelectController>()?.RefreshUI();
            Debug.Log("<color=yellow>[CHEAT]</color> Token +25");
        }

        if (Input.GetKeyDown(addKeyMapKey))
        {
            _currency.KeyMap += 5;
            UIManager.Instance?.UpdateStoreCurrency(_currency.Coin, _currency.Token, _currency.KeyMap);
            FindFirstObjectByType<MapSelectController>()?.RefreshUI();
            Debug.Log("<color=yellow>[CHEAT]</color> Key +5");
        }

        if (Input.GetKeyDown(unlockAllMapsKey))
        {
            var progress = _gm.GetProgressData();
            progress.AddUnlockedMap("School Zone");
            progress.AddUnlockedMap("City Road");
            progress.AddUnlockedMap("Kitchen Mayhem");
            _gm.SaveProgress();
            FindFirstObjectByType<MapSelectController>()?.RefreshUI();
            Debug.Log("<color=cyan>[CHEAT]</color> All Maps Unlocked");
        }

        if (Input.GetKeyDown(godModeKey) && _player != null)
        {
            _player.Heal(99999);
            UIManager.Instance?.UpdateHealth(_player.CurrentHealth);
            Debug.Log("<color=red>[CHEAT]</color> GOD MODE — HP RESTORED");
        }

        if (Input.GetKeyDown(addScoreKey))
        {
            _gm.AddScore(250);
            Debug.Log("<color=yellow>[CHEAT]</color> Score +250");
        }

        // Toggle Cheat Panel
        if (Input.GetKeyDown(KeyCode.Y) && cheatPanel != null)
            cheatPanel.SetActive(!cheatPanel.activeSelf);
    }
}
