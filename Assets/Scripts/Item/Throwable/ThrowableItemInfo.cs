using UnityEngine;
using System.Collections;

public class ThrowableItemInfo : MonoBehaviour, IInteractable
{
    public string PoolTag { get; private set; }
    private ThrowableItemSO _data;

    private Collider2D _col;
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;

    private bool _isThrown = false;
    private bool _hasBounced = false;

    private Coroutine _autoDespawnRoutine;
    [SerializeField] private float _despawnDelay = 5f;
    [SerializeField] private float _throwRotationSpeed = 360f;
    [SerializeField] private float _bounceForce = 4f;
    [SerializeField] private GameObject hitFX;

    [SerializeField] private GameObject promptUI;
    [SerializeField] private TMPro.TMP_Text promptText;
    
    private bool _activated = false;

    public bool CanInteract { get; private set; } = true;

    [SerializeField] private Animator promptAnim; //Anim Text

    private void Awake()
    {
        TryGetComponent(out _col);
        TryGetComponent(out _rb);
        TryGetComponent(out _sr);
    }

    private void Update()
    {
        if (!_isThrown || _rb == null) return;
        {
            if (_rb.linearVelocity.sqrMagnitude < 0.3f)
                _isThrown = false;

            transform.Rotate(Vector3.forward * _throwRotationSpeed * Time.deltaTime);
        }
        
    }


#region  Physic
    public void ApplyData(ThrowableItemSO data)
    {
        _data = data;
        PoolTag = data.poolTag;
        _activated = false;      // <-- spawn idle
        _isThrown = false;
        _hasBounced = false;
        _throwRotationSpeed = 360f;

        // 🎯 ตั้ง sprite ของไอเทมจาก itemSprite ใน SO
        if (_sr != null && _data.itemSprite != null)
            _sr.sprite = _data.itemSprite;

        transform.localScale = _data.scaleDefault;

        CanInteract = true;
        if (!_activated)   // spawn but not thrown
            ShowPrompt();
        else
            HidePrompt();

    }

    public void Interact(Player player)
    {
        if (!CanInteract) return;
        if (player.TryGetComponent<PlayerInteract>(out var interact))
            interact.SetThrowable(gameObject);
    }

    public void DisablePhysicsOnHold()
    {
        CanInteract = false;
        HidePrompt();

        if (_col != null) _col.enabled = false;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0;
        }

        transform.localScale = _data.scaleOnHold;
    }

    public void EnablePhysicsOnThrow()
    {
        HidePrompt();
        CanInteract = false;

        if (_col != null) _col.enabled = true;

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 3.2f;
            _rb.linearDamping = 0f;   
        }

        transform.localScale = _data.scaleOnThrow;

        _activated = true;
        _isThrown = true;
        _hasBounced = false;

        if (_autoDespawnRoutine != null) StopCoroutine(_autoDespawnRoutine);
        _autoDespawnRoutine = StartCoroutine(AutoDespawnTimer());
    }

    public void OnReturnedToPool()
    {
        CanInteract = true;
        HidePrompt();

        if (_col != null) _col.enabled = true;

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1;
        }

        transform.localScale = _data.scaleDefault;
        transform.SetParent(null);

        if (_autoDespawnRoutine != null)
        {
            StopCoroutine(_autoDespawnRoutine);
            _autoDespawnRoutine = null;
        }

        _isThrown = false;
        _hasBounced = false;
        _throwRotationSpeed = 360f;

        SpawnSlot.Unreserve(transform.position);


    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
       
        if (!_activated) return;

        //  ถ้าโดน Throwable ตัวอื่น → ข้าม
        if (collision.collider.TryGetComponent<ThrowableItemInfo>(out var otherItem))
            return;

        if (_data == null)
        {
            ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
            return;
        }

        if (!_hasBounced)
        {
            _hasBounced = true;
            if (_rb != null)
            {
                 _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.55f, _bounceForce * 1.4f);
                _rb.gravityScale = 3.2f;   
                _rb.linearDamping = 4f;
            }
            _throwRotationSpeed *= 0.55f; // ลดความเร็วหมุนหลังเด้ง
            return; // ครั้งแรกขอเด้งก่อน ยังไม่ Despawn
        }

        // After bounce → Hit
        if (collision.collider.TryGetComponent<Enemy>(out var enemy))
            enemy.TakeDamage(_data.damage);

        // FX
        if (hitFX != null && collision.contacts.Length > 0)
        {
            float flip = _rb.linearVelocity.x < 0 ? 180f : 0f;
            Instantiate(hitFX, collision.contacts[0].point, Quaternion.Euler(0, flip, 0));
        }

        // Stop rotation and despawn
        _isThrown = false;
        if (_autoDespawnRoutine != null) 
        {
            StopCoroutine(_autoDespawnRoutine);
            _autoDespawnRoutine = null;
        }


        ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }
    #endregion

#region PromptUI
    public void ShowPrompt()
    {
        if (promptAnim != null)
            promptAnim.SetBool("Visible", true);
    }

    public void HidePrompt()
    {
        if (promptAnim != null)
            promptAnim.SetBool("Visible", false);
    }

    public void RefreshPrompt(PlayerInteract interact)
    {
        promptText.text = interact.HasItem() 
            ? "<color=#FFA400>TO THROW</color>" 
            : "<color=white>TO PICK UP</color>";
    }
#endregion


    private IEnumerator AutoDespawnTimer()
    {
        float t = 0f;
        while (t < _despawnDelay)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (gameObject.activeSelf) 
            ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
    }

    
}
