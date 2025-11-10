using UnityEngine;
/// <summary>
/// Class to hold player data such as stats, currency, and progress
/// </summary>
[System.Serializable]
public class PlayerData
{
    #region Fields
    [Header("Basic Info")]
    [SerializeField] private string _playerName;
    [SerializeField] private string _selectedCareer;

    [Header("Stats")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _attackPower = 10f;
    [SerializeField] private int _defense = 5;

    [Header("Systems")]
    [SerializeField] private Currency _currency = new Currency();
    [SerializeField] private GameProgressData _progress = new GameProgressData();
    #endregion


    #region Properties
    public string PlayerName { get => _playerName; set => _playerName = value; }
    public string SelectedCareer { get => _selectedCareer; set => _selectedCareer = value; }

    public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
    public float Speed { get => _speed; set => _speed = value; }
    public float AttackPower { get => _attackPower; set => _attackPower = value; }
    public int Defense { get => _defense; set => _defense = value; }

    public Currency Currency => _currency;
    public GameProgressData Progress => _progress;
    #endregion


    #region Methods
    /// <summary>
    /// Upgrade any permanent player stat (used by shop or level up system)
    /// </summary>
    public void UpgradeStat(string statName, int value)
    {
        switch (statName)
        {
            case "MaxHealth": _maxHealth += value; break;
            case "Speed": Speed += value; break;
            case "AttackPower": AttackPower += value; break;
            case "Defense": Defense += value; break;
            default:
                Debug.LogWarning($"[PlayerData] Unknown stat: {statName}");
                break;
        }
    }

    /// <summary>
    /// Reset player state at start of game or after death
    /// </summary>
    public void ResetPlayerState()
    {
        MaxHealth = 100;
        Speed = 5f;
        AttackPower = 10f;
        Defense = 5;

        Currency.ResetAll();
    }
#endregion
}
