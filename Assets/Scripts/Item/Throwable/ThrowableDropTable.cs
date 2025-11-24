using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ThrowableDropTable", menuName = "DUFFDUCK/ThrowableDropTable")]
public class ThrowableDropTable : ScriptableObject
{
    public MapType mapType;

    [System.Serializable]
    public class DropEntry
    {
        [HideInInspector] public string id;   // auto สร้าง
        public string assetName;              // ชื่อ asset เช่น "Bento", "Chair", "Burger"
        public string poolTag;                // *ต้องตรงกับ ObjectPool key*
        public float weight = 1f;
        public Sprite icon;                   // รูปไอเท็มสำหรับ UI
    }

    public List<DropEntry> dropList = new List<DropEntry>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < dropList.Count; i++)
        {
            var e = dropList[i];
            if (e == null) continue;

            // สร้างชื่อ ID ให้ตรง Format เดิม
            e.id = $"map_ThrowItem_{mapType}_{e.assetName}_{i}";
        }
    }
#endif
}
