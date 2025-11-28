using UnityEngine;
using UnityEngine.UI;

public class UIButton_Play : MonoBehaviour
{
    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnPlay);
    }

    void OnPlay()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSelectMap();
        else
            Debug.LogError("UIManager INSTANCE NOT FOUND");
    }
}
