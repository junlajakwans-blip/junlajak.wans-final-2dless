using UnityEngine;
using System.Collections.Generic;

public class UpgradeUI : MonoBehaviour
{
    [Header("Template Icon (turn OFF in hierarchy)")]
    [SerializeField] private GameObject iconTemplate;

    [Header("Parent that holds cloned icons")]
    [SerializeField] private Transform iconContainer;

    private readonly List<GameObject> spawned = new();
    private StoreUpgrade _storeUpgrade;
    private StoreItem _item;

    public void Init(StoreUpgrade store, StoreItem item)
    {
        _storeUpgrade = store;
        _item = item;
        Refresh();
    }

    public void Refresh()
    {
        if (_storeUpgrade == null || _item == null) return;

        // Clear old icons
        foreach (var obj in spawned)
            Destroy(obj);
        spawned.Clear();

        int max = _item.MaxLevel;
        int level = _storeUpgrade.GetLevel(_item);

        for (int i = 0; i < max; i++)
        {
            GameObject clone = Instantiate(iconTemplate, iconContainer);
            clone.SetActive(true);

            var image = clone.GetComponentInChildren<UnityEngine.UI.Image>();
            image.color = (i < level) ? Color.white : new Color(1,1,1,0.25f);

            spawned.Add(clone);
        }
    }


}
