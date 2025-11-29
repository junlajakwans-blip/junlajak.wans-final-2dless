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

    // Smooth Prompt Runtime Vars
    private CanvasGroup _promptCanvasGroup;
    private Coroutine _fadeRoutine;

    // Offset สำหรับ Prompt
    private Vector3 _promptStartOffset = new Vector3(0, 0.40f, 0);
    private Vector3 _promptTargetOffset = new Vector3(0, 0.70f, 0);

    private Coroutine _autoDespawnRoutine;
    [SerializeField] private float _despawnDelay = 5f;
    [SerializeField] private float _throwRotationSpeed = 360f;
    [SerializeField] private float _bounceForce = 4f;
    [SerializeField] private float _bounceDrag = 4f;
    [SerializeField] private GameObject hitFX;

    [SerializeField] private GameObject promptUI;
    
    private bool _activated = false;

    public bool CanInteract { get; private set; } = true;

    private void Awake()
    {
        TryGetComponent(out _col);
        TryGetComponent(out _rb);
        TryGetComponent(out _sr);
    }

    private void Update()
    {
        if (!_isThrown || _rb == null) return;

        if (_rb.linearVelocity.sqrMagnitude < 0.3f)
            _isThrown = false;

        transform.Rotate(Vector3.forward * _throwRotationSpeed * Time.deltaTime);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            HidePrompt();
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
        HidePrompt();
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
        if (promptUI == null || !CanInteract) return;

        // เช็คถือของอยู่แล้ว → ไม่ให้โชว์
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent<PlayerInteract>(out var interact))
            if (interact.HasItem()) return;

        if (!promptUI.activeSelf)
            promptUI.SetActive(true);

        promptUI.transform.position = transform.position + _promptStartOffset;
        promptUI.transform.rotation = Quaternion.identity;

        // Lazy init CanvasGroup
        if (_promptCanvasGroup == null)
        {
            _promptCanvasGroup = promptUI.GetComponent<CanvasGroup>();
            if (_promptCanvasGroup == null)
                _promptCanvasGroup = promptUI.AddComponent<CanvasGroup>(); //  กันพังตอน prefab ไม่มี
        }


        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadePrompt(1f));
    }

    public void HidePrompt()
    {
        if (promptUI == null) return;

        if (_promptCanvasGroup == null)
        {
            _promptCanvasGroup = promptUI.GetComponent<CanvasGroup>();
            if (_promptCanvasGroup == null)
                _promptCanvasGroup = promptUI.AddComponent<CanvasGroup>(); //  กันพังตอน prefab ไม่มี
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadePrompt(0f));
    }

    private IEnumerator FadePrompt(float targetAlpha)
    {
        float duration = 0.18f;
        float t = 0;
        float startAlpha = _promptCanvasGroup.alpha;

        Vector3 startPos = promptUI.transform.position;
        Vector3 endPos = transform.position + (targetAlpha > 0 ? _promptTargetOffset : _promptStartOffset);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            _promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerp);
            promptUI.transform.position = Vector3.Lerp(startPos, endPos, lerp);

            yield return null;
        }

        _promptCanvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0)
            promptUI.SetActive(false);
    }
#endregion

    private void Reset()
    {
        if (!TryGetComponent<CircleCollider2D>(out var physicsCol))
            physicsCol = gameObject.AddComponent<CircleCollider2D>();
        physicsCol.isTrigger = false;

        GameObject triggerObj = new GameObject("PromptTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.localPosition = Vector3.zero;

        CircleCollider2D trigger = triggerObj.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

        var handler = triggerObj.AddComponent<ThrowablePromptTrigger>();
        handler.Init(this);

        _isThrown = false;
        _hasBounced = false;
        _throwRotationSpeed = 360f; 
    }

    public class ThrowablePromptTrigger : MonoBehaviour
    {
        private ThrowableItemInfo info;
        public void Init(ThrowableItemInfo i) => info = i;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                info.ShowPrompt();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                info.HidePrompt();
        }
    }

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
