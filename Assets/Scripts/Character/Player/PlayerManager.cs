using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private Player _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPointP1;
    [SerializeField] private Transform _spawnPointP2;

    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }

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
       var cam = Camera.main.GetComponent<CameraFollow>();

        if (cam != null)
        {
            cam.SetTargets(
                Player1.transform,
                isTwoPlayer ? Player2.transform : null
            );
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
        {
            controller.SetPlayerID(id);
        }

        Debug.Log($"[PlayerManager] Spawned Player {id}");
    }
}