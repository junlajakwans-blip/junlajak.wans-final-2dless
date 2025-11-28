using System.Collections.Generic;
using UnityEngine;
// ต้องแน่ใจว่าได้อ้างอิงถึง namespace ของ MapType และ GameManager ถูกต้อง
using DuffDuck.Stage; 

public class BackgroundLooper : MonoBehaviour
{
    // การตั้งค่าการ Loop ฉากหลัง
    [Header("Looping Settings")]
    [Tooltip("ความกว้างของฉากหลัง 1 ชิ้นใน World Unit (เช่น 19.2f)")]
    [SerializeField] private float _backgroundWidth = 19.2f;
    [Tooltip("ระยะห่างจากแกน Y (ความสูง) ที่จะวางฉากหลัง")]
    [SerializeField] private float _backgroundYOffset = 0.2f;
    [Tooltip("ระยะห่างจากกล้องที่ฉากหลังต้องขยับออกไป จึงจะถูก Loop กลับมา (20.0f ช่วยแก้ปัญหา Flicker)")]
    [SerializeField] private float _destroyThreshold = 20.0f; 

    // การควบคุมการเคลื่อนไหวเพื่อหยุด/เริ่ม Loop เมื่อกล้องหยุดนิ่ง
    [Header("Movement Control")]
    [Tooltip("ระยะทางต่ำสุดที่กล้องต้องขยับใน 1 Frame เพื่อถือว่ามีการเคลื่อนไหว (เช่น 0.01f)")]
    [SerializeField] private float _minMoveThreshold = 0.01f;
    [Tooltip("เวลา (วินาที) ที่กล้องต้องหยุดนิ่งก่อนที่จะหยุดการ Loop ฉากหลัง")]
    [SerializeField] private float _stationaryWaitTime = 0.5f;

    [Header("Background Type Key")]
    [SerializeField] private string _currentBackgroundKey = "default";

    private const int BG_COUNT = 3;
    private readonly List<GameObject> _backgroundLayers = new();
    private Transform _cameraTransform;
    
    // ตัวแปรสำหรับตรวจสอบการเคลื่อนไหว
    private float _lastCameraX;
    private float _stationaryTimer;
    private bool _isCameraMoving = false; // เริ่มต้นให้เป็น false จนกว่าจะมีการเคลื่อนไหว

    private void Start()
    {
        // พยายามดึง Transform ของกล้องหลัก
        _cameraTransform = Camera.main?.transform;
        if (_cameraTransform == null)
        {
            Debug.LogError("[BackgroundLooper] ❌ Main Camera ไม่พบ! โปรดตรวจสอบแท็ก 'MainCamera' ใน Scene.");
        }
        else
        {
            // กำหนดตำแหน่ง X เริ่มต้น
            _lastCameraX = _cameraTransform.position.x;
        }
    }

    private void OnEnable()
    {
        // สมัครรับ Event เมื่อเกมพร้อมทำงาน
        GameManager.OnGameReady += HandleGameReady;
    }

    private void OnDisable()
    {
        // ยกเลิกรับ Event เมื่อ Script ถูกปิดใช้งาน
        GameManager.OnGameReady -= HandleGameReady;
    }

    private void HandleGameReady()
    {
        SetBackground(_currentBackgroundKey);
    }

    private void Update()
    {
        // Guard Clause: ออกจากการทำงานถ้าไม่มีกล้องหรือฉากหลังยังไม่ถูกสร้าง
        if (_cameraTransform == null || _backgroundLayers.Count == 0) return;

        // 1. ตรวจสอบการเคลื่อนไหวของกล้องทุก Frame
        CheckCameraMovement();

        // 2. ถ้ากล้องมีการเคลื่อนที่ (หรือหยุดไม่นานเกินไป) จึงจะทำการ Loop
        if (_isCameraMoving)
        {
            UpdateBackgroundPosition();
        }
    }

    /// <summary>
    /// ตรวจสอบว่ากล้องเคลื่อนที่หรือไม่ และปรับสถานะ _isCameraMoving
    /// </summary>
    private void CheckCameraMovement()
    {
        float currentCameraX = _cameraTransform.position.x;
        // คำนวณระยะการเคลื่อนที่ใน Frame นี้
        float movement = Mathf.Abs(currentCameraX - _lastCameraX);

        if (movement > _minMoveThreshold)
        {
            // กล้องขยับ: รีเซ็ตตัวนับและตั้งค่าให้มีการ Loop
            _isCameraMoving = true;
            _stationaryTimer = 0f;
        }
        else
        {
            // กล้องไม่ขยับ: เริ่มนับเวลา
            _stationaryTimer += Time.deltaTime;
            if (_stationaryTimer >= _stationaryWaitTime)
            {
                // หยุด Loop ถ้าหยุดนิ่งนานพอ
                _isCameraMoving = false;
            }
        }

        // อัพเดทตำแหน่งกล้องล่าสุดสำหรับ Frame ถัดไป
        _lastCameraX = currentCameraX;
    }


