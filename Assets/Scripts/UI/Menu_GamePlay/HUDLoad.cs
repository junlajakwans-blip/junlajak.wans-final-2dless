using UnityEngine;

public class HUDLoader : MonoBehaviour
{
    private static bool created = false;

    void Awake()
    {
        if (!created)
        {
            DontDestroyOnLoad(gameObject);
            created = true;
        }
        else
        {
            Destroy(gameObject); // ป้องกันซ้ำ
        }
    }
}
