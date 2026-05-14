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
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
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

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name != "MainMenu")
            _isGameOver = false;
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

    public void HandleScoreEvent(Player actor, string eventType, int baseValue)
    {
        if (actor == null) return;

        int final = baseValue;

        switch (_currentMode)
        {
            case GameMode.Solo:
                // ปกติ
                break;

            case GameMode.Coop:
                // ตัวอย่าง: โบนัสทีมเล็กน้อย
                final = Mathf.RoundToInt(baseValue * 1.1f);
                break;

            case GameMode.Competition:
                // กันฟาร์มแปลกๆ / กติกาเฉพาะ PvP
                if (actor.IsDead) return;
                break;
        }

        actor.AddScore(final);
        Debug.Log($"[ScoreEvent] {eventType} | Base: {baseValue} | Final: {final} | Player: {actor.PlayerName}");
    }


    // ===== END GAME =====

    private void EndGame(string result)
    {
        if (_isGameOver)
        {
            Debug.Log($"[GameMode] EndGame already called. Ignoring: {result}");
            return;
        }
        _isGameOver = true;

        Debug.Log($"[GameMode] ★★★ GAME OVER TRIGGERED ★★★ Result: {result}");

        SaveScore();

        if (GameManager.Instance != null)
        {
            Debug.Log("[GameMode] Telling GameManager to set game over state.");
            GameManager.Instance.SetGameOver();
        }

        if (UIManager.Instance != null)
        {
            Debug.Log("[GameMode] Telling UIManager to show Result Menu.");
            UIManager.Instance.ShowResultMenu();
        }
        else
        {
            Debug.LogError("[GameMode] UIManager.Instance is MISSING during EndGame!");
        }

        Time.timeScale = 0f;
    }

    private void EvaluateResult()
    {
        var players = PlayerManager.Instance.GetAllPlayers();

        switch (_currentMode)
        {
            case GameMode.Solo:
                EndGame($"Score: {players[0].Data.Score}");
                break;

            case GameMode.Coop:
                int team = players.Sum(p => p.Data.Score);
                EndGame($"Team Score: {team}");
                break;

            case GameMode.Competition:
                var p1 = players[0];
                var p2 = players[1];

                if (p1.Data.Score > p2.Data.Score)
                    EndGame($"Winner: {p1.PlayerName}");
                else if (p2.Data.Score > p1.Data.Score)
                    EndGame($"Winner: {p2.PlayerName}");
                else
                    EndGame("Draw");
                break;
        }
    }

    private void SaveScore()
    {
        var players = PlayerManager.Instance.GetAllPlayers();

        Debug.Log($"===== SAVE SCORE | MODE: {_currentMode} =====");

        foreach (var p in players)
        {
            Debug.Log($"[Before Save] {p.PlayerName} | Score: {p.Data.Score}");
        }

        switch (_currentMode)
        {
            case GameMode.Solo:
            {
                int score = players[0].Data.Score;
                players[0].Data.Progress.UpdateBestScore(score);

                Debug.Log($"[Solo] Saved Score: {score}");
                break;
            }

            case GameMode.Coop:
            {
                int teamScore = players.Sum(p => p.Data.Score);

                Debug.Log($"[Coop] Team Score: {teamScore}");

                foreach (var p in players)
                {
                    p.Data.Progress.UpdateBestScore(teamScore);
                    Debug.Log($"[Coop] {p.PlayerName} Save: {teamScore}");
                }
                break;
            }

            case GameMode.Competition:
            {
                foreach (var p in players)
                {
                    p.Data.Progress.UpdateBestScore(p.Data.Score);
                    Debug.Log($"[PvP] {p.PlayerName} Save: {p.Data.Score}");
                }
                break;
            }
        }

        Debug.Log("===== SAVE COMPLETE =====");
    }

    public void SetMode(GameMode mode)
    {
        _currentMode = mode;
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