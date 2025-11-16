using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _coinText; 

    [Header("Runtime Values")]
    [SerializeField] private int _currentScore; // Competitive integer score (Time/Kills based)
    [SerializeField] private int _highScore;
    [SerializeField] private int _currentCoins; 
    #endregion

    #region Public Methods

    public void InitializeScore(int startScore)
    {
        _currentScore = startScore;
        UpdateScore(_currentScore);
    }

    /// <summary> Updates the main competitive score display (Distance/Time based). </summary>
    public void UpdateScore(int newScore)
    {
        _currentScore = newScore;

        if (_scoreText != null)
            // Show Player only int
            _scoreText.text = $"Score: {_currentScore}"; 

        if (_currentScore > _highScore)
            _highScore = _currentScore;
    }

    /// <summary> Updates the coin count display. </summary>
    public void UpdateCoins(int newCoins)
    {
        _currentCoins = newCoins;
        if (_coinText != null)
            _coinText.text = $"Coins: {_currentCoins}";
    }

    public void ShowComboEffect(int comboValue)
    {
        Debug.Log($"Combo! x{comboValue}");
        // TODO: Implement visual combo display animation
    }

    /// <summary> Sets the high score display (used on game over screen or initialization). </summary>
    public void DisplayHighScore(int highScore)
    {
        _highScore = highScore;

        if (_scoreText != null)
            _scoreText.text = $"High Score: {_highScore}";

        Debug.Log($"High Score updated: {_highScore}");
    }
    #endregion
}