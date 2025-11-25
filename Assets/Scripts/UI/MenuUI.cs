using UnityEditor;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    #region Fields
    [Header("Menu Panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private GameObject _storePanel;

    [Header("Runtime State")]
    [SerializeField] private GameObject _currentActivePanel;
    [SerializeField] private StoreUI _storeUI;

    public static MenuUI Instance { get; private set; }
    #endregion


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ทำลายตัวเอง ถ้ามีตัวอื่นอยู่แล้ว
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }
    #region Public Methods

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
        _resultPanel.SetActive(true);
        _currentActivePanel = _resultPanel;
    }


    /// <summary>
    /// Controls the visibility of the Store Menu panel.
    /// NOTE: Data initialization must be handled by UIManager.InitializeStore().
    /// </summary>
    public void ShowStoreMenu(bool isActive)
    {
        if (_storePanel == null) return;

        _storePanel.SetActive(isActive);
        _currentActivePanel = isActive ? _storePanel : null;

        // เรียก Refresh ผ่าน UIManager ทุกครั้งที่เปิดร้าน
        if (isActive && UIManager.Instance != null)
            UIManager.Instance.RefreshStoreUI();
    }

    public void CloseAllPanels()
    {
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_resultPanel != null) _resultPanel.SetActive(false);
        if (_storePanel != null) _storePanel.SetActive(false);
        _currentActivePanel = null;
    }

    public bool IsAnyPanelActive()
    {
        return _currentActivePanel != null && _currentActivePanel.activeSelf;
    }
    #endregion
}