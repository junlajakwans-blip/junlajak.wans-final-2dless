using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    #region Fields
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _coinText; 
    // เปลี่ยนชื่อ Text Component สำหรับแสดงผลลัพธ์รอบสุดท้าย
    [SerializeField] private TextMeshProUGUI _finalResultScoreText; 

    [Header("Runtime Values")]
    [SerializeField] private int _currentScore; // Competitive integer score (Time/Kills based)
    // เปลี่ยนชื่อตัวแปร Global High Score ที่บันทึกไว้
    [SerializeField] private int _savedHighScore; 
    [SerializeField] private int _currentCoins; 
    
    // เปลี่ยนชื่อ Property สำหรับดึงค่า High Score ที่บันทึกไว้
    public int GetSavedHighScoreValue() => _savedHighScore;

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
            _scoreText.text = $"Score: {_currentScore:D6}";  
    }

    /// <summary> Updates the coin count display. </summary>
    public void UpdateCoins(int newCoins)
    {
        _currentCoins = newCoins;
        if (_coinText != null)
            _coinText.text = $"Coins: {_currentCoins}";
        Debug.Log($" Coin Remain : {_currentCoins} ");
        Debug.Log("SCORE UI RECEIVED COIN UPDATE");
    }

    public void ShowComboEffect(int comboValue)
    {
        Debug.Log($"Combo! x{comboValue}");
        // TODO: Implement visual combo display animation
    }

    /// <summary> 
    /// โหลดค่าคะแนนสูงสุดที่บันทึกไว้ (Global High Score)
    /// </summary>
    public void DisplaySavedHighScore(int highScore)
    {
        _savedHighScore = highScore;
        // ไม่ต้องเรียก UpdateFinalResultDisplay() ที่นี่ เพราะเราต้องการให้แสดงแค่ตอนจบรอบ
        Debug.Log($"Global High Score loaded: {_savedHighScore}");
    }
    
    /// <summary>
    /// Helper method สำหรับอัปเดต Text แสดงผลลัพธ์สุดท้าย (Final Score)
    /// </summary>
    private void UpdateFinalResultDisplay(int scoreToDisplay)
    {
        if (_finalResultScoreText != null)
        {
            // แสดงคะแนนสุดท้ายของรอบนั้น
            _finalResultScoreText.text = $"Final Score: {scoreToDisplay:D6}"; 
        }
    }
    
    /// <summary>
    /// สำหรับใช้ใน UI อื่นๆ (เช่น Panel Result) เพื่อดึงค่า Global High Score ที่บันทึกไว้
    /// </summary>
    public int GetCurrentSavedHighScore() => _savedHighScore; // เปลี่ยนชื่อจาก GetCurrentHighScore()

    /// <summary>
    /// สำหรับใช้ใน UI อื่นๆ (เช่น Panel Result) เพื่อดึงค่า Coin ปัจจุบัน
    /// </summary>
    public int GetCurrentCoins() => _currentCoins;
    
    //  เปลี่ยนชื่อเมธอด
    public void SyncSavedHighScoreFromSave(int savedValue)
    {
        _savedHighScore = savedValue;
        Debug.Log($"Global High Score synchronized: {_savedHighScore}");
    }

    /// <summary>
    ///  เมธอดหลักที่เรียกเมื่อจบรอบ
    /// จะแสดงคะแนนสุดท้ายใน Result Text และตรวจสอบสถิติสูงสุดใหม่
    /// </summary>
    public void ShowFinalResult()
    {
        int finalScore = _currentScore;
        
        // 1. แสดงคะแนนสุดท้ายของรอบนั้น
        UpdateFinalResultDisplay(finalScore); 

        // 2. ตรวจสอบสถิติสูงสุดที่บันทึกไว้ (Global High Score)
        if (finalScore > _savedHighScore)
        {
            _savedHighScore = finalScore;
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SetGlobalHighScore(_savedHighScore);
            }
            Debug.Log($"NEW GLOBAL HIGH SCORE RECORD: {_savedHighScore}");
        }
    }

    #endregion
}