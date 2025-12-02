using UnityEngine;
using UnityEngine.UI;

public class UIButton_UIManager : MonoBehaviour
{
    public enum Action { MainMenu, SelectMap, Store, Settings }

    [SerializeField] Action action;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(InvokeAction);
    }

    void InvokeAction()
    {
        if (UIManager.Instance == null) return;

        switch (action)
        {
            case Action.MainMenu:   UIManager.Instance.ShowMainMenu(); break;
            case Action.SelectMap:  UIManager.Instance.ShowSelectMap(); break;
            case Action.Store:      UIManager.Instance.ShowStoreBase(); break;
            case Action.Settings:   UIManager.Instance.SetPanel(UIManager.Instance.panelSettings); break;
        }
    }
}
