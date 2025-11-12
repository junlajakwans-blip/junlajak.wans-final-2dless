using UnityEngine;

/// <summary>
/// Base class for all enemy types in the game.
/// Implements core attack, detection, and damage logic shared across all enemies.
/// Derived classes (e.g., DoggoMon, PeterMon) override behavior for movement or special skills.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IAttackable, IDamageable
{
    #region Fields
    [Header("Enemy Stats")]
    [SerializeField] protected float _speed = 1.5f;
    [SerializeField] protected int _attackPower = 10;
    [SerializeField] protected float _detectionRange = 5f;
    [SerializeField] protected int _health = 1;
    [SerializeField] protected Transform _target;
    [SerializeField] protected EnemyType _enemyType = EnemyType.None;
    #endregion

    #region Properties
    public float Speed { get => _speed; set => _speed = value; }
    public int AttackPower { get => _attackPower; set => _attackPower = value; }
    public float DetectionRange { get => _detectionRange; set => _detectionRange = value; }
    public EnemyType EnemyType { get => _enemyType; set => _enemyType = value; }
    public bool IsDead => _health <= 0;
    #endregion

    #region Unity Lifecycle
    protected virtual void Update()
    {
        if (_target == null) return;

        if (DetectPlayer(_target.position))
        {
            Move();
            Attack();
        }
    }
    #endregion

    #region Core Logic
    /// <summary>
    /// Moves toward the player if within detection range.
    /// </summary>
    public virtual void Move()
    {
        if (_target == null) return;

        Vector3 direction = (_target.position - transform.position).normalized;
        transform.position += direction * Speed * Time.deltaTime;
    }

    /// <summary>
    /// Executes a default melee attack behavior.
    /// </summary>
    public virtual void Attack()
    {
        Debug.Log($"[{_enemyType}] attacks the player with power {_attackPower}!");
    }

    /// <summary>
    /// Reduces health when hit by damage.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        _health -= amount;
        Debug.Log($"[{_enemyType}] took {amount} damage! Remaining HP: {_health}");

        if (_health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Detects player based on distance.
    /// </summary>
    public bool CanDetectOverride = true;

    public virtual bool DetectPlayer(Vector3 playerPos)
    {
        if (!CanDetectOverride) return false;
        float distance = Vector3.Distance(transform.position, playerPos);
        return distance <= _detectionRange;
    }

    /// <summary>
    /// Called when this enemy dies.
    /// </summary>
    public virtual void Die()
    {
        Debug.Log($"[{_enemyType}] has been defeated.");
        Destroy(gameObject);
    }
    #endregion

    #region IAttackable Implementation
    public virtual void ChargeAttack(float power)
    {
        Debug.Log($"[{_enemyType}] is charging attack with power x{power:F1}!");
    }

    public virtual void RangeAttack(Transform target)
    {
        Debug.Log($"[{_enemyType}] performs a ranged attack on {target?.name ?? "unknown target"}.");
    }

    public virtual void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
        Debug.Log($"[{_enemyType}] dealt {amount} damage to target!");
    }
    #endregion

    #region IDamageable Implementation
    public void Heal(int amount)
    {
        _health += amount;
        Debug.Log($"[{_enemyType}] healed for {amount} HP (Current: {_health}).");
    }

    void IAttackable.RangeAttack(Transform target)
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
