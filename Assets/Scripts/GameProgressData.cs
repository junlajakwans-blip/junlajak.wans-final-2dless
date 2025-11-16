using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameProgressData
{
    #region Fields
    
    [SerializeField] private List<string> _unlockedMaps = new List<string>();
    [SerializeField] private List<string> _unlockedCareers = new List<string>();
    
    //Upgrade Store
    [SerializeField] private int _permanentHPUpgradeLevel = 0; // For Whey Protein (+10 HP/level)
    //[SerializeField] private int _permanentCoinMultiplier = 1; // Future expansion

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

    public int PermanentHPUpgradeLevel { get => _permanentHPUpgradeLevel; set => _permanentHPUpgradeLevel = value; }

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
        // Initialization now uses the serialized fields
        _unlockedMaps = new List<string>();
        _unlockedCareers = new List<string>();
        
        _totalCoins = 0;
        _bestScore = 0;
        _playTime = 0f;
        _lastPlayDate = DateTime.Now;
    }
    #endregion

    #region Methods
    /// <summary>
    /// Used by StoreMap.cs to permanently unlock a new map.
    /// </summary>
    public void AddUnlockedMap(string mapName)
    {
        if (!_unlockedMaps.Contains(mapName))
            _unlockedMaps.Add(mapName);
    }

    public bool IsMapUnlocked(string mapName)
    {
        return _unlockedMaps.Contains(mapName);
    }


    public void AddUnlockedCareer(string careerName)
    {
        if (!_unlockedCareers.Contains(careerName))
            _unlockedCareers.Add(careerName);
    }

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

    public void ResetProgress()
    {
        _unlockedMaps.Clear();
        _unlockedCareers.Clear();
        _totalCoins = 0;
        _bestScore = 0;
        _playTime = 0f;
        _lastPlayDate = DateTime.Now;
        
        // Reset permanent upgrades on full reset
        _permanentHPUpgradeLevel = 0;
    }
    #endregion
}