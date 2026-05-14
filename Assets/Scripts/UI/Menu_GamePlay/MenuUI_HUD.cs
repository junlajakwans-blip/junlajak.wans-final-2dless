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
        // Search in all loaded canvases
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            // Priority: Find in scene canvases first
            if (_pausePanel == null)
            {
                _pausePanel = FindInChildren(canvas.transform, "Panel_Menu_Pause");
                if (_pausePanel == null) _pausePanel = FindInChildren(canvas.transform, "Panel_Pause");
            }
            
            if (_resultPanel == null)
            {
                _resultPanel = FindInChildren(canvas.transform, "Panel_Result");
            }

            if (_pausePanel != null && _resultPanel != null) break;
        }

        // Fallback: If still not found, search by Name globally
        if (_pausePanel == null)
        {
             _pausePanel = SearchGlobal("Panel_Menu_Pause") ?? SearchGlobal("Panel_Pause");
        }
        
        if (_resultPanel == null)
        {
             _resultPanel = SearchGlobal("Panel_Result");
        }

        Debug.Log($"[MenuUI_HUD] AutoFind Result -> Pause:{(_pausePanel != null ? _pausePanel.name : "MISSING")} | Result:{(_resultPanel != null ? _resultPanel.name : "MISSING")}");
        
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_resultPanel != null) _resultPanel.SetActive(false);
    }

    private GameObject SearchGlobal(string name)
    {
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name == name && (t.gameObject.scene.isLoaded || t.gameObject.scene.name == "DontDestroyOnLoad"))
            {
                return t.gameObject;
            }
        }
        return null;
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
