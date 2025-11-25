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
    [SerializeField] private Transform _visualRoot;
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

    [SerializeField] protected bool _facingRight = true;

    protected void FaceTarget(Transform target)
    {
        if (target == null) return;

        float xDir = target.position.x - transform.position.x;

        // ถ้าอยู่ซ้ายและกำลังหันขวา → พลิกกลับ
        if (xDir < 0 && _facingRight)
            Flip();
        // ถ้าอยู่ขวาและกำลังหันซ้าย → พลิกกลับ
        else if (xDir > 0 && !_facingRight)
            Flip();
    }

    protected void Flip()
    {
        _facingRight = !_facingRight;

        if (_visualRoot == null) return;  

        Vector3 s = _visualRoot.localScale;

        
        s.x = Mathf.Abs(s.x) * (_facingRight ? 1 : -1);
        _visualRoot.localScale = s;
    }



    #endregion
}
