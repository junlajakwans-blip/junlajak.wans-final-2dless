using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private Player _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPointP1;
    [SerializeField] private Transform _spawnPointP2;

    [SerializeField] private HealthBarUI _healthBarPrefab;

    private Transform _anchorP1;
    private Transform _anchorP2;

    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }

    private void Awake()
    {
        // โหลด prefab จาก Resources (ถ้ายังไม่มีใน inspector)
        _healthBarPrefab = Resources.Load<HealthBarUI>("UI/Panel_HealthBar");

        if (_healthBarPrefab == null)
        {
            Debug.LogError("Load prefab failed: Resources/UI/Panel_HealthBar");
            return;
        }

        // หา Canvas และ Anchor สำหรับ UI
        var canvasGO = GameObject.Find("Canvas_HUD");

        if (canvasGO == null)
        {
            Debug.LogError("Canvas_HUD not found");
            return;
        }

        var panel = canvasGO.transform.Find("Panel_HUD");

        _anchorP1 = panel.Find("UI_HealthBar_P1");
        _anchorP2 = panel.Find("UI_HealthBar_P2");

        if (_anchorP1 == null || _anchorP2 == null)
            Debug.LogError("Anchor not found");
    }

    private void Start()
    {
        FindSpawnPoints();
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        if (_spawnPointP1 == null)
        {
            Debug.LogError("P1 spawn missing");
            return;
        }

        Player1 = Instantiate(_playerPrefab, _spawnPointP1.position, Quaternion.identity);
        SetupPlayer(Player1, 1);

        bool isTwoPlayer = false;

        if (GameModeSelector.Instance != null)
        {
            var mode = GameModeSelector.Instance.CurrentMode;
            isTwoPlayer = mode != GameModeManager.GameMode.Solo;
        }

        if (isTwoPlayer)
        {
            if (_spawnPointP2 == null)
            {
                Debug.LogWarning("P2 spawn missing → ใช้ตำแหน่ง P1");
                _spawnPointP2 = _spawnPointP1;
            }

            Player2 = Instantiate(_playerPrefab, _spawnPointP2.position, Quaternion.identity);
            SetupPlayer(Player2, 2);
        }

        Debug.Log($"[PlayerManager] Mode: {GameModeSelector.Instance?.CurrentMode}, TwoPlayer: {isTwoPlayer}");

        var mapGen = FindFirstObjectByType<MapGeneratorBase>();

        if (mapGen != null)
        {
            mapGen.InitializeGenerators(Player1.transform);
            mapGen.InitializePlatformGeneration();

            Debug.Log("[PlayerManager] MapGenerator initialized");
        }
        else
        {
            Debug.LogError("MapGenerator NOT FOUND");
        }
    
    }


    private void FindSpawnPoints()
    {
        var p1 = GameObject.FindGameObjectWithTag("SpawnP1");
        var p2 = GameObject.FindGameObjectWithTag("SpawnP2");

        if (p1 != null) _spawnPointP1 = p1.transform;
        else Debug.LogError("SpawnP1 NOT FOUND");

        if (p2 != null) _spawnPointP2 = p2.transform;
        else Debug.LogError("SpawnP2 NOT FOUND");
    }

    private void SetupPlayer(Player player, int id)
    {
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetPlayerID(id);

        string uiName = $"UI_HealthBar_P{id}";
        Transform anchor = GameObject.Find(uiName)?.transform;

        if (anchor == null)
        {
            Debug.LogError("Anchor NULL");
            return;
        }

        if (_healthBarPrefab == null)
        {
            Debug.LogError("HealthBar Prefab NULL");
            return;
        }

        var ui = Instantiate(_healthBarPrefab, anchor);
        ui.Setup(player);

        Debug.Log($"[PlayerManager] Spawned Player {id}");

        Debug.Log($"Spawned UI for P{id}: " + (ui != null));
    }

    public Player GetAlivePlayer()
    {
        if (Player1 != null && !Player1.IsDead) return Player1;
        if (Player2 != null && !Player2.IsDead) return Player2;

        return null;
    }
}