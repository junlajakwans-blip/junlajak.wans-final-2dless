using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interact Settings")]
    [SerializeField] private float interactRadius = 2.2f;
    [SerializeField] private Transform interactOrigin;

    [Header("Throwable Display (Hold Point)")]
    [SerializeField] private Transform displayPoint;

    [Header("Throw Settings")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private LayerMask interactLayer;

    private Player player;
    private GameObject heldItemObject;

    public bool HasItem() => heldItemObject != null;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    // ---------------- PICKUP ----------------
    public void TryPickUp()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(interactOrigin.position, interactRadius, interactLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out var interactObj) && interactObj.CanInteract)
            {
                interactObj.Interact(player);
                return;
            }
        }
    }

    // ---------------- RECEIVE ITEM FROM SPAWNER ----------------
    public void SetThrowable(GameObject itemObject)
    {
        // ถ้ามีของเดิมอยู่บนหัว → คืน pool ก่อน
        if (heldItemObject != null)
        {
            if (heldItemObject.TryGetComponent<ThrowableItemInfo>(out var prevInfo))
                ObjectPoolManager.Instance.ReturnToPool(prevInfo.PoolTag, heldItemObject);
        }

        // ต้องเซ็ตใหม่หลังคืน pool — ไม่ต้องอยู่ใน if
        heldItemObject = itemObject;

        // ย้ายไปไว้บนหัว
        heldItemObject.transform.SetParent(displayPoint);
        heldItemObject.transform.localPosition = Vector3.zero;
        heldItemObject.transform.localRotation = Quaternion.identity;
        heldItemObject.transform.localScale = Vector3.one;

        // คุมฟิสิกส์ตอนถือขึ้นหัว
        if (heldItemObject.TryGetComponent<ThrowableItemInfo>(out var info))
            info.DisablePhysicsOnHold();
    }


    // ---------------- THROW ----------------
    public void ThrowItem()
    {
        if (heldItemObject == null) return;

        if (!player.IsDuckling)
        {
            Debug.Log("[PlayerInteract] Cannot throw — current form is NOT Duckling.");
            return;
        }

        // ThrowPoint ถูก Flip ตาม Visual 
        if (throwPoint != null)
        {
        // ย้ายของที่ถูกถือ (heldItemObject) ให้มี World Position เดียวกับ throwPoint
        heldItemObject.transform.position = throwPoint.position;
        }

        // ปล่อยออกจากหัว
        heldItemObject.transform.SetParent(null);

        // เปิดฟิสิกส์ก่อนปา
        if (heldItemObject.TryGetComponent<ThrowableItemInfo>(out var info))
            info.EnablePhysicsOnThrow();

        if (heldItemObject.TryGetComponent<Rigidbody2D>(out var rb))
        {
            float inherit = 0f;
            if (player.TryGetComponent<Rigidbody2D>(out var prb))
                inherit = prb.linearVelocity.x * 1.2f;

            float x = player.FaceDir * throwForce * 3.2f + inherit;
            float y = throwForce * 0.8f;

            rb.linearVelocity = new Vector2(x, y);
        }


        // ล้าง state บนหัว
        heldItemObject = null;
    }


    // ถ้าระบบอื่นต้องการดึงของบนหัวโดยตรง
    public GameObject ConsumeHeldItem()
    {
        var temp = heldItemObject;
        heldItemObject = null;
        return temp;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactOrigin == null) interactOrigin = transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactOrigin.position, interactRadius);
    }
}
