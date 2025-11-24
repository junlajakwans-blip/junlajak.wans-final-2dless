using UnityEngine;

public class ThrowableItemInfo : MonoBehaviour
{
    public string PoolTag { get; private set; }
    public Sprite Icon { get; private set; }

    public bool CanInteract { get; private set; } = true;

    private Collider2D _col;
    private Rigidbody2D _rb;

    private void Awake()
    {
        TryGetComponent(out _col);
        TryGetComponent(out _rb);
    }

    public void SetInfo(string poolTag, Sprite icon)
    {
        PoolTag = poolTag;
        Icon = icon;
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
        // reset ให้พร้อม spawn ใหม่
        SetInteractable(true);
        if (_col != null) _col.enabled = true;
    }
}
