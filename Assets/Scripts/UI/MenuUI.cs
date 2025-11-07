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
    #endregion

    #region Public Methods
    public void ShowPauseMenu(bool isActive)
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(isActive);
            _currentActivePanel = isActive ? _pausePanel : null;
        }
    }

    public void ShowResultMenu(int finalScore, int coins)
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
            _currentActivePanel = _resultPanel;
            Debug.Log($" Result Menu: Score={finalScore}, Coins={coins}");
        }
    }

    public void ShowStoreMenu(bool isActive)
    {
        if (_storePanel != null)
        {
            _storePanel.SetActive(isActive);
            _currentActivePanel = isActive ? _storePanel : null;
        }

        if (isActive && _storeUI != null)
            _storeUI.InitializeStore(new System.Collections.Generic.List<string>());
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
