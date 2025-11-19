using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameProgressData
{
    #region Fields
    
    [SerializeField] private List<string> _unlockedMaps = new();
    [SerializeField] private List<string> _unlockedCareers = new();

    // NEW — upgrade levels per StoreItem ID
    [SerializeField] private Dictionary<string, int> _upgradeLevels = new();

    [SerializeField] private int _totalCoins;
    [SerializeField] private int _totalTokens;
    [SerializeField] private int _totalKeyMaps;
    [SerializeField] private int _bestScore;
    [SerializeField] private float _playTime;
    [SerializeField] private DateTime _lastPlayDate;
    #endregion

    #region Properties
    public List<string> UnlockedMaps => _unlockedMaps;
    public List<string> UnlockedCareers => _unlockedCareers;

    // Read-only for save/load by store
    public Dictionary<string, int> UpgradeLevels => _upgradeLevels;

    public int TotalCoins { get => _totalCoins; set => _totalCoins = value; }
    public int TotalTokens { get => _totalTokens; set => _totalTokens = value; }
    public int TotalKeyMaps { get => _totalKeyMaps; set => _totalKeyMaps = value; }
    public int BestScore { get => _bestScore; set => _bestScore = value; }
    public float PlayTime { get => _playTime; set => _playTime = value; }
    public DateTime LastPlayDate { get => _lastPlayDate; set => _lastPlayDate = value; }
    #endregion

    #region Constructor
    public GameProgressData()
    {
        _unlockedMaps = new();
        _unlockedCareers = new();
        _upgradeLevels = new();
        _totalCoins = 0;
        _bestScore = 0;
        _playTime = 0f;
        _lastPlayDate = DateTime.Now;
    }
    #endregion

    #region Upgrade Logic (NEW)
    public int GetUpgradeLevel(string itemID)
    {
        return _upgradeLevels.TryGetValue(itemID, out int lv) ? lv : 0;
    }

    public void SetUpgradeLevel(string itemID, int level)
    {
        _upgradeLevels[itemID] = Mathf.Max(0, level);
    }
    #endregion

    #region Map + Career Methods
    public void AddUnlockedMap(string id)
    {
        if (!_unlockedMaps.Contains(id))
            _unlockedMaps.Add(id);
    }

    public bool IsMapUnlocked(string id)
    {
        return _unlockedMaps.Contains(id);
    }

    public void AddUnlockedCareer(string id)
    {
        if (!_unlockedCareers.Contains(id))
            _unlockedCareers.Add(id);
    }
    #endregion

    #region Stats + Score
    public void AddCoins(int amount)
    {
        if (amount > 0)
            TotalCoins += amount;
    }

    public void UpdateBestScore(int score)
    {
        if (score > BestScore)
            BestScore = score;
    }
    #endregion

    #region Reset
    public void ResetProgress()
    {
        _unlockedMaps.Clear();
        _unlockedCareers.Clear();
        _upgradeLevels.Clear();
        _totalCoins = 0;
        _bestScore = 0;
        _playTime = 0f;
        _lastPlayDate = DateTime.Now;
    }
    #endregion
}
