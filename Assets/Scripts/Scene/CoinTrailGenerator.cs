using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สร้าง Coin Trail แบบสุ่ม pattern:
/// 1) Straight      - เส้นตรงตามแนว X
/// 2) StepUp        - ขั้นบันไดขึ้น
/// 3) StepDown      - ขั้นบันไดลง
/// 4) ZigZagStairs  - ขึ้น ๆ ลง ๆ แบบขั้นบันได
/// </summary>
public class CoinTrailGenerator : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private string _coinKey = "Coin"; // key ใน ObjectPoolManager
    [SerializeField] private int _minCoins = 6;
    [SerializeField] private int _maxCoins = 12;
    [SerializeField] private float _xStep = 1.5f;   // ระยะ X ระหว่างเหรียญแต่ละเหรียญ
    [SerializeField] private float _yStep = 1.0f;   // ระยะขั้นบันไดในแนว Y

    [Header("Runtime")]
    [SerializeField] private ObjectPoolManager _pool; // ถ้าไม่เซ็ต จะ Find ตอน Start

    private void Awake()
    {
        if (_pool == null)
            _pool = FindFirstObjectByType<ObjectPoolManager>();
    }

    /// <summary>
    /// สั่งสร้าง Coin Trail แบบสุ่ม pattern จากจุดเริ่มต้น
    /// ใช้เวลา Player วิ่งมาถึงจุดที่เหมาะ เช่น platform ใหม่ หรือช่วงว่าง ๆ
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
        // กันทับ Enemy / Asset / Platform / Floor อื่น ๆ
        if (!SpawnSlot.Reserve(pos))
            return;

        GameObject coin = _pool.SpawnFromPool(_coinKey, pos, Quaternion.identity);
        if (coin == null)
        {
            Debug.LogError($"[CoinTrailGenerator] Coin prefab not found for key '{_coinKey}'.");
            return;
        }

        coin.transform.SetParent(transform);
        coin.SetActive(true);
    }
}
