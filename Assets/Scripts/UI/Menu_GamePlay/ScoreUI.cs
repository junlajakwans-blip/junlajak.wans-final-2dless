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
    
    [Header("Player Slot")]
    [Tooltip("Which player this ScoreUI instance represents (1 = P1, 2 = P2).")]
    [SerializeField] private int _playerNumber = 1;
    [Header("Display Mode")]
    [Tooltip("When true this ScoreUI will render the player's Score into the coin area (used for 2-player side displays).")]
    [SerializeField] private bool _sideMode = false;
    
    // เปลี่ยนชื่อ Property สำหรับดึงค่า High Score ที่บันทึกไว้
    public int GetSavedHighScoreValue() => _savedHighScore;

    // Expose player slot so other systems can target this UI
    public int PlayerNumber => _playerNumber;

    public void SetPlayerNumber(int num)
    {
        _playerNumber = num;
    }

    /// <summary>
    /// When enabled, Score text will be rendered to the coin area and the central score text will be hidden.
    /// Used for per-player side displays in 2-player modes.
    /// </summary>
    public void SetSideMode(bool enabled)
    {
        _sideMode = enabled;
        if (_scoreText != null)
            _scoreText.gameObject.SetActive(!enabled);
        if (_coinText != null)
        {
            if (enabled)
            {
                _coinText.gameObject.SetActive(true);
                _coinText.color = new Color(_coinText.color.r, _coinText.color.g, _coinText.color.b, 1f);
                _coinText.text = ""; // will be updated by UpdateScore when needed
            }
        }
    }

    /// <summary>
    /// Debug helper: logs whether serialized Text references are assigned.
    /// </summary>
    public void DebugLogBindings()
    {
        Debug.Log($"[ScoreUI] {name} bindings -> _scoreText={( _scoreText != null ? _scoreText.name : "NULL")}, _coinText={( _coinText != null ? _coinText.name : "NULL")}, _finalResultScoreText={( _finalResultScoreText != null ? _finalResultScoreText.name : "NULL")}, sideMode={_sideMode}");
    }

    #endregion

    #region Public Methods

    [Header("Highlight")]
    [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float _highlightDuration = 0.8f;
    private Coroutine _flashCoroutine;

    private void StartFlash(TextMeshProUGUI target)
    {
        if (target == null) return;
        // If the target or its GameObject is inactive, do not start coroutine — it will warn in Unity.
        if (!target.gameObject.activeInHierarchy || !this.gameObject.activeInHierarchy)
            return;

        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashText(target));
    }

    private System.Collections.IEnumerator FlashText(TextMeshProUGUI target)
    {
        var original = target.color;
        target.color = _highlightColor;
        yield return new WaitForSeconds(_highlightDuration);
        try { target.color = original; } catch { }
        _flashCoroutine = null;
    }

    public void InitializeScore(int startScore)
    {
        _currentScore = startScore;
        UpdateScore(_currentScore); // อัปเดต Score เริ่มต้น (มักจะเป็น 0)
    }

    /// <summary> Updates the main competitive score display (Distance/Time based). </summary>
    public void UpdateScore(int newScore)
    {
        Debug.Log($"<color=cyan>[ScoreUI: {name}] UpdateScore({newScore}) | SideMode={_sideMode} | scoreText={(_scoreText != null)} | coinText={(_coinText != null)}</color>");
        _currentScore = newScore;

        if (_sideMode)
        {
            // Render score into coin area for side displays
            if (_coinText != null)
            {
                // Ensure target text is visible
                try
                {
                    if (!_coinText.gameObject.activeInHierarchy)
                        _coinText.gameObject.SetActive(true);
                    var c = _coinText.color;
                    if (c.a <= 0f)
                        _coinText.color = new Color(c.r, c.g, c.b, 1f);
                }
                catch { }

                _coinText.text = $"{_currentScore}"; // shorter display for coin-area
                StartFlash(_coinText);
            }
            else
            {
                // Fallback: write to central score text if coin text unavailable
                if (_scoreText != null)
                {
                    _scoreText.gameObject.SetActive(true);
                    _scoreText.text = $"Score: {_currentScore:D6}";
                }
            }
        }
        else
        {
                if (_scoreText != null)
                {
                    _scoreText.text = $"Score: {_currentScore:D6}";
                    StartFlash(_scoreText);
                }
                else {
                    Debug.LogWarning($"<color=red>[ScoreUI: {name}] ScoreText is NULL!</color>");
                }
        }
    }

    /// <summary> Updates the coin count display. </summary>
    public void UpdateCoins(int newCoins)
    {
        Debug.Log($"<color=yellow>[ScoreUI: {name}] UpdateCoins({newCoins}) | SideMode={_sideMode}</color>");
        // When in side mode, coins are replaced by score display — ignore coin updates
        if (_sideMode) return;

        _currentCoins = newCoins;
        if (_coinText != null)
        {
            // Show only session-collected coins (starts at 0 each run)
            _coinText.text = $"Coins: {_currentCoins}";
            StartFlash(_coinText);
        }
        else {
             Debug.LogWarning($"<color=red>[ScoreUI: {name}] CoinText is NULL!</color>");
        }
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
    ///  เมธอดหลักที่เรียกเมื่อจบรอบ (Solo / Coop)
    /// </summary>
    public void ShowFinalResult()
    {
        int finalScore = _currentScore;
        UpdateFinalResultDisplay(finalScore);

        if (finalScore > _savedHighScore)
        {
            _savedHighScore = finalScore;
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.SetGlobalHighScore(_savedHighScore);
            Debug.Log($"NEW GLOBAL HIGH SCORE RECORD: {_savedHighScore}");
        }
    }

    /// <summary>
    /// แสดงผลแบบ Competition (P1 vs P2)
    /// </summary>
    public void ShowCompetitionResult(int p1Score, int p2Score)
    {
        if (_finalResultScoreText == null) return;

        string winner = p1Score > p2Score ? "P1 WIN!"
                      : p2Score > p1Score ? "P2 WIN!"
                      : "DRAW!";
        _finalResultScoreText.text = $"P1: {p1Score:D6}\nP2: {p2Score:D6}\n{winner}";
    }

    /// <summary>
    /// อัปเดต Score HUD แบบ Competition (ใช้ _scoreText เดิม แสดงทั้ง P1 และ P2)
    /// </summary>
    public void UpdateCompetitionScores(int p1Score, int p2Score)
    {
            if (_scoreText != null)
            {
                _scoreText.text = $"P1:{p1Score:D6}  P2:{p2Score:D6}";
                Debug.Log($"[ScoreUI] UpdateCompetitionScores -> P1: {p1Score:D6} | P2: {p2Score:D6}");
                StartFlash(_scoreText);
            }
            else
            {
                Debug.LogWarning("[ScoreUI] _scoreText is NULL — cannot display competition scores.");
            }
    }

    /// <summary>
    /// Debug helper: force enable both score and coin texts and reset alpha.
    /// Use to diagnose visibility issues at runtime.
    /// </summary>
    public void ForceShowAll()
    {
        if (_scoreText != null)
        {
            try { _scoreText.gameObject.SetActive(true); _scoreText.color = new Color(_scoreText.color.r, _scoreText.color.g, _scoreText.color.b, 1f); } catch { }
        }
        if (_coinText != null)
        {
            try { _coinText.gameObject.SetActive(true); _coinText.color = new Color(_coinText.color.r, _coinText.color.g, _coinText.color.b, 1f); } catch { }
        }
        Debug.Log($"[ScoreUI] {name} ForceShowAll called — scoreText={( _scoreText != null ? "ok" : "NULL")}, coinText={( _coinText != null ? "ok" : "NULL")}");
    }

    #endregion
}