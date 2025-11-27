using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int _damageAmount = 2;

    [Header("Lifetime")]
    [SerializeField] private float _lifetime = 3f;

    /// <summary>
    /// กลับเข้า Pool โดยอัตโนมัติ (อ่านชื่อ Prefab)
    /// ไม่ต้อง SetPoolTag จาก Enemy อีก
    /// </summary>
    public string PoolTag { get; private set; }

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }
    }

    private void OnEnable()
    {
        // ตรวจ poolTag อัตโนมัติ
        if (string.IsNullOrEmpty(PoolTag))
        {
            PoolTag = gameObject.name.Replace("(Clone)", "").Trim();
        }

        // reset physics
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        CancelInvoke(nameof(Despawn));
        Invoke(nameof(Despawn), _lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Despawn));
    }

    public void SetDamage(int amount)
    {
        _damageAmount = amount;
    }

    // enemy / player hit
    private void OnTriggerEnter2D(Collider2D other)
    {
        // เข้า Player → โดน
        if (other.TryGetComponent<Player>(out var player))
        {
            player.TakeDamage(_damageAmount);
            Despawn();
            return;
        }

        // เข้า Enemy → ถ้ามาจากศัตรู อย่าทำ Damage ฝั่งเดียวกัน
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            // ยิงโดนเพื่อนศัตรู? ให้ลอยผ่าน
            return;
        }

        // ชนพื้นหรือกำแพง → แตก
        if (other.CompareTag("Ground") || other.CompareTag("Obstacle"))
        {
            Despawn();
            return;
        }
    }

    private void Despawn()
    {
        if (!gameObject.activeInHierarchy) return;

        if (!string.IsNullOrEmpty(PoolTag) && ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
        }
        else
        {
            // เผื่อเผลอยิง projectile ที่ไม่ได้อยู่ใน Pool
            Destroy(gameObject);
        }
    }
}
