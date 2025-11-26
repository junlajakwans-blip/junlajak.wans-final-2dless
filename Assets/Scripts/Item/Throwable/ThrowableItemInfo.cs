using UnityEngine;

public class ThrowableItemInfo : MonoBehaviour
{
    public string PoolTag { get; private set; }
    public Sprite Icon { get; private set; }

    public bool CanInteract { get; private set; } = true;

    private Collider2D _col;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr; // ⬅️ NEW: เพิ่ม SpriteRenderer

    private void Awake()
    {
        TryGetComponent(out _col);
        TryGetComponent(out _rb);
        TryGetComponent(out _sr); // ⬅️ NEW: Get Component
    }

    public void SetInfo(string poolTag, Sprite icon)
    {
        PoolTag = poolTag;
        Icon = icon;

    // FIX: เปลี่ยน Sprite ทันทีที่ตั้งค่า
        if (_sr != null)
            _sr.sprite = icon; 
    }

    public void SetInteractable(bool active)
    {
        CanInteract = active;
    }

    public void DisablePhysicsOnHold()
    {
        SetInteractable(false);
        if (_col != null) _col.enabled = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0;
        }
    }

    public void EnablePhysicsOnThrow()
    {
        SetInteractable(false);
        if (_col != null) _col.enabled = true;
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1;
        }
    }

    public void OnReturnedToPool()
    {
        // FIX: ต้อง reset ให้พร้อม spawn ใหม่
        SetInteractable(true);
        if (_col != null) _col.enabled = true;
        
        // NEW: Reset Physics เป็น Dynamic/Gravity Scale 1 (ถ้าเป็น Prefab)
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ชนศัตรู → ทำดาเมจ
        if (collision.collider.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(20); // หรือตามค่าใน dropTable
            Debug.Log("[Throwable] Hit enemy!");
        }

        // ชนอะไรก็ได้ → คืนเข้ากอง
        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }

}
