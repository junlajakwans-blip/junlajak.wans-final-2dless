using UnityEngine;
using UnityEngine.UI;

public class UIButton_GameMode : MonoBehaviour
{
        [SerializeField] private GameModeManager.GameMode mode = GameModeManager.GameMode.Solo;


    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(InvokeAction);
    }

    private void InvokeAction()
    {
        if (GameModeSelector.Instance == null)
        {
            Debug.LogWarning("[UIButton_GameMode] GameModeSelector not found!");
            return;
        }

        GameModeSelector.Instance.SetMode(mode);
    }
}