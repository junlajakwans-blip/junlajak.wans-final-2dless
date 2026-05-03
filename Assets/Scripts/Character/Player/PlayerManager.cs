using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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
    private List<Player> _players = new List<Player>();

    public Player Player1 { get; private set; }
    public Player Player2 { get; private set; }

    public static PlayerManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

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
        int playerCount = GameModeManager.Instance.PlayerCount;

        Transform[] spawnPoints = new Transform[]
        {
            _spawnPointP1,
            _spawnPointP2
        };

        for (int i = 0; i < playerCount; i++)
        {
            Transform spawn = spawnPoints[i];

            if (spawn == null)
            {
                Debug.LogWarning($"Spawn {i} missing → ใช้ P1");
                spawn = _spawnPointP1;
            }

            var player = Instantiate(_playerPrefab, spawn.position, Quaternion.identity);
            _players.Add(player);
            SetupPlayer(player, i + 1);

            if (i == 0) Player1 = player;
            if (i == 1) Player2 = player;
        }

        Debug.Log($"[PlayerManager] Spawned {playerCount} players");
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
        Transform anchor = (id == 1) ? _anchorP1 : _anchorP2;

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
        return _players.FirstOrDefault(p => p != null && !p.IsDead);
    }

    public List<Player> GetAllPlayers()
    {
        return _players;
    }
}