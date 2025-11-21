using UnityEngine;
using UnityEngine.UI;

public class MuscleButton : MonoBehaviour
{
    public static MuscleButton Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Button _button;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_button == null)
            _button = GetComponent<Button>();

        _button.onClick.AddListener(OnClickMuscle);
        Hide(); // ซ่อนไว้ก่อนจนกว่าจะครบ 5 ใบ
    }

    private void OnClickMuscle()
    {
        // เรียกแลก 5 ใบ → MuscleDuck
        if (CardManager.Instance != null)
            CardManager.Instance.ActivateMuscleDuck();

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
