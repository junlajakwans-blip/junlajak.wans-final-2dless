using UnityEngine;
using System.Linq;

public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        Solo,
        Coop,
        Competition
    }

    private bool _isGameOver = false;
    private GameMode _currentMode = GameMode.Solo;
    public GameMode CurrentMode => _currentMode;

    public static GameModeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return; 
        }

        if (GameModeSelector.Instance != null)
        {
            _currentMode = GameModeSelector.Instance.CurrentMode;
        }
        Debug.Log("[GameMode] Mode = " + _currentMode);
    }


    private void OnEnable()
    {
        Player.OnAnyPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnAnyPlayerDied -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath(Player deadPlayer)
    {
        Debug.Log($"[GameMode] Player {deadPlayer.PlayerName} died");

        var players = PlayerManager.Instance.GetAllPlayers();
        var alivePlayers = players.Where(p => !p.IsDead).ToList();

        switch (_currentMode)
        {
            case GameMode.Solo:
                HandleSolo();
                break;

            case GameMode.Coop:
                HandleCoop(alivePlayers.Count);
                break;

            case GameMode.Competition:
                HandleCompetition(alivePlayers);
                break;
        }
    }

    // ===== MODE LOGIC =====

    private void HandleSolo()
    {
        EndGame("You Died");
    }

    private void HandleCoop(int aliveCount)
    {
        if (aliveCount <= 0)
        {
            Debug.Log("[GameMode] All players dead → Game Over");
            EndGame("All Dead");
        }
    }

    private void HandleCompetition(System.Collections.Generic.List<Player> alivePlayers)
    {
        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            Debug.Log($"[GameMode] Winner: {winner.PlayerName}");
            EndGame($"Winner: {winner.PlayerName}");
        }
        else if (alivePlayers.Count == 0)
        {
            Debug.Log("[GameMode] Draw (everyone died)");
            EndGame("Draw");
        }
    }

    // ===== END GAME =====

    private void EndGame(string result)
    {
        if (_isGameOver) return; // ถ้าเกมจบแสดงผลลัพธ์
        _isGameOver = true; 

        Debug.Log($"[GameMode] GAME OVER → {result}");

        if (UIManager.Instance != null)
            UIManager.Instance.ShowResultMenu();

        Time.timeScale = 0f; // หยุดเกม
    }

    public int PlayerCount
    {
        get
        {
            switch (_currentMode)
            {
                case GameMode.Solo:
                    return 1;

                case GameMode.Coop:
                case GameMode.Competition:
                    return 2;

                default:
                    return 1;
            }
        }
    }

    public bool AllowFriendlyFire
    {
        get
        {
            return _currentMode == GameMode.Competition;
        }
    }

    public bool AllowRevive
    {
        get
        {
            return _currentMode == GameMode.Coop;
        }
    }
}