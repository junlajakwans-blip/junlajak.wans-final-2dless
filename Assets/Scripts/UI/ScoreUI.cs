using UnityEngine;
using TMPro;

/// <summary>
/// ScoreUI — จัดการ UI แสดงคะแนนปัจจุบันและคะแนนสูงสุด
/// มีเอฟเฟกต์คอมโบหรือโบนัสเมื่อทำคะแนนต่อเนื่อง
/// </summary>
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
    /// <summary>
    /// ตั้งค่าคะแนนเริ่มต้น
    /// </summary>
    public void InitializeScore(int startScore)
    {
        _currentScore = startScore;
        UpdateScore(_currentScore);
    }

    /// <summary>
    /// อัปเดตคะแนนปัจจุบัน
    /// </summary>
    public void UpdateScore(int newScore)
    {
        _currentScore = newScore;

        if (_scoreText != null)
            _scoreText.text = $"Score: {_currentScore}";

        if (_currentScore > _highScore)
            _highScore = _currentScore;
    }

    /// <summary>
    /// แสดงเอฟเฟกต์คอมโบเมื่อผู้เล่นทำคะแนนต่อเนื่อง
    /// </summary>
    public void ShowComboEffect(int comboValue)
    {
        Debug.Log($"🔥 Combo! x{comboValue}");
        // TODO: อาจใส่ Particle หรือ Text Popup effect
    }

    /// <summary>
    /// แสดงคะแนนสูงสุด
    /// </summary>
    public void DisplayHighScore(int highScore)
    {
        _highScore = highScore;

        if (_scoreText != null)
            _scoreText.text = $"High Score: {_highScore}";

        Debug.Log($"🏆 High Score updated: {_highScore}");
    }
    #endregion
}
