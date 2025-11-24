using UnityEngine;

public class CoinMagnet : MonoBehaviour
{
    [SerializeField] private float detectRadius = 4f;
    [SerializeField] private float pullSpeed = 9f;
    [SerializeField] private AnimationCurve speedCurve;

    private Transform _playerTransform; 
    private bool _isPulled = false;
    private float _timePulled = 0f; 

    private void Awake()
    {
        // ค้นหา Player ใน Awake อย่างปลอดภัย
        _playerTransform = FindFirstObjectByType<Player>()?.transform; 
        
        if (_playerTransform != null)
        {
            Debug.Log("[Magnet] Player transform successfully cached in Awake.");
        }
    }
    
    /// <summary>
    /// รีเซ็ตสถานะทุกครั้งที่ Object ถูกดึงกลับมาใช้งานจาก Pool
    /// </summary>
    private void OnEnable()
    {
        _isPulled = false;
        _timePulled = 0f;
        Debug.Log($"[Magnet] State reset on OnEnable. Pulled={_isPulled}.");
        
        // เช็ค Player อีกครั้งเผื่อ Awake ไม่ทำงาน
        if (_playerTransform == null)
        {
            _playerTransform = FindFirstObjectByType<Player>()?.transform;
            if (_playerTransform != null)
            {
                Debug.Log("[Magnet] Player transform FOUND in OnEnable fallback.");
            }
        }
    }

    private void Update()
    {
        if (_playerTransform == null) 
        {
            return;
        }

        // 1. เริ่มถูกดึงเมื่อเข้าในรัศมี
        if (!_isPulled)
        {
            float distance = Vector2.Distance(transform.position, _playerTransform.position);
            
            if (distance <= detectRadius)
            {
                _isPulled = true;
                _timePulled = 0f; 
                Debug.Log($"[Magnet] Pull ACTIVATED! Distance={distance:F2} (Threshold={detectRadius})."); // 🔥 Log เมื่อเริ่มดึง
            }
            else
            {
                return;
            }
        }

        // 2. เคลื่อนที่เข้าหา Player
        _timePulled += Time.deltaTime;
        float evaluatedTime = Mathf.Clamp01(_timePulled);
        float spd = pullSpeed * speedCurve.Evaluate(evaluatedTime);

        transform.position = Vector2.MoveTowards(
            transform.position,
            _playerTransform.position,
            spd * Time.deltaTime
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ตรวจสอบว่าชนกับ Player
        if (other.TryGetComponent<Player>(out var player))
        {
            // ให้ CollectibleItem จัดการการเก็บและการ Despawn (Unreserve Slot)
            if (TryGetComponent<CollectibleItem>(out var item))
            {
                item.Collect(player);
                Debug.Log($"[Magnet] Coin collected by Player via Magnet Trigger at X={transform.position.x:F2}."); // 🔥 Log เมื่อเก็บสำเร็จ
            }
            else
            {
                Debug.LogError($"[Magnet] Coin at {transform.position:F2} is missing CollectibleItem script!");
            }
        }
    }
}