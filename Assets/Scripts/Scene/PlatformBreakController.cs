using UnityEngine;
using System.Collections;

public class BreakPlatform : MonoBehaviour
{
    [Header("Break Settings")]
    [SerializeField] private float delayBeforeFall = 0.35f;
    [SerializeField] private float fallGravity = 2.5f;
    [SerializeField] private float despawnDelay = 2f;

    [Header("FX / Feedback")]
    [SerializeField] private GameObject breakWarningFX; // เอฟเฟคเตือนก่อนตก
    [SerializeField] private GameObject breakDustFX;    // เอฟเฟคตอนตก (พื้นแตก)
    [SerializeField] private bool screenShake = true;   // ให้กล้องสั่นตอนเหยียบ

    private bool _isBreaking = false;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isBreaking) return;
        if (collision.collider.TryGetComponent<Player>(out _))
            StartBreak();
    }

    public void StartBreak()
    {
        if (_isBreaking) return;
        _isBreaking = true;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        // ⚠ เอฟเฟคเตือนก่อนตก
        if (breakWarningFX != null)
            Instantiate(breakWarningFX, transform.position, Quaternion.identity);

        // 📸 เขย่าจอเล็กน้อย
        if (screenShake)
            CameraShaker.ShakeOnce(0.2f, 0.1f);   // (duration, strength)

        yield return new WaitForSeconds(delayBeforeFall);

        // 💥 เอฟเฟคตอนแพลตฟอร์มร่วง
        if (breakDustFX != null)
            Instantiate(breakDustFX, transform.position, Quaternion.identity);

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = fallGravity;

        yield return new WaitForSeconds(despawnDelay);
        ResetAndReturnToPool();
    }

    private void ResetAndReturnToPool()
    {
        _isBreaking = false;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        gameObject.SetActive(false);

        ObjectPoolManager.Instance.ReturnToPool(
            gameObject.name.Replace("(Clone)", "").Trim(),
            gameObject
        );
    }
}
