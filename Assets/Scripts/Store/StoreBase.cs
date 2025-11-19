using System.Collections.Generic;
using UnityEngine;

public abstract class StoreBase : MonoBehaviour
{
    public abstract string StoreName { get; }
    public abstract StoreType StoreType { get; }

    // รายการสินค้า → ถูก Inject จาก StoreManager ตามประเภท (Exchange / Upgrade / Map)
    [SerializeField] protected List<StoreItem> items = new List<StoreItem>();
    public IReadOnlyList<StoreItem> Items => items;

    protected StoreManager _manager;

    /// <summary>
    /// Inject StoreManager + รายการ StoreItem จาก StoreManager
    /// </summary>
    public virtual void Initialize(StoreManager manager, List<StoreItem> injectedItems)
    {
        _manager = manager;

        items.Clear();
        if (injectedItems != null)
            items.AddRange(injectedItems);
    }

    /// <summary>
    /// ค้นหา item จาก itemID — ใช้เวลาเรียกซื้อผ่าน StoreManager.Purchase
    /// </summary>
    public virtual StoreItem GetItem(string id)
    {
        return items.Find(i => i != null && i.ID == id);
    }

    /// <summary>
    /// UI จะเรียกฟังก์ชันนี้ตอนกดซื้อ
    /// </summary>
    public abstract bool Purchase(StoreItem item);

    /// <summary>
    /// ไว้ให้ร้านที่ Item ซื้อครั้งเดียว override เช่น Map / Upgrade
    /// </summary>
    public virtual bool IsUnlocked(StoreItem item) => false;
}