    /// <summary>
    /// ปรับตำแหน่งของฉากหลังเพื่อสร้างการ Loop แบบไร้รอยต่อ
    /// </summary>
    private void UpdateBackgroundPosition()
    {
        float cameraX = _cameraTransform.position.x;
        // ระยะทางการเคลื่อนย้ายที่ถูกต้องสำหรับระบบ 3 ชิ้น (3 * BackgroundWidth)
        float moveDist = _backgroundWidth * _backgroundLayers.Count; 

        foreach (var layer in _backgroundLayers)
        {
            if (layer == null) continue;

            float bgX = layer.transform.position.x;
            
            // ตรวจสอบ: ฉากหลังหลุดไปทางซ้ายเกิน Threshold หรือไม่? (กล้องอยู่ไกลกว่าฉากหลังมาก)
            if (cameraX - bgX > _destroyThreshold)
            {
                // เลื่อนฉากหลังนี้ไปต่อด้านขวา
                layer.transform.position += new Vector3(moveDist, 0f, 0f);
            }
            // ตรวจสอบ: ฉากหลังหลุดไปทางขวาเกิน Threshold หรือไม่? (ฉากหลังอยู่ไกลกว่ากล้องมาก)
            // ใช้ 'else if' เพื่อให้แน่ใจว่ามีการเคลื่อนที่เพียงครั้งเดียวต่อ Frame (สำคัญในการแก้ปัญหา Flicker)
            else if (bgX - cameraX > _destroyThreshold)
            {
                // เลื่อนฉากหลังนี้ไปต่อด้านซ้าย
                layer.transform.position -= new Vector3(moveDist, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// กำหนดฉากหลังใหม่โดยการเปลี่ยน Pool Key
    /// </summary>
    public void SetBackground(string backgroundKey)
    {
        // ตรรกะการเลือกฉากหลังตาม MapType เดิม
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

        // หาก Background ไม่ได้เปลี่ยนและมี Layer อยู่แล้ว ไม่ต้องทำซ้ำ
        if (_currentBackgroundKey == backgroundKey && _backgroundLayers.Count > 0)
            return;

        _currentBackgroundKey = backgroundKey;
        SpawnBackgroundLayers(backgroundKey);
    }

    /// <summary>
    /// สร้างฉากหลัง 3 ชิ้นจาก Object Pool และจัดเรียงให้ตรงกับตำแหน่งกล้องปัจจุบัน
    /// </summary>
    private void SpawnBackgroundLayers(string poolKey)
    {
        // 1. คืนพื้นหลังเก่าเข้าพูลก่อน
        foreach (var bg in _backgroundLayers)
            if (bg) ObjectPoolManager.Instance.ReturnToPool(GetObjectTag(bg), bg);

        _backgroundLayers.Clear();

        if (_cameraTransform == null) return;

        // 2. คำนวณตำแหน่งสำหรับวาง 3 รูป
        float centerX = _cameraTransform.position.x;

        float firstX = centerX - _backgroundWidth;     // ซ้ายสุด
        float secondX = centerX;                       // กลาง
        float thirdX = centerX + _backgroundWidth;     // ขวา
        float[] positions = { firstX, secondX, thirdX };

        // 3. Spawn และกำหนดค่า
        foreach (float px in positions)
        {
            Vector3 spawnPos = new Vector3(px, _backgroundYOffset, 0);
            // สมมติว่า ObjectPoolManager.Instance.SpawnFromPool พร้อมใช้งาน
            var bg = ObjectPoolManager.Instance.SpawnFromPool(poolKey, spawnPos, Quaternion.identity);

            if (!bg)
            {
                Debug.LogError($"❌ ไม่พบ BG prefab ใน Pool Key: {poolKey}");
                return; 
            }

            bg.transform.SetParent(transform);

            // ตั้งค่า Sorting Layer
            if (bg.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerName = "Background";
                sr.sortingOrder = -10;
            }

            _backgroundLayers.Add(bg);
        }
    }

    /// <summary>
    /// ดึง Tag (ชื่อ Prefab เดิม) จาก GameObject ที่โคลนมา
    /// </summary>
    private string GetObjectTag(GameObject obj)
    {
        return obj.name.Replace("(Clone)", "").Trim();
    }
}