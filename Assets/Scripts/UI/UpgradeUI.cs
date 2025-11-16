using UnityEngine;
using System.Collections.Generic;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private List<GameObject> hpUpgradeIcons; // 5 icons
    private StoreUpgrade _store;

    public void Initialize(StoreUpgrade store)
    {
        _store = store;
        Refresh();
    }

    public void Refresh()
    {
        if (_store == null) return;

        int level = _store.GetWheyLevelForUI();  
        level = Mathf.Clamp(level, 0, hpUpgradeIcons.Count);

        for (int i = 0; i < hpUpgradeIcons.Count; i++)
            hpUpgradeIcons[i].SetActive(i < level);
    }
}
