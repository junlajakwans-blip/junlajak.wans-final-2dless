using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _coinText; 
    [SerializeField] private TextMeshProUGUI _highScoreText; 

    [Header("Runtime Values")]
    [SerializeField] private int _currentScore; // Competitive integer score (Time/Kills based)
    [SerializeField] private int _highScore;
    [SerializeField] private int _currentCoins; 
    #endregion

    #region Public Methods

    public void InitializeScore(int startScore)
    {
        _currentScore = startScore;
        UpdateScore(_currentScore); // อัปเดต Score เริ่มต้น (มักจะเป็น 0)
    }

    /// <summary> Updates the main competitive score display (Distance/Time based). </summary>
    public void UpdateScore(int newScore)
    {
        _currentScore = newScore;

        if (_scoreText != null)
            // Show Player only int
            _scoreText.text = $"Score: {_currentScore}"; 

        // ตรวจสอบและอัปเดต High Score (ภายในเท่านั้น)
        if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                // อัปเดต Text High Score ทันทีถ้ามีการทำลายสถิติใหม่
                UpdateHighScoreDisplay(); 
            }
    }

    /// <summary> Updates the coin count display. </summary>
    public void UpdateCoins(int newCoins)
    {
        _currentCoins = newCoins;
        if (_coinText != null)
            _coinText.text = $"Coins: {_currentCoins}";
        Debug.Log($" Coin Remain : {_currentCoins}");
    }

    public void ShowComboEffect(int comboValue)
    {
        Debug.Log($"Combo! x{comboValue}");
        // TODO: Implement visual combo display animation
    }

    /// <summary> 
    /// Sets the high score value (ใช้สำหรับโหลดจาก Save System)
    /// High Score นี้จะไม่แสดงผลใน HUD โดยตรง (เพราะ HUD แสดงคะแนนปัจจุบัน)
    /// </summary>
    public void DisplayHighScore(int highScore)
    {
        _highScore = highScore;
        UpdateHighScoreDisplay();
        Debug.Log($"High Score loaded: {_highScore}");
    }
    
    /// <summary>
    /// Helper method สำหรับอัปเดต _highScoreText
    /// </summary>
    private void UpdateHighScoreDisplay()
    {
        if (_highScoreText != null)
            _highScoreText.text = $"High Score: {_highScore}";
    }
    
    /// <summary>
    /// สำหรับใช้ใน UI อื่นๆ (เช่น Panel Result) เพื่อดึงค่า High Score ที่โหลดมา
    /// </summary>
    public int GetCurrentHighScore() => _highScore;

    /// <summary>
    /// สำหรับใช้ใน UI อื่นๆ (เช่น Panel Result) เพื่อดึงค่า Coin ปัจจุบัน
    /// </summary>
    public int GetCurrentCoins() => _currentCoins;
    
    #endregion
}