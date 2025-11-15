using UnityEngine;
using System.Collections.Generic; // Added for list access in ShowStoreMenu (removed below)

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
    public void InitializeStore(List<StoreBase> stores, StoreManager manager)
    {
        if (_storeUI != null)
            _storeUI.InitializeStore(manager, stores);
    }


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

    /// <summary>
    /// Controls the visibility of the Store Menu panel.
    /// NOTE: Data initialization must be handled by UIManager.InitializeStore().
    /// </summary>
    public void ShowStoreMenu(bool isActive)
    {
        if (_storePanel != null)
        {
            _storePanel.SetActive(isActive);
            _currentActivePanel = isActive ? _storePanel : null;
        }
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