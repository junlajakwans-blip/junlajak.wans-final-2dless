using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// PlayerManager - จัดการการเกิดของ Player และ UI HealthBar
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private Player _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform _spawnPointP1;
    [SerializeField] private Transform _spawnPointP2;

    [Header("UI Prefab")]
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

        // 1. โหลด HealthBar Prefab จาก Resources ถ้าใน Inspector ไม่ได้ลากใส่ไว้
        if (_healthBarPrefab == null)
        {
            _healthBarPrefab = Resources.Load<HealthBarUI>("UI/Panel_HealthBar");
        }

        if (_healthBarPrefab == null)
        {
            Debug.LogError("[PlayerManager] Load prefab failed: Resources/UI/Panel_HealthBar");
            return;
        }

        // 2. ค้นหา Anchor สำหรับเกาะ UI
    var canvas = GameObject.Find("Canvas_HUD"); 
        if (canvas != null)
        {
            // ใช้ GetChild หรือ Find แบบไม่ระบุ Path เต็ม
            _anchorP1 = RecursiveFind(canvas.transform, "UI_HealthBar_P1");
            _anchorP2 = RecursiveFind(canvas.transform, "UI_HealthBar_P2");
        }

        if (_anchorP1 == null || _anchorP2 == null)
            Debug.LogError("[PlayerManager] UI Anchors still missing! Check Object Names.");
    }

    private Transform RecursiveFind(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = RecursiveFind(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void Start()
    {
        FindSpawnPoints();
        StartCoroutine(SpawnPlayers());
    }

    private IEnumerator SpawnPlayers()
    {

        _players.Clear();

        // ดึงจำนวน Player จาก GameModeManager
        int playerCount = GameModeManager.Instance != null ? GameModeManager.Instance.PlayerCount : 1;

        Transform[] spawnPositions = new Transform[] { _spawnPointP1, _spawnPointP2 };

        for (int i = 0; i < playerCount; i++)
        {
            int playerID = i + 1;
            Transform spawnPoint = spawnPositions[i];

            // ถ้าหาจุดเกิดไม่เจอ ให้ Backup ไปที่ P1
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[PlayerManager] SpawnPoint P{playerID} missing! Fallback to P1.");
                spawnPoint = _spawnPointP1;
            }

            if (spawnPoint == null) continue;

            // สปาว Player
            var player = Instantiate(_playerPrefab, spawnPoint.position, Quaternion.identity);
            player.name = $"Player_{playerID}";
            _players.Add(player);

            // เก็บ Reference
            if (playerID == 1) Player1 = player;
            else if (playerID == 2) Player2 = player;

            // ตั้งค่า UI และ ID
            SetupPlayer(player, playerID);

            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        Debug.Log($"[PlayerManager] Successfully spawned {_players.Count} players");
        Debug.Log("MODE = " + GameModeManager.Instance.CurrentMode);
        Debug.Log("COUNT = " + GameModeManager.Instance.PlayerCount);
        
        
    }

    private void FindSpawnPoints()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var root in roots)
        {
            var p1 = root.transform.Find("Spawn_P1");
            var p2 = root.transform.Find("Spawn_P2");

            if (p1 != null) _spawnPointP1 = p1;
            if (p2 != null) _spawnPointP2 = p2;
        }

        if (_spawnPointP1 == null)
            Debug.LogError("Spawn_P1 NOT FOUND");

        if (_spawnPointP2 == null)
            Debug.LogError("Spawn_P2 NOT FOUND");
    }

    private void SetupPlayer(Player player, int id)
    {
        // 1. ตั้งค่า PlayerID ให้ Controller
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetPlayerID(id);

        // 2. จัดการ UI HealthBar
        Transform anchor = (id == 1) ? _anchorP1 : _anchorP2;

        if (anchor != null && _healthBarPrefab != null)
        {
            // สปาว UI ลงไปเกาะที่ Anchor (เพราะ Anchor เป็นแค่ Empty Location)
            var uiInstance = Instantiate(_healthBarPrefab, anchor);
            uiInstance.name = $"HealthBar_P{id}";
            uiInstance.Setup(player);
            
            Debug.Log($"[PlayerManager] P{id} UI Attached to {anchor.name}");
        }
        else
        {
            Debug.LogError($"[PlayerManager] Cannot setup UI for P{id}: Anchor or Prefab is NULL");
        }
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