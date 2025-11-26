using UnityEngine;

public class ThrowableItemInfo : MonoBehaviour, IInteractable   // ⬅️ IMPLEMENT
{
    public string PoolTag { get; private set; }
    public Sprite Icon { get; private set; }

    public bool CanInteract { get; private set; } = true;

    private Collider2D _col;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private void Awake()
    {
        TryGetComponent(out _col);
        TryGetComponent(out _rb);
        TryGetComponent(out _sr);
    }

    public void SetInfo(string poolTag, Sprite icon)
    {
        PoolTag = poolTag;
        Icon = icon;

        if (_sr != null)
            _sr.sprite = icon;
    }

    public void SetInteractable(bool active)
    {
        CanInteract = active;
    }

    // ⬅️ REQUIRED FOR PICK UP
    public void Interact(Player player)
    {
        if (!CanInteract) return;
        if (player == null) return;

        var interact = player.GetComponent<PlayerInteract>();
        if (interact != null)
        {
            interact.SetThrowable(gameObject);
        }
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

        // ✅ FIX: กำหนด Local Scale เมื่อถูกถือ (เพื่อให้มีขนาดเล็ก)
        transform.localScale = new Vector3(0.2f, 0.2f, 1f); 
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
        
        // ✅ FIX: กำหนด Local Scale เมื่อถูกปา (เพื่อให้มีขนาดเล็ก)
        transform.localScale = new Vector3(0.2f, 0.2f, 1f); 
    }

    public void OnReturnedToPool()
    {
        SetInteractable(true);
        if (_col != null) _col.enabled = true;

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1;
        }
        
        // ✅ FIX: รีเซ็ต Local Scale กลับไปเป็นขนาดเล็กเมื่อคืน Pool
        transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        
        // 💡 แนะนำ: Unparent
        transform.SetParent(null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        // 1. ชนศัตรู → ดาเมจ (และตั้งค่า Flag)
        if (collision.collider.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(20);
        }

        // 2. ชนอะไรก็ได้ → คืน pool
        // ไอเทมควรคืน Pool เสมอหลังจากการชนครั้งแรก
        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }

    public void ShowPrompt()
    {
        throw new System.NotImplementedException();
    }
}