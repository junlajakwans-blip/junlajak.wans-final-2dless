using UnityEngine;
using System;

/// <summary>
/// Endless Wall — pushes player forward and kills on contact.
/// Works with Object Pool & MapGeneratorBase.
/// </summary>
/// 
/// [คำอธิบาย]: ควบคุมการเคลื่อนที่ของกำแพงที่ไล่ผู้เล่นจากด้านหลัง กำแพงจะเร่งความเร็วเมื่อผู้เล่นอยู่ไกล
/// เพื่อบีบให้ผู้เล่นเคลื่อนที่ไปข้างหน้าตลอดเวลา และป้องกันการถอยหลัง

namespace DuffDuck.Stage
{
[RequireComponent(typeof(Collider2D))]
public class WallPushController : MonoBehaviour
{
  [Header("Speed Growth")]

  [Header("Settings")]
  [SerializeField] private float _pushSpeed = 1.0f; // overwritten by MapGenerator
  [SerializeField] private bool _isPushing = true;
  [SerializeField] private float safeDistance = 3f;

  [Header("Runtime")]

  private Player[] _players;
  private float _refreshTimer = 0f;

  
  private void LateUpdate()
  {
    if (!_isPushing) return;

    var center = GetCenterPosition();
    if (_players == null || _players.Length == 0) return;

        // 1. คำนวณความห่างระหว่าง Player กับกำแพง
    float dist = Mathf.Max(0f, center.x - transform.position.x - safeDistance); // ความห่าง
    
        // 2. คำนวณค่า t (0 ถึง 1) โดยให้ 45 หน่วย เป็นระยะที่กำแพงต้องวิ่งเต็มสปีด
    float t = Mathf.Clamp01(dist / 45f);           // ระยะ 45 หน่วย = ไล่เต็มสปีด

        // 3. กำหนดความเร็วเป้าหมายตามความห่าง (ใช้ Lerp แบบนุ่มนวล)
    float minSpeed = 1.0f;  // ความเร็วต่ำสุด (เมื่อ Player อยู่ใกล้กำแพง)
    float maxSpeed = 7.5f;  // ความเร็วสูงสุด (เมื่อ Player ทิ้งห่าง)
    float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

    // 4. ปรับความเร็วปัจจุบันเข้าใกล้เป้าหมายแบบนิ่มนวล
    _pushSpeed = Mathf.MoveTowards(_pushSpeed, targetSpeed, Time.deltaTime * 4f);

        // 5. ขยับกำแพงไปข้างหน้า
    transform.Translate(Vector3.right * _pushSpeed * Time.deltaTime);
  }

  private Vector3 GetCenterPosition()
  {
      _refreshTimer -= Time.deltaTime;

      if (_players == null || _players.Length == 0 || _refreshTimer <= 0f)
      {
          _players = FindObjectsByType<Player>(FindObjectsSortMode.None);
          _refreshTimer = 0.5f;
      }

      if (_players == null || _players.Length == 0) return Vector3.zero;

      Vector3 sum = Vector3.zero;

      foreach (var p in _players)
      {
          sum += p.transform.position;
      }

      return sum / _players.Length;
  }

  /// <summary>
  /// Called by MapGeneratorBase to sync speed and toggle state
  /// </summary>
  public void SetPushState(float speed, bool enabled)
  {
    // รับค่า Base Speed มาจาก MapGeneratorBase (ใช้สำหรับบัฟ/ดีบัฟ)
    // NOTE: โค้ดใน LateUpdate จะปรับ _pushSpeed นี้ตามระยะห่างจาก Player อีกที
    _pushSpeed = speed; 
    _isPushing = enabled;
    
    // Invoke event ทันทีที่ความเร็วเปลี่ยน
    OnWallSpeedChanged?.Invoke(_pushSpeed);
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.TryGetComponent<Player>(out var player))
    {
      player.TakeDamage(9999); // instant kill
      Debug.Log("[Wall] Player touched the wall → instant death.");
    }
  }

  public static event Action<float> OnWallSpeedChanged;
}
}