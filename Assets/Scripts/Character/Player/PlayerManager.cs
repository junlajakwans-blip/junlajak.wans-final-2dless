using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private Player _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPointP1;
    [SerializeField] private Transform _spawnPointP2;

    [Header("Mode")]
    [SerializeField] private bool _isTwoPlayer = false;

    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }

    private void Start()
    {
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        // Spawn Player 1
        Player1 = Instantiate(_playerPrefab, _spawnPointP1.position, Quaternion.identity);
        SetupPlayer(Player1, 1);

        if (_isTwoPlayer)
        {
            Player2 = Instantiate(_playerPrefab, _spawnPointP2.position, Quaternion.identity);
            SetupPlayer(Player2, 2);
        }
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