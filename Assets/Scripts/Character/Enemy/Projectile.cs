using UnityEngine;
using System.Collections;
using System;


/// <summary>
/// Handles the behavior, damage, and lifespan of a projectile (like the skewer).
/// Requires Rigidbody2D and Collider2D (set as trigger) on the Prefab.
/// </summary>
/// 
/// 
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private int _damageAmount;
    [SerializeField] private float _lifetime = 3f; // How long the projectile stays active

    // [NEW FIELDS]: References ที่ถูก Inject เข้ามา
    private IObjectPool _poolRef; 
    private string _poolTag;      

    #region Dependencies
    
    /// <summary>
    /// Sets the necessary references for Object Pooling.
    /// </summary>
    public void SetDependencies(IObjectPool pool, string poolTag)
    {
        _poolRef = pool;
        _poolTag = poolTag;
    }

    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void OnEnable()
    {
        // [FIX 1]: เปลี่ยนจาก Start() มาใช้ OnEnable() เพื่อจัดการ Timer เมื่อถูก Reuse
        if (_poolRef != null)
        {
            // ยกเลิก Invoke ที่ค้างอยู่ (ถ้ามี)
            CancelInvoke(nameof(ReturnToPool)); 
            // เริ่มนับถอยหลังใหม่เพื่อคืน Pool เมื่อหมดอายุ
            Invoke(nameof(ReturnToPool), _lifetime);
        }
        else
        {
            // Fallback: ถ้า Pool Reference หาย ให้ใช้ Destroy() แบบเดิม
            Destroy(gameObject, _lifetime); 
        }
    }
    
    private void OnDisable()
    {
        // [FIX 2]: ยกเลิก Invoke เมื่อ Object ถูกส่งกลับ Pool แล้ว
        CancelInvoke(nameof(ReturnToPool));
    }
    
    // NOTE: เมธอด Start() เดิมถูกลบออก เพราะ OnEnable() รับหน้าที่จัดการ Timer แทน

    #endregion

    /// <summary>
    /// Sets the damage value.
    /// </summary>
    public void SetDamage(int amount)
    {
        _damageAmount = amount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ----- โดน Player → ทำดาเมจ → หาย -----
        if (other.TryGetComponent<Player>(out var player))
        {
            player.TakeDamage(_damageAmount);

            Debug.Log($"[Projectile] Hit PLAYER → Damage: {_damageAmount} | PoolTag: {_poolTag}");

            ReturnToPool();
            return;
        }

        // ----- โดน Enemy → ทำดาเมจ → หาย -----
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(_damageAmount);

            Debug.Log($"[Projectile] Hit ENEMY → Damage: {_damageAmount} | PoolTag: {_poolTag} | Enemy: {enemy.name}");

            ReturnToPool();
            return;
        }

        // ----- อะไรอย่างอื่นที่ไม่ใช่ Player/Enemy → หาย -----
        if (other.CompareTag("Ground") ||
            other.CompareTag("Wall") ||
            other.CompareTag("Obstacle") ||
            other.CompareTag("Prop"))
        {
            Debug.Log($"[Projectile] Collided with MAP OBJECT ({other.tag}) → Returned to pool");
            ReturnToPool();
            return;
        }

        // เผื่อพลาด ยังไงไม่ใช่เป้าหมายก็คืน pool
        Debug.Log($"[Projectile] Unexpected collision with {other.name} → Returned to pool");
        ReturnToPool();
    }



    // ReturnToPool()
    private void ReturnToPool()
    {
        if (_poolRef != null)
        {
            // ส่ง Object กลับ Pool
            _poolRef.ReturnToPool(_poolTag, gameObject);
        } 
        else 
        {
            // Fallback (ถ้า DI ล้มเหลว)
            Destroy(gameObject); 
        }
    }
}