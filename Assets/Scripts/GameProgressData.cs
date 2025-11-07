using System;
using System.Collections.Generic;

[Serializable]
public class GameProgressData
{
    #region Fields
    private List<string> _unlockedMaps = new List<string>();
    private List<string> _unlockedCareers = new List<string>();
    private int _totalCoins;
    private int _bestScore;
    private float _playTime;
    private DateTime _lastPlayDate;
    #endregion

    #region Properties
    public List<string> UnlockedMaps { get; private set; }
    public List<string> UnlockedCareers { get; private set; }

    public int TotalCoins { get; set; }
    public int BestScore { get; set; }
    public float PlayTime { get; set; }
    public DateTime LastPlayDate { get; set; }
    #endregion

    #region Constructor
    public GameProgressData()
    {
        UnlockedMaps = new List<string>();
        UnlockedCareers = new List<string>();
        _totalCoins = 0;
        _bestScore = 0;
        _playTime = 0f;
        _lastPlayDate = DateTime.Now;
    }
    #endregion

    #region Methods
    public void AddUnlockedMap(string mapName)
    {
        if (!UnlockedMaps.Contains(mapName))
            UnlockedMaps.Add(mapName);
    }

    public void AddUnlockedCareer(string careerName)
    {
        if (!UnlockedCareers.Contains(careerName))
            UnlockedCareers.Add(careerName);
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
        UnlockedMaps.Clear();
        UnlockedCareers.Clear();
        TotalCoins = 0;
        BestScore = 0;
        PlayTime = 0f;
        LastPlayDate = DateTime.Now;
    }
    #endregion
}
