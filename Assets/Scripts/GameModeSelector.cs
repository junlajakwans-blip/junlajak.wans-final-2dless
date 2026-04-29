using UnityEngine;

public class GameModeSelector : MonoBehaviour
{
    public static GameModeSelector Instance { get; private set; }

    public GameModeManager.GameMode CurrentMode { get; private set; } 
        = GameModeManager.GameMode.Solo;

    private void Awake()
    {
        // Singleton แบบไม่ต้อง DontDestroy (เพราะอยู่ใน MainMenu Scene)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ===== ใช้กับปุ่ม UI =====

    public void SelectSolo()
    {
        SetMode(GameModeManager.GameMode.Solo);
    }

    public void SelectCoop()
    {
        SetMode(GameModeManager.GameMode.Coop);
    }

    public void SelectCompetition()
    {
        SetMode(GameModeManager.GameMode.Competition);
    }

    // ===== Core Logic =====

    public void SetMode(GameModeManager.GameMode mode)
    {
        CurrentMode = mode;

        Debug.Log($"[GameModeSelector] Selected: {mode}");

        // ไปหน้าเลือกแมพ
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateModeUI(mode);
            UIManager.Instance.ShowSelectMap();
        }
    }
}