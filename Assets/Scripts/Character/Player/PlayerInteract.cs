using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interact Settings")]
    [SerializeField] private float interactRadius = 1f;
    [SerializeField] private Transform interactOrigin;

    [Header("Throwable Display (Hold Point)")]
    [SerializeField] private Transform displayPoint;

    [Header("Throw Settings")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private LayerMask interactLayer;


    private Player player;
    private GameObject heldItemObject = null;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // ถ้าถือของอยู่ → ปา
            if (heldItemObject != null)
            {
                Throw();
                return;
            }

            // ไม่ถืออะไร → พยายามเก็บ
            TryPickUp();
        }
    }

    // ---------------- PICKUP ----------------
    private void TryPickUp()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(interactOrigin.position, interactRadius, interactLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out var interactObj) && interactObj.CanInteract)
            {
                interactObj.Interact(player); // ตัว Spawner จะเรียก SetThrowable(gameObject) ให้เอง
                return;
            }
        }
    }

    // ---- Called by ThrowableSpawner when player picks up ----
    public void SetThrowable(GameObject itemObject)
    {
        // ถ้าเคยถืออยู่แล้ว → เอาของเก่าทิ้งก่อน
        if (heldItemObject != null)
        {
            Destroy(heldItemObject);
        }

        heldItemObject = itemObject;

        // ย้าย object ขึ้นหัวผู้เล่น
        heldItemObject.transform.SetParent(displayPoint);
        heldItemObject.transform.localPosition = Vector3.zero;
        heldItemObject.transform.localRotation = Quaternion.identity;
        heldItemObject.transform.localScale = Vector3.one; // ป้องกัน scale แปลก

        // ปิด collider ระหว่างถือบนหัว
        if (heldItemObject.TryGetComponent<Collider2D>(out var col))
            col.enabled = false;
    }

    // ---------------- THROW ----------------
    private void Throw()
    {
        if (heldItemObject == null) return;

        // ถ้าไม่ใช่ Duckling → ใช้เป็นท่า Attack ของอาชีพ & ของหายไป
        if (!player.IsDuckling)
        {
            player.Attack();   // ท่าอาชีพจัดการเอง
            Destroy(heldItemObject);
            heldItemObject = null;
            return;
        }

        // Duckling = ปาของตามแรง
        heldItemObject.transform.SetParent(null);

        if (heldItemObject.TryGetComponent<Collider2D>(out var col))
            col.enabled = true;

        var rb = heldItemObject.GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(player.FaceDir * throwForce, 0);

        heldItemObject = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactOrigin == null) interactOrigin = transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactOrigin.position, interactRadius);
    }
}
