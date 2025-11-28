using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบจองช่องสำหรับ Spawn ป้องกันวัตถุทับกัน (เช่น Enemy ซ้อน Asset / Collectible ซ้อน Platform)
/// ใช้เป็น Grid 1x1 ด้วย Vector2Int (round จาก world position)
/// </summary>
public static class SpawnSlot
{
  // Grid-based slot map - เก็บ Slot Center ที่ถูกจองไว้แล้ว
  private static readonly HashSet<Vector2Int> _usedSlots = new HashSet<Vector2Int>();

    // Dictionary เพื่อช่วยจัดการ Unreserve ในกรณีที่มีการจองซ้อนกัน (เพื่อความปลอดภัย)
    private static readonly Dictionary<Vector2Int, int> _reservedCounts = new Dictionary<Vector2Int, int>();


  /// <summary>
  /// ขอจองตำแหน่ง spawn
  /// **ทำการตรวจสอบ Slot ที่ตำแหน่ง World Pos และ Slot ข้างเคียงในแนวแกน X**
  /// return true = จองสำเร็จ (ยังไม่มีใครจอง) → spawn ได้
  /// return false = มีวัตถุอื่นจองไปแล้ว → ห้าม spawn ตรงนี้
  /// </summary>
  public static bool Reserve(Vector3 worldPos)
  {
    int x = Mathf.RoundToInt(worldPos.x);
    int y = Mathf.RoundToInt(worldPos.y);

        // 1. ตรวจสอบ Slot ปัจจุบันและ Slot ข้างเคียงในแนวแกน X (3x1 area)
        for (int dx = -1; dx <= 1; dx++)
        {
            Vector2Int checkKey = new Vector2Int(x + dx, y);
            if (_usedSlots.Contains(checkKey))
            {
                // ถ้า Slot ปัจจุบันหรือข้างเคียงถูกจองแล้ว ให้ยกเลิก
                return false;
            }
        }
        
        // 2. ถ้าผ่านการตรวจสอบ ให้จอง Slot ปัจจุบัน (X, Y) เท่านั้น
        // (การตรวจสอบ 3x1 ในขั้นตอนที่ 1 เป็นการเพิ่มระยะห่าง)
        Vector2Int key = new Vector2Int(x, y);

        // 3. เพิ่ม Count และ Add เข้า HashSet
        if (_usedSlots.Add(key)) // ถ้าเพิ่มสำเร็จ (ยังไม่เคยถูกจอง)
        {
            _reservedCounts[key] = 1;
            return true;
        }

        // กรณีนี้ไม่ควรเกิดขึ้นถ้าใช้ _usedSlots.Add(key)
        return false;
  }

  /// <summary>
  /// ยกเลิกการจอง slot (ต้องเรียกเมื่อวัตถุถูก Despawn กลับเข้า Pool)
  /// </summary>
  public static void Unreserve(Vector3 worldPos)
  {
    Vector2Int key = new Vector2Int(
      Mathf.RoundToInt(worldPos.x),
      Mathf.RoundToInt(worldPos.y)
    );

        if (_reservedCounts.ContainsKey(key))
        {
            _reservedCounts[key]--;
            
            // ถ้า Count เป็น 0 ให้ลบออกจาก HashSet และ Dictionary
            if (_reservedCounts[key] <= 0)
            {
                _usedSlots.Remove(key);
                _reservedCounts.Remove(key);
            }
        }
  }
  
  /// <summary>
  /// ตรวจสอบว่า slot นี้ถูกจองไปแล้วหรือไม่
  /// </summary>
  public static bool IsReserved(Vector3 worldPos)
  {
    Vector2Int key = new Vector2Int(
      Mathf.RoundToInt(worldPos.x),
      Mathf.RoundToInt(worldPos.y)
    );

    return _usedSlots.Contains(key);
  }

  /// <summary>
  /// เคลียร์ช่องที่อยู่ "ไกลหลังผู้เล่น" ทิ้ง เพื่อลด memory
  /// ควรเรียกจาก MapGeneratorBase โดยอิงจากตำแหน่ง player (pivot)
  /// </summary>
  public static void ClearBehind(float pivotX, float keepDistance = 20f)
  {
    float minX = pivotX - keepDistance;
        
        // ใช้วิธีวนซ้ำแบบปลอดภัยเพื่อป้องกันการแก้ไข HashSet/Dictionary ขณะวนซ้ำ
        List<Vector2Int> keysToRemove = new List<Vector2Int>();
        foreach (var slot in _usedSlots)
        {
            if (slot.x < minX)
            {
                keysToRemove.Add(slot);
            }
        }

        foreach (var key in keysToRemove)
        {
            _usedSlots.Remove(key);
            _reservedCounts.Remove(key); 
        }
  }

  /// <summary>
  /// เคลียร์ทั้งหมด (เช่น เวลาเปลี่ยนฉากใหม่)
  /// </summary>
  public static void ClearAll()
  {
    _usedSlots.Clear();
        _reservedCounts.Clear();
  }
}