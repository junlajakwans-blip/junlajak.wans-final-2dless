using UnityEngine;

public abstract class Character : MonoBehaviour, IDamageable
{
    [Header("Character Stats")]
    [SerializeField] protected int _maxHealth = 500;
    [SerializeField] protected int _currentHealth;
    [SerializeField] protected float _moveSpeed = 3.5f;

    [Header("Components")]
    [SerializeField] protected Rigidbody2D _rigidbody;
    [SerializeField] protected Animator _animator;
    [SerializeField] private Transform _visualRoot;

    protected bool _isDead = false;
    [SerializeField] protected bool _facingRight = true;

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

        if (_animator != null)
            _animator.SetBool("IsDead", false);
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
            Die();     //_bonkEffecttrigger death instantly
            return;     //_bonkEffectstop here (no post-update after death)
        }

        UpdateHealthBar();
    }

    public virtual void Heal(int amount)
    {
        if (_isDead) return;

        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        UpdateHealthBar();
    }

    // Enemy.cs / Player.cs ต้อง override
    public abstract void Die();
    #endregion

    #region Movement / Attack
    public virtual void Move(Vector2 direction)
    {
        if (_isDead)
        {
            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        if (_rigidbody != null)
            _rigidbody.linearVelocity = direction * _moveSpeed;

        if (_animator != null)
            _animator.SetFloat("MoveSpeed", direction.magnitude);
    }

    public abstract void Attack();
    #endregion

    #region Health Bar
    protected virtual void UpdateHealthBar() { }
    #endregion

    #region Facing
    protected void FaceTarget(Transform target)
    {
        if (target == null) return;

        float xDir = target.position.x - transform.position.x;

        if (xDir < 0 && _facingRight) Flip();
        else if (xDir > 0 && !_facingRight) Flip();
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
