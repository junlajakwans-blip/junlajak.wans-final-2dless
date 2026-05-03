using UnityEngine;

public class MenuUI_HUD : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _resultPanel;

    [Header("Runtime State")]
    [SerializeField] private GameObject _currentActivePanel;

    private void Awake()
    {
        // ❌ ไม่มี Singleton
        // ❌ ไม่ DontDestroyOnLoad
    }

    public void ShowPauseMenu(bool isActive)
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(isActive);
            _currentActivePanel = isActive ? _pausePanel : null;
        }
    }

    public void ShowResultMenu()
    {
        if (_resultPanel == null) return;
        _resultPanel.SetActive(true);
        _currentActivePanel = _resultPanel;
    }

    // HUD ไม่ต้องเปิด Store จากในเกม
    public void ShowStoreMenu(bool isActive)
    {
        // intentionally empty
    }

    public void CloseAllPanels()
    {
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_resultPanel != null) _resultPanel.SetActive(false);
        _currentActivePanel = null;
    }

    public bool IsAnyPanelActive()
    {
        return _currentActivePanel != null && _currentActivePanel.activeSelf;
    }
}
