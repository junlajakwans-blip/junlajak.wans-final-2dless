using UnityEngine;
using UnityEngine.UI;

public class UIButton_StoreSelect : MonoBehaviour
{
    public enum Action { Upgrade, Exchange, Back }
    [SerializeField] Action action;

    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(InvokeAction);
    }

    void InvokeAction()
    {
        if (StoreUI.Instance == null)
        {
            Debug.LogError("❌ StoreUI Instance missing");
            return;
        }

        switch (action)
        {
            case Action.Upgrade:   StoreUI.Instance.OpenUpgrade(); break;
            case Action.Exchange:  StoreUI.Instance.OpenExchange(); break;
            case Action.Back:      UIManager.Instance.ShowMainMenu(); break;
        }
    }
}
