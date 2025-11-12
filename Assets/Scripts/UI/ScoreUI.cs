using UnityEngine;
using TMPro;


public class ScoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Runtime Values")]
    [SerializeField] private int _currentScore;
    [SerializeField] private int _highScore;
    #endregion

    #region Public Methods

    public void InitializeScore(int startScore)
    {
        _currentScore = startScore;
        UpdateScore(_currentScore);
    }


    public void UpdateScore(int newScore)
    {
        _currentScore = newScore;

        if (_scoreText != null)
            _scoreText.text = $"Score: {_currentScore}";

        if (_currentScore > _highScore)
            _highScore = _currentScore;
    }


    public void ShowComboEffect(int comboValue)
    {
        Debug.Log($"Combo! x{comboValue}");

    }


    public void DisplayHighScore(int highScore)
    {
        _highScore = highScore;

        if (_scoreText != null)
            _scoreText.text = $"High Score: {_highScore}";

        Debug.Log($"High Score updated: {_highScore}");
    }
    #endregion
}
