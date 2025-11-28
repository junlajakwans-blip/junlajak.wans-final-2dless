/*using UnityEngine;

public class MenuUI_Main : MonoBehaviour
{
    [Header("Menu Panels")]
    //[SerializeField] private GameObject _pausePanel;      // ไม่ใช้ใน Main แต่รักษาฟอร์แมตไว้
    //[SerializeField] private GameObject _resultPanel;     // ไม่ใช้ใน Main แต่รักษาฟอร์แมตไว้
    [SerializeField] private GameObject _storePanel;

    [Header("Runtime State")]
    [SerializeField] private GameObject _currentActivePanel;
    [SerializeField] private StoreUI _storeUI;

    private void Awake()
    {
        // ❌ ไม่มี Singleton
        // ❌ ไม่ DontDestroyOnLoad
    }

    public void ShowStoreMenu(bool isActive)
    {
        if (_storePanel == null) return;

        _storePanel.SetActive(isActive);
        _currentActivePanel = isActive ? _storePanel : null;

        if (isActive && UIManager.Instance != null)
            UIManager.Instance.RefreshStoreUI();
    }

    public void CloseAllPanels()
    {
        if (_storePanel != null) _storePanel.SetActive(false);
        _currentActivePanel = null;
    }

    public bool IsAnyPanelActive()
    {
        return _currentActivePanel != null && _currentActivePanel.activeSelf;
    }
}
*/