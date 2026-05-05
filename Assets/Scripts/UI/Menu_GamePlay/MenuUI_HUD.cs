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
        if (_pausePanel == null || _resultPanel == null)
            AutoFindPanels();
    }

    private void AutoFindPanels()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (!canvas.gameObject.scene.isLoaded) continue;
            if (canvas.gameObject.scene.name == "DontDestroyOnLoad") continue;
            if (_pausePanel == null)
                _pausePanel = FindInChildren(canvas.transform, "Panel_Pause");
            if (_resultPanel == null)
                _resultPanel = FindInChildren(canvas.transform, "Panel_Result");
            if (_pausePanel != null && _resultPanel != null) break;
        }
        Debug.Log($"[MenuUI_HUD] AutoFind → Pause:{(_pausePanel != null ? "OK" : "NULL")} Result:{(_resultPanel != null ? "OK" : "NULL")}");
    }

    private GameObject FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child.gameObject;
            var found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
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
