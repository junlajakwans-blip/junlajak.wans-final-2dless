using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบจองช่องสำหรับ Spawn ป้องกันวัตถุทับกัน (เช่น Enemy ซ้อน Asset / Collectible ซ้อน Platform)
/// ใช้เป็น Grid 1x1 ด้วย Vector2Int (round จาก world position)
/// </summary>
public static class SpawnSlot
{
    // Grid-based slot map
    private static readonly HashSet<Vector2Int> _usedSlots = new HashSet<Vector2Int>();

    /// <summary>
    /// ขอจองตำแหน่ง spawn
    /// return true = จองสำเร็จ (ยังไม่มีใครจอง) → spawn ได้
    /// return false = มีวัตถุอื่นจองไปแล้ว → ห้าม spawn ตรงนี้
    /// </summary>
    public static bool Reserve(Vector3 worldPos)
    {
        Vector2Int key = new Vector2Int(
            Mathf.RoundToInt(worldPos.x),
            Mathf.RoundToInt(worldPos.y)
        );

        if (_usedSlots.Contains(key))
            return false;

        _usedSlots.Add(key);
        return true;
    }

    /// <summary>
    /// เคลียร์ช่องที่อยู่ "ไกลหลังผู้เล่น" ทิ้ง เพื่อลด memory
    /// ควรเรียกจาก MapGeneratorBase โดยอิงจากตำแหน่ง player (pivot)
    /// </summary>
    public static void ClearBehind(float pivotX, float keepDistance = 20f)
    {
        float minX = pivotX - keepDistance;
        _usedSlots.RemoveWhere(slot => slot.x < minX);
    }

    /// <summary>
    /// เคลียร์ทั้งหมด (เช่น เวลาเปลี่ยนฉากใหม่)
    /// </summary>
    public static void ClearAll()
    {
        _usedSlots.Clear();
    }
}
