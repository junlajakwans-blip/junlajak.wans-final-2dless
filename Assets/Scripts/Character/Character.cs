using UnityEngine;

public abstract class Character : MonoBehaviour, IDamageable
{
    [Header("Character Stats")]
    [SerializeField] protected int _maxHealth = 100;
    [SerializeField] protected int _currentHealth;
    [SerializeField] protected float _moveSpeed = 3.5f;

    [Header("Components")]
    [SerializeField] protected Rigidbody2D _rigidbody;
    [SerializeField] protected Animator _animator;

    protected bool _isDead = false;


    #region Properties
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _isDead;
    #endregion

    #region Initialization
    public virtual void Initialize(int maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = _maxHealth;
        _isDead = false;
    }
    #endregion
    

    #region Health System
    public virtual void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }

        UpdateHealthBar();
    }

    public virtual void Heal(int amount)
    {
        if (_isDead) return;

        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        UpdateHealthBar();
    }

    public abstract void Die();
    #endregion

    #region Movement / Attack
    public virtual void Move(Vector2 direction)
    {
        if (_rigidbody != null)
            _rigidbody.linearVelocity = direction * _moveSpeed;

        if (_animator != null)
            _animator.SetFloat("MoveSpeed", direction.magnitude);
    }

    public abstract void Attack();

    protected virtual void UpdateHealthBar()
    {
    }
    #endregion
}
