using System.Collections;
using UnityEngine;

/// <summary>
/// Main Player entity — handles movement, combat, currency, and interaction systems.
/// Implements IDamageable, IAttackable, ISkillUser, ICollectable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour, IDamageable, IAttackable, ISkillUser, ICollectable
{
    #region Fields
    [Header("Core Data")]
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private CareerSwitcher _careerSwitcher;
    [SerializeField] private CardManager _cardManager;
    [SerializeField] private Currency _currency;

    [Header("Components")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private CharacterRigAnimator _animator;

    [Header("Stats")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth;

    [Header("Runtime State")]
    [SerializeField] private bool _isGrounded = false;
    [SerializeField] private bool _isDead = false;

    private float _speedModifier = 1f;
    private Coroutine _speedRoutine;
    private WaitForSeconds _speedWait;

    #endregion


    #region Initialization
    public void Initialize(PlayerData data)
    {
        _playerData = data;
        _maxHealth = data.MaxHealth;
        _currentHealth = _maxHealth;
        _moveSpeed = data.Speed;

        if (_careerSwitcher == null)
            _careerSwitcher = GetComponent<CareerSwitcher>();

        if (_cardManager == null)
            _cardManager = GetComponent<CardManager>();

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();

        if (_animator == null)
            _animator = GetComponent<CharacterRigAnimator>();

        // Currency setup
        if (_currency == null)
            _currency = new Currency();

        _currency.Initialize(_careerSwitcher);

        Debug.Log($"[Player] Initialized with HP: {_maxHealth}, Speed: {_moveSpeed}");
    }
    #endregion


    #region Movement
    public void Move(Vector2 direction)
    {
        if (_isDead) return;

        Vector2 velocity = new Vector2(direction.x * _moveSpeed, _rigidbody.linearVelocity.y);
        _rigidbody.linearVelocity = velocity;

        if (_animator != null)
            _animator.SetMoveAnimation(direction.x);

        _isGrounded = Mathf.Abs(_rigidbody.linearVelocity.y) < 0.01f;
    }
    #region 🧊 Speed Modifier (Slow / Boost)
    /// <summary>
    /// Temporarily modifies the player's move speed.
    /// Example: ApplySpeedModifier(0.5f, 3f) → Slow 50% for 3 seconds.
    /// </summary>
    public void ApplySpeedModifier(float multiplier, float duration)
    {
        if (_speedRoutine != null)
            StopCoroutine(_speedRoutine);

        _speedWait = new WaitForSeconds(duration);
        _speedRoutine = StartCoroutine(SpeedModifierRoutine(multiplier));
    }

    private IEnumerator SpeedModifierRoutine(float multiplier)
    {
        _speedModifier = multiplier;
        Debug.Log($"[Player] Speed modifier applied: x{_speedModifier}");
        yield return _speedWait;
        _speedModifier = 1f;
        Debug.Log("[Player] Speed modifier reset to normal.");
    }
    #endregion


    public void Jump()
    {
        if (_isDead || !_isGrounded) return;

        _rigidbody.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        _isGrounded = false;

        if (_animator != null)
            _animator.SetTrigger("Jump");
    }
    #endregion


    #region Health System (IDamageable)
    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }

        Debug.Log($"[Player] Took {amount} damage. HP: {_currentHealth}/{_maxHealth}");
    }

    public void Heal(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

        Debug.Log($"[Player] Healed +{amount}. HP: {_currentHealth}/{_maxHealth}");
    }

    private void Die()
    {
        _isDead = true;
        _rigidbody.linearVelocity = Vector2.zero;
        if (_animator != null)
            _animator.SetTrigger("Die");

        Debug.Log("[Player] Player died.");
        // TODO: Notify GameManager / Trigger respawn / GameOver event
    }
    #endregion


    #region Currency System
    public void AddCoin(int amount)
    {
        if (_currency == null) return;
        _currency.AddCoin(amount);
    }

    public bool SpendCoin(int amount)
    {
        return _currency != null && _currency.UseCoin(amount);
    }

    public Currency GetCurrency() => _currency;
    #endregion


    #region Combat System (IAttackable, ISkillUser)
    public void Attack()
    {
        Debug.Log("[Player] Basic attack triggered.");
        // TODO: integrate with weapon or animation
    }

    public void ChargeAttack(float power)
    {
        Debug.Log($"[Player] Charge attack power: {power}");
        // TODO: implement hold-release mechanic
    }

    public void RangeAttack(Transform target)
    {
        Debug.Log($"[Player] Range attack at {target.name}");
        // TODO: projectile or ability cast
    }

    public void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
        Debug.Log($"[Player] Dealt {amount} damage to {target}");
    }

    public void UseSkill()
    {
        Debug.Log("[Player] Skill used.");
        //TODO : Career-based skill activation
        //_careerSwitcher?.ActivateCareerSkill();
    }

    public void OnSkillCooldown()
    {
        Debug.Log("[Player] Skill cooldown started.");
    }
    #endregion


    #region Card System
    public void UseCard(Card card)
    {
        if (card == null) return;
        Debug.Log($"[Player] Using card: {card.SkillName}");
        card.ActivateEffect(this);
    }
    #endregion


    #region Career System
    public void OnCareerChanged(DuckCareerData newCareer)
    {
        _playerData.SelectedCareer = newCareer.DisplayName;
        Debug.Log($"[Player] Career switched to: {newCareer.DisplayName}");
    }
    #endregion


    #region Reset
    public void ResetState()
    {
        _isDead = false;
        _currentHealth = _maxHealth;
        _currency.ResetAll();
        _rigidbody.linearVelocity = Vector2.zero;

        if (_animator != null)
            _animator.ResetAllTriggers();

        Debug.Log("[Player] Reset to initial state.");
    }
    #endregion


    #region ICollectable Implementation
    public void Collect(Player player)
    {
        // Player collecting itself doesn't make sense — handled by items
    }

    public void OnCollectedEffect()
    {
        // For CoinItem or other ICollectable only
    }

    public string GetCollectType()
    {
        return "Player";
    }
    #endregion
}
