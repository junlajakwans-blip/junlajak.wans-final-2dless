using System.Collections;
using UnityEngine;


/// <summary>
/// Main Player entity — handles movement, combat, currency, and interaction systems.
/// Implements IDamageable, IAttackable, ISkillUser, ICollectable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour, IDamageable, IAttackable, ISkillUser, ICollectable, IInteractable
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

    [Header("Environment Awareness")]
    [SerializeField] protected MapType _currentMapType = MapType.None;

    [Header("Buff Settings")]
    [SerializeField] protected bool _hasMapBuff;
    [SerializeField] protected float _buffMultiplier = 1.0f;

    private float _speedModifier = 1f;
    private Coroutine _speedRoutine;
    private WaitForSeconds _speedWait;

    public bool CanInteract => throw new System.NotImplementedException();
    public string PlayerName => _playerData != null ? _playerData.PlayerName : "Unknown";

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

        UpdatePlayerFormState();

        DetectMap();

        Debug.Log($"[Player] Initialized with HP: {_maxHealth}, Speed: {_moveSpeed}");
    }
    #endregion


    #region Movement
    public void Move(Vector2 direction)
    {
        if (_isDead) return;

        float speed = _moveSpeed * _speedModifier;

#if UNITY_2022_3_OR_NEWER
        Vector2 velocity = new Vector2(direction.x * speed, _rigidbody.linearVelocity.y);
        _rigidbody.linearVelocity = velocity;
#else
        Vector2 velocity = new Vector2(direction.x * speed, _rigidbody.velocity.y);
        _rigidbody.velocity = velocity;
#endif

        _animator?.SetMoveAnimation(direction.x);

#if UNITY_2022_3_OR_NEWER
        _isGrounded = Mathf.Abs(_rigidbody.linearVelocity.y) < 0.01f;
#else
        _isGrounded = Mathf.Abs(_rigidbody.velocity.y) < 0.01f;
#endif
    }
    #endregion

    #region  Speed Modifier (Slow / Boost)
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


    #region Jump & Jump Attack
    public void Jump()
    {
        if (_isDead || !_isGrounded) return;

        _rigidbody.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        _isGrounded = false;

        if (_animator != null)
            _animator.SetTrigger("Jump");
    }

    // detwect jump attack on collision
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Reset grounded
        _isGrounded = true;

        // Check only if collided with Enemy
        if (collision.gameObject.TryGetComponent<Enemy>(out var enemy))
        {
            foreach (var contact in collision.contacts)
            {
                // Collided from above and falling
                if (contact.normal.y > 0.5f && _rigidbody.linearVelocity.y < 0f)
                {
                    // Execute jump attack
                    HandleJumpAttack(enemy);
                    break;
                }
            }
        }
    }

    // Stomp on enemy after Jump
    private void HandleJumpAttack(Enemy enemy)
    {
        Debug.Log($"[{PlayerName}] stomped on {enemy.EnemyType}");

        // If enemy is moveable, stop its movement
        if (enemy is IMoveable moveableEnemy)
            moveableEnemy.Stop();

        // Bounce back slightly
        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _jumpForce * 0.8f);

        // If there is a stomp animation
        if (_animator != null)
            _animator.SetTrigger("JumpAttack");
            
    }
    #endregion


    #region Map Detection
    /// <summary>
    /// Detects current map type from SceneManager and caches it.
    /// </summary>
    protected virtual void DetectMap()
    {
        var sceneManager = FindFirstObjectByType<SceneManager>();

        if (sceneManager != null && sceneManager.IsInitialized)
        {
            _currentMapType = sceneManager.GetCurrentMapType();
            Debug.Log($"[{name}] detected map: {_currentMapType}");
        }
        else
        {
            _currentMapType = MapType.None;
            Debug.LogWarning($"[{name}] SceneManager not found or not ready!");
        }
    }

    /// <summary>
    /// Returns the cached current map type for subclasses (careers).
    /// </summary>
    protected MapType GetCurrentMapType() => _currentMapType;

    #endregion


    #region Health System (IDamageable)
    /// <summary>
    /// Apply damage to the player.
    /// </summary>
    /// <param name="amount"></param>
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

    /// <summary>
    /// Heal the player by a specified amount.
    /// </summary>
    /// <param name="amount"></param>
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

    public DuckCareer GetCurrentCareerID()
    {
        return _careerSwitcher != null && _careerSwitcher.CurrentCareer != null
            ? _careerSwitcher.CurrentCareer.CareerID
            : DuckCareer.Duckling;
    }

    /// <summary>
    /// Player throws an item.
    /// </summary>
    public void ThrowItem()
    {
        Debug.Log("[Player] Threw an item!");
        // TODO: projectile, animation, or effect
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
    public virtual void Attack()
    {
        Debug.Log("[Player] Basic attack triggered.");
        // TODO: integrate with weapon or animation
    }

    public virtual void ChargeAttack(float power)
    {
        Debug.Log($"[Player] Charge attack power: {power}");
        // TODO: implement hold-release mechanic
    }

    public virtual void RangeAttack(Transform target)
    {
        Debug.Log($"[Player] Range attack at {target.name}");
        // TODO: projectile or ability cast
    }

    public virtual void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
        Debug.Log($"[Player] Dealt {amount} damage to {target}");
    }

    public virtual void UseSkill()
    {
        Debug.Log("[Player] Skill used.");
        //TODO : Career-based skill activation
        //_careerSwitcher?.ActivateCareerSkill();
    }

    public virtual void OnSkillCooldown()
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

    public bool IsDuckling =>
    _careerSwitcher != null && _careerSwitcher.IsDuckling;

    public void UpdatePlayerFormState()
    {
        // sync ข้อมูลระหว่าง Data และ CareerSwitcher
        if (_careerSwitcher != null)
            _playerData.IsDefaultDuckling = _careerSwitcher.IsDuckling;

        Debug.Log($"[Player] Form state updated. Duckling = {_playerData.IsDefaultDuckling}");
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


    #region ICollectable and IInteractable Implementation
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

    public void Interact(Player player)
    {
        throw new System.NotImplementedException();
    }

    public void ShowPrompt()
    {
        throw new System.NotImplementedException();
    }
    #endregion
}