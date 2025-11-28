using System.Collections.Generic;
using UnityEngine;
using DuffDuck.Stage;

public class BackgroundLooper : MonoBehaviour
{
    [Header("Looping Settings")]
    [Tooltip("ความกว้างจริงของรูป Background 1 รูป (หน่วย World Space)")]
    [SerializeField] private float _backgroundWidth = 19.2f; //  เช็คให้ตรงกับขนาดรูปจริง
    [SerializeField] private float _backgroundYOffset = 0.2f;
    
    [Tooltip("ระยะห่างจากกล้องทางซ้าย ที่จะให้ย้ายรูปไปข้างหน้า")]
    [SerializeField] private float _destroyThreshold = 25f;

    [Header("Background Type Key")]
    [SerializeField] private string _currentBackgroundKey = "default";

    // ใช้ 3 รูป (ซ้าย-กลาง-ขวา) เพื่อความเนียนเวลาวิ่งเร็วๆ
    private const int BG_COUNT = 3; 
    
    private readonly List<GameObject> _backgroundLayers = new();
    private Transform _cameraTransform;

    // =========================================================
    // LIFECYCLE
    // =========================================================
    private void Start()
    {

        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
        else
            Debug.LogError("[BackgroundLooper] Main Camera not found!");
    }

    private void OnEnable()
    {
        GameManager.OnGameReady += HandleGameReady;
    }

    private void OnDisable()
    {
        GameManager.OnGameReady -= HandleGameReady;
    }

    private void HandleGameReady()
    {
        SetBackground(_currentBackgroundKey);
    }

    private void Update()
    {
        if (_cameraTransform == null || _backgroundLayers.Count == 0) return;

     
        UpdateBackgroundPosition();
    }

    // =========================================================
    // LOGIC: ย้ายพื้นหลังไปดักหน้า (Leapfrog)
    // =========================================================
    private void UpdateBackgroundPosition()
    {
        float cameraX = _cameraTransform.position.x;

        foreach (var layer in _backgroundLayers)
        {
            if (layer == null) continue;

            float bgX = layer.transform.position.x;

            // 🟢 หลุดซ้าย → ย้ายไปขวา
            if (bgX < cameraX - _destroyThreshold)
            {
                float moveDist = _backgroundWidth * _backgroundLayers.Count;
                layer.transform.position += new Vector3(moveDist, 0f, 0f);
            }
            // 🔵 หลุดขวา → ย้ายไปซ้าย (ขากลับ)
            else if (bgX > cameraX + _destroyThreshold)
            {
                float moveDist = _backgroundWidth * _backgroundLayers.Count;
                layer.transform.position -= new Vector3(moveDist, 0f, 0f);
            }
        }
    }

    // =========================================================
    // SPAWN / SETUP
    // =========================================================
    public void SetBackground(string backgroundKey)
    {
        // Mapping ชื่อ BG ให้ตรงกับ MapType
        if (backgroundKey == "default" && GameManager.Instance != null)
        {
            backgroundKey = GameManager.Instance.CurrentMapType switch
            {
                MapType.School      => "map_bg_School",
                MapType.RoadTraffic => "map_bg_RoadTraffic",
                MapType.Kitchen     => "map_bg_Kitchen",
                _                   => "map_bg_School"
            };
        }

        if (_currentBackgroundKey == backgroundKey && _backgroundLayers.Count > 0)
            return;

        _currentBackgroundKey = backgroundKey;
        SpawnBackgroundLayers(backgroundKey);
    }

    /// <summary>
    /// จัดการคืนของเก่าและ Spawn พื้นหลังใหม่ตาม Pool Key
    /// </summary>
    private void SpawnBackgroundLayers(string poolKey)
    {
        // 1. คืนของเก่าเข้า Pool
        foreach (var bg in _backgroundLayers)
        {
            if (bg) 
            {
                // 🔥 FIX: ใช้ GetObjectTag() เพื่อให้ได้ชื่อ Prefab ที่ถูกต้อง
                ObjectPoolManager.Instance.ReturnToPool(GetObjectTag(bg), bg); 
            }
        }
        _backgroundLayers.Clear();

        // 2. 🔥 FIX: หาจุดเริ่มวาง (Start X)
        // ใช้ตำแหน่งขอบซ้ายของกล้องเป็นเกณฑ์
        float startX = 0f; 
        if (_cameraTransform != null)
        {
            // คำนวณขอบซ้ายของกล้อง + ระยะที่กำหนดไว้ (_destroyThreshold) 
            // แล้ววางรูปแรกให้อยู่ตรงกลางของตำแหน่งนั้น
            float cameraLeft = _cameraTransform.position.x - _destroyThreshold;
            
            // รูปแรกควรวางตำแหน่ง ณ ขอบซ้ายของกล้อง + ครึ่งหนึ่งของความกว้างรูป
            // เพื่อให้รูปแรกครอบคลุมพื้นที่ที่กล้องมองเห็น
            startX = cameraLeft + (_backgroundWidth / 2f); 
        }

        // 3. Spawn ใหม่ 3 รูป เรียงต่อกัน
        for (int i = 0; i < BG_COUNT; i++)
        {
            // ตำแหน่งของรูปที่ i จะห่างจาก startX เป็นระยะ i * _backgroundWidth
            Vector3 spawnPos = new Vector3(startX + (i * _backgroundWidth), _backgroundYOffset, 0);

            var bg = ObjectPoolManager.Instance.SpawnFromPool(poolKey, spawnPos, Quaternion.identity);

            if (!bg)
            {
                Debug.LogError($"❌ BG prefab not found in pool: {poolKey}");
                // หยุด Loop ทันที
                return;
            }

            bg.transform.SetParent(transform);

            // ตั้งค่า Layer ให้แน่ใจว่าอยู่ข้างหลังสุด
            if (bg.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -10;
            }

            _backgroundLayers.Add(bg);
        }
    }
        
    private string GetObjectTag(GameObject obj)
    {
        return obj.name.Replace("(Clone)", "").Trim();
    }
}