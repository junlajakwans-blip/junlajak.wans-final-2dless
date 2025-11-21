using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// Main Player entity — handles movement, combat, currency, and interaction systems.
/// Implements IDamageable, IAttackable, ISkillUser, ICollectable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : Character, IDamageable, IAttackable, ISkillUser, IInteractable
{
#region Fields
    [Header("Core Data")]
    [SerializeField] private PlayerData _playerData;
    [SerializeField] protected CareerSwitcher _careerSwitcher;
    [SerializeField] private CardManager _cardManager;
    [SerializeField] private Currency _currency;

    [Header("Components")]
    [SerializeField] private CharacterRigAnimator _rigAnimator; 

    [Header("Stats")]

    [SerializeField] protected float _jumpForce = 5f;


    [Header("Runtime State")]
    [SerializeField] private bool _isGrounded = false;

    [Header("Environment Awareness")]
    [SerializeField] protected MapType _currentMapType = MapType.None;

    [Header("Buff Settings")]
    [SerializeField] protected bool _hasMapBuff;
    [SerializeField] protected float _buffMultiplier = 1.0f;

    private float _speedModifier = 1f;
    private Coroutine _speedRoutine;
    private WaitForSeconds _speedWait;

    [Header("Interaction Settings")]
    [SerializeField] private float _interactRadius = 1.5f; // รัศมีการค้นหา
    [SerializeField] private LayerMask _collectibleLayer;


    public bool CanInteract => true;
    public string PlayerName => _playerData != null ? _playerData.PlayerName : "Unknown";

    #endregion


    #region Initialization
    public void Initialize(PlayerData data)
    {
        _playerData = data;
        _maxHealth = data.MaxHealth;
        _currentHealth = _maxHealth;
        _moveSpeed = data.Speed;

        _careerSwitcher ??= FindFirstObjectByType<CareerSwitcher>();
        _cardManager ??= FindFirstObjectByType<CardManager>();
        _rigidbody ??= GetComponent<Rigidbody2D>();
        _rigAnimator ??= GetComponent<CharacterRigAnimator>();

        // Currency setup
        _currency ??= new Currency();

        //career
        _currency.Initialize(_careerSwitcher);

        if (_cardManager != null)
        _cardManager.Initialize(this);

        UpdatePlayerFormState();

        DetectMap();

        Debug.Log($"[Player] Initialized with HP {_maxHealth}, Speed {_moveSpeed}, Career={_careerSwitcher?.CurrentCareer?.CareerID}");
    }
    #endregion


#region Movement
    public override void Move(Vector2 direction)
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

        _rigAnimator?.SetMoveAnimation(direction.x); 

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
    public virtual void Jump()
    {
        if (_isDead || !_isGrounded) return;


        _rigidbody.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse); 
        _isGrounded = false;


        if (_rigAnimator != null)
            _rigAnimator.SetTrigger("Jump");
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


        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _jumpForce * 0.8f);


        if (_rigAnimator != null)
            _rigAnimator.SetTrigger("JumpAttack");
            
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
    public override void TakeDamage(int amount)
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
    /// Player – Default form (no passive heal)
    /// Only way to Heal Player or some career must use BuffItem when Icollectable
    /// </summary>
    /// <param name="amount"></param>
    public override void Heal(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

        Debug.Log($"[Player] Healed +{amount}. HP: {_currentHealth}/{_maxHealth}");
    }

    public override void Die()
    {
        _isDead = true;
        _rigidbody.linearVelocity = Vector2.zero;
        if (_animator != null)
            _animator.SetTrigger("Die");

        Debug.Log("[Player] Player died.");
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
    public virtual void ThrowItem()
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

    public void AddToken(int amount)
    {
        if (_currency == null) return;
        _currency.AddToken(amount);
    }

    public Currency GetCurrency() => _currency;
    #endregion


    #region Combat System (IAttackable, ISkillUser)
    public override void Attack()
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

    protected virtual void InitializeCareerBuffs()
    {
        //Ducklng No Buff
    }
    #endregion


    #region Reset
    public void ResetState()
    {
        _isDead = false;
        _currentHealth = _maxHealth;
        _currency.ResetAll();
        _rigidbody.linearVelocity = Vector2.zero;

        if (_rigAnimator != null)
            _rigAnimator.ResetAllTriggers();

        Debug.Log("[Player] Reset to initial state.");
    }
    #endregion


    #region IInteractable Implementation

    public void Interact(Player player)
    {
        //  Interact/Pickup ที่ทำให้เกิด Error ถูกแทนที่ด้วยการค้นหาวัตถุ
        
        // 1. สแกนหา Collider 2D ในรัศมีที่กำหนด
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position, 
            _interactRadius, 
            _collectibleLayer
        );

        foreach (Collider2D hit in hitColliders)
        {
            // 2. ตรวจสอบว่าวัตถุที่เจอเป็น CollectibleItem หรือไม่
            if (hit.TryGetComponent<CollectibleItem>(out var collectible))
            {
                // 3. ถ้าเจอ ให้เรียก Collect() แล้วหยุด (เก็บแค่ชิ้นเดียวต่อการกด E)
                collectible.Collect(player);
                Debug.Log($"[PlayerInteract] Collected {collectible.GetCollectType()} via E.");
                return; 
            }
        }

    }

    public void ShowPrompt()
    {
        Debug.Log ("Player pick up");
    }
    #endregion

}