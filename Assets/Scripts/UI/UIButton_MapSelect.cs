using UnityEngine;
using UnityEngine.UI;

public class UIButton_MapSelect : MonoBehaviour
{
    public enum Action { Next, Prev, Play, Back }
    [SerializeField] Action action;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(InvokeAction);
    }

    void InvokeAction()
    {
        if (MapSelectController.Instance == null) return;

        switch (action)
        {
            case Action.Next: UIManager.Instance.MapNext(); break;
            case Action.Prev: UIManager.Instance.MapPrev(); break;
            case Action.Play: UIManager.Instance.PlaySelectedMap(); break;
            case Action.Back: UIManager.Instance.ShowMainMenu(); break;
        }
    }
}
