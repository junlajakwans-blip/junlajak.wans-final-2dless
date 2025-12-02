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
            case Action.Next: MapSelectController.Instance.NextMap(); break;
            case Action.Prev: MapSelectController.Instance.PrevMap(); break;
            case Action.Play: MapSelectController.Instance.TryPlaySelectedMap(); break;
            case Action.Back: MapSelectController.Instance.BackToMainMenu(); break;
        }
    }
}
