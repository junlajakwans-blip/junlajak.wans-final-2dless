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

    private GameMode _currentMode = GameMode.Solo;
    public GameMode CurrentMode => _currentMode;

    private void Start()
    {
        //  ดึงค่าจาก UI Selector
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

        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
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
        Debug.Log($"[GameMode] GAME OVER → {result}");

        //  ส่งไป UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResultMenu();
        }

        Time.timeScale = 0f; // หยุดเกม
    }
}