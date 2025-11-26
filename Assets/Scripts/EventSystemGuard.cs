using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemGuard : MonoBehaviour
{
    void Awake()
    {
        var systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (systems.Length > 1)
        {
            Destroy(gameObject);
        }
    }
}
