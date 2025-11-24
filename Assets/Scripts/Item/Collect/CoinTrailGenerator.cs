using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้าง Coin Trail แบบสุ่ม pattern:
/// 1) Straight      - เส้นตรงตามแนว X
/// 2) StepUp        - ขั้นบันไดขึ้น
/// 3) StepDown      - ขั้นบันไดลง
/// 4) ZigZagStairs  - ขึ้น ๆ ลง ๆ แบบขั้นบันได
/// </summary>
public class CoinTrailGenerator : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private string _coinKey = "Coin"; // key ใน ObjectPoolManager
    [SerializeField] private int _minCoins = 6;
    [SerializeField] private int _maxCoins = 12;
    [SerializeField] private float _xStep = 1.5f;   // ระยะ X ระหว่างเหรียญแต่ละเหรียญ
    [SerializeField] private float _yStep = 1.0f;   // ระยะขั้นบันไดในแนว Y
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayDistance = 6f;
    [SerializeField] private float coinYOffset = 0.6f;


    [Header("Runtime")]
    [SerializeField] private ObjectPoolManager _pool;

    private void Start() // เปลี่ยนจาก Awake เป็น Start (เพื่อให้ Pool พร้อม)
    {
        if (_pool == null)
            _pool = FindFirstObjectByType<ObjectPoolManager>();
    }

    /// <summary>
    /// สั่งสร้าง Coin Trail แบบสุ่ม pattern จากจุดเริ่มต้น
    /// </summary>
    public void SpawnRandomTrail(Vector3 startPosition)
    {
        if (_pool == null)
        {
            Debug.LogError("[CoinTrailGenerator] ObjectPoolManager not found.");
            return;
        }

        int coinCount = Random.Range(_minCoins, _maxCoins + 1);
        int pattern = Random.Range(0, 4); // 0..3

        // ก่อนเริ่มสร้าง Coin Trail ต้องเคลียร์ Slot ที่ CollectibleSpawner จองไว้ก่อน
        // (เพราะ Coin Trail จะมาใช้ Slot นี้ต่อ)
        SpawnSlot.Unreserve(startPosition); 
        
        switch (pattern)
        {
            case 0:
                SpawnStraightTrail(startPosition, coinCount);
                break;
            case 1:
                SpawnStepUpTrail(startPosition, coinCount);
                break;
            case 2:
                SpawnStepDownTrail(startPosition, coinCount);
                break;
            case 3:
                SpawnZigZagTrail(startPosition, coinCount);
                break;
        }
    }

    #region Pattern Implementations
    // ... (ฟังก์ชัน SpawnStraightTrail, SpawnStepUpTrail, SpawnStepDownTrail, SpawnZigZagTrail เหมือนเดิม) ...
    
    private void SpawnStraightTrail(Vector3 start, int count)
    {
        Vector3 pos = start;
        for (int i = 0; i < count; i++)
        {
            TrySpawnCoin(pos);
            pos.x += _xStep;
        }
    }

    private void SpawnStepUpTrail(Vector3 start, int count)
    {
        Vector3 pos = start;
        for (int i = 0; i < count; i++)
        {
            TrySpawnCoin(pos);

            pos.x += _xStep;
            // ทุก ๆ 2 เหรียญ ขึ้น 1 ขั้น
            if (i % 2 == 1)
                pos.y += _yStep;
        }
    }

    private void SpawnStepDownTrail(Vector3 start, int count)
    {
        Vector3 pos = start;
        for (int i = 0; i < count; i++)
        {
            TrySpawnCoin(pos);

            pos.x += _xStep;
            if (i % 2 == 1)
                pos.y -= _yStep;
        }
    }

    private void SpawnZigZagTrail(Vector3 start, int count)
    {
        Vector3 pos = start;
        int dir = 1; // 1 = ขึ้น, -1 = ลง
        for (int i = 0; i < count; i++)
        {
            TrySpawnCoin(pos);

            pos.x += _xStep;

            if (i % 2 == 1)
            {
                pos.y += dir * _yStep;
                dir *= -1; // สลับขึ้น/ลง
            }
        }
    }

    #endregion

    /// <summary>
    /// Spawn เหรียญ 1 เหรียญที่ตำแหน่ง pos (เช็ค slot ก่อนกันทับของอื่น)
    /// </summary>
    private void TrySpawnCoin(Vector3 pos)
    {
        // 1. หาพื้นด้วย Raycast
        RaycastHit2D hit = Physics2D.Raycast(
            pos + Vector3.up * 2f,
            Vector2.down,
            groundRayDistance,
            groundLayer
        );

        Vector3 finalPos = pos;
        if (hit.collider != null)
        {
            finalPos.y = hit.point.y + coinYOffset; // วางให้อยู่บนพื้นพอดี
        }
        else
        {
            return; // ถ้าไม่เจอพื้น — ไม่ spawn กันลอย
        }

        // 2. กันทับของอื่น (ต้องจอง Slot ที่ตำแหน่งวางเหรียญจริงๆ)
        if (!SpawnSlot.Reserve(finalPos)) // ใช้ finalPos ที่ปรับ Y แล้ว
        {
             return;
        }

        // 3. Spawn
        GameObject coin = _pool.SpawnFromPool(_coinKey, finalPos, Quaternion.identity);
        
        if (coin == null)
        {
            // สำคัญ: ถ้า Spawn ไม่สำเร็จ ต้องคืน Slot
            SpawnSlot.Unreserve(finalPos); 
            return;
        }

        // 4. Setup
        coin.transform.SetParent(transform);
        coin.SetActive(true);
        
        // Note: ถ้า Coin Prefab มี CollectibleItem Script ติดอยู่
        // และต้อง Inject Dependency (CardManager/Spawner) เหมือน Item อื่น
        // ต้องเรียก CollectibleItem.SetDependencies() ที่นี่ด้วย
    }
}