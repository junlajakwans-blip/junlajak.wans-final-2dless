using System.Collections;
using System;
using UnityEngine;



/// <summary>
/// Main Player entity — handles movement, combat, currency, and interaction systems.
/// Implements IDamageable, IAttackable, ISkillUser, ICollectable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : Character, IDamageable, IAttackable, ISkillUser
{
#region Fields
    [Header("Core Data")]
    [SerializeField] private PlayerData _playerData;
    private CareerSwitcher _careerSwitcher;
    public DuckCareerData CurrentCareerData => _careerSwitcher?.CurrentCareer;

    public DuckCareer CurrentCareerID =>
    _careerSwitcher != null && _careerSwitcher.CurrentCareer != null
        ? _careerSwitcher.CurrentCareer.CareerID
        : DuckCareer.Duckling;
    
    private CardManager _cardManager;
     private Currency _currency;

    [Header("Components")]
    [SerializeField] private CharacterRigAnimator _rigAnimator; 

    [Header("Stats")]

    [SerializeField] protected float _jumpForce = 5f;
    [SerializeField] protected const int JUMP_ATTACK_DAMAGE = 10; // Damage from stomp
    [SerializeField] protected const int BASIC_ATTACK_DAMAGE = 15; // Damage from basic attack

    [Header("Death Zone")]
    [SerializeField] private float _fallDeathY = -5.0f; 
    public float FallDeathY => _fallDeathY; // Allow Game Manager to read

    [Header("Runtime State")]
    [SerializeField] private bool _isGrounded = false;
    [SerializeField] private float _chargePower = 0f;

    [Header("Environment Awareness")]
    [SerializeField] protected MapType _currentMapType = MapType.None;
    public MapType CurrentMapType => _currentMapType;


    [Header("Buff Settings")]
    [SerializeField] protected bool _hasMapBuff;
    [SerializeField] protected float _buffMultiplier = 1.0f;

    [Header("Defense State")]
    [SerializeField] private bool _isInvulnerable = false;
    public bool IsInvulnerable => _isInvulnerable;

    [Header("UI References")]
    [SerializeField] private HealthBarUI _healthBarUI;
    public event System.Action<int> OnCoinCollected;
    // Stored handler for ScoreUI so we can unsubscribe cleanly
    private System.Action<int> _scoreUIHandler;

    [Header("Fx References")]
    [SerializeField] private CareerEffectProfile _fxProfile;

    public float GetChargePower() => _chargePower;


    private float _speedModifier = 1f;
    private Coroutine _speedRoutine;
    private WaitForSeconds _speedWait;
    private PlayerInteract _interact;

    public CareerEffectProfile FXProfile => _fxProfile;
    public string PlayerName => _playerData != null ? _playerData.PlayerName : "Unknown";
    public int FaceDir { get; private set; } = 1;

    public CareerSkillBase CurrentCareerSkill =>
    _careerSwitcher != null && _careerSwitcher.CurrentCareer != null
        ? _careerSwitcher.CurrentCareer.CareerSkill
        : null;
        
    //public event Action<Player> OnPlayerDied;

    #endregion

    private void Update()
    {
        if (_isDead) return;

        // Check for falling into Death Zone
        if (transform.position.y < _fallDeathY)
        {
            Debug.Log($"[Player] Fell below Death Zone ({_fallDeathY}). Forcing Die.");
            // Call Die() which triggers GameManager.EndGame()
            Die(); 
        }
    }

    // Editor helper to force Inspector refresh when running in the Unity Editor.
    public void ForceEditorRefresh()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    #region Initialization
    public void Initialize(PlayerData data, CardManager cardManager, CareerSwitcher careerSwitcher)
    {
        gameObject.SetActive(true);
        _playerData = data;
        // Ensure Player uses the same Currency instance provided by GameManager via PlayerData
        if (data != null && data.Currency != null)
        {
            _currency = data.Currency;
            Debug.Log($"[Player] Currency synced from PlayerData. Coin={_currency.Coin}, Token={_currency.Token}, KeyMap={_currency.KeyMap}");
        }
        
        // 1. Fetch Max Health including upgrade bonuses (from PlayerData.MaxHealth)
        // Note: We assume data.MaxHealth is the correct total value (e.g., 100 if no upgrade) 
        int upgradedMaxHealth = data.MaxHealth; 
        
        // 2. Fetch Base Health of the current career (200 for Duckling default)
        // If CurrentCareerData is null (e.g., Duckling), use Duckling's BaseHealth (200)
        int careerBaseHealth = CurrentCareerData != null ? CurrentCareerData.BaseHealth : 200; 

        // 3. 🔥 FIX: Calculate final Max Health
        //    * Use the greater value between PlayerData's MaxHealth and the Career's BaseHealth.
        //    * If data.MaxHealth (100) < careerBaseHealth (200), finalMaxHealth will be 200.
        //    * If data.MaxHealth (600) is upgraded, finalMaxHealth will be 600.
        int finalMaxHealth = Mathf.Max(upgradedMaxHealth, careerBaseHealth);
        
        // 4. 🔥 FIX: Call base.Initialize to set the correct _maxHealth and _currentHealth in the base class (Character)
        base.Initialize(finalMaxHealth);

        // =========================================================
        // ✅ NEW: COLORED DEBUG LOG
        // =========================================================
        Debug.Log($"<color=lime>[Player] ★ MAX HP SET! Final HP: {finalMaxHealth} (Upgraded:{upgradedMaxHealth} / Base:{careerBaseHealth})</color>");
        // =========================================================

        
        // Set speed (which is not affected by HP logic)
        _moveSpeed = data.Speed;


        // -----------------------------------------------------------------
        // 1. Get Dependencies and check immediately
        // -----------------------------------------------------------------
        _careerSwitcher = careerSwitcher;
        _cardManager = cardManager;

        if (_careerSwitcher == null) 
            Debug.LogError($"[Player] ❌ CareerSwitcher is MISSING! (Injection failed from GameManager)");
        
        if (_cardManager == null) 
            Debug.LogError($"[Player] ❌ CardManager is MISSING! (Injection failed from GameManager)");


        // -----------------------------------------------------------------
        // 2. Fetch internal Components and check
        // -----------------------------------------------------------------
        if (_rigAnimator == null)
        {
            _rigAnimator = GetComponent<CharacterRigAnimator>() ?? FindFirstObjectByType<CharacterRigAnimator>();
        }

        _rigidbody ??= GetComponent<Rigidbody2D>();
        if (_rigidbody == null) Debug.LogError("[Player] ❌ Rigidbody2D component is missing!");
        
        _rigAnimator ??= GetComponent<CharacterRigAnimator>();
        if (_rigAnimator == null) Debug.LogError("[Player] ❌ CharacterRigAnimator component is missing!");

        _interact = GetComponent<PlayerInteract>(); 
        if (_interact == null) Debug.LogError("[Player] ❌ PlayerInteract component is missing on Player or children!");


        // -----------------------------------------------------------------
        // 3. Initialize sub-systems
        // -----------------------------------------------------------------
        _currency ??= new Currency();

        // Check before calling to prevent NullReferenceException
        if (_careerSwitcher != null)
            _currency.Initialize(_careerSwitcher);
        
        if (_cardManager != null)
            _cardManager.Initialize(this);

        
        if (UIManager.Instance != null)
        {
            // UIManager.Instance is the DDoL Singleton holding HealthBarUI
            HealthBarUI healthBar = UIManager.Instance.GetPlayerHealthBarUI();
            
            // Use the existing SetHealthBarUI method to Inject Reference and Initialize Max HP
            if (healthBar != null)
            {
                // SetHealthBarUI will call healthBar.InitializeHealth(_maxHealth) again
                // _maxHealth is already set by base.Initialize(finalMaxHealth)
                SetHealthBarUI(healthBar);
            }
        }
        else
        {
            Debug.LogWarning("[Player] ❌ UIManager Instance not found (DDoL not ready). Cannot set HealthBarUI.");
        }

        UpdatePlayerFormState();

        DetectMap();


        // -----------------------------------------------------------------
        // 4. Initialization summary
        // -----------------------------------------------------------------
        bool isSuccess = _careerSwitcher != null && _cardManager != null && _rigidbody != null;
        string statusIcon = isSuccess ? "✅" : "⚠️";
        
        Debug.Log($"[Player] {statusIcon} Initialize Complete.\n" +
                  $"  - Final Max HP: {finalMaxHealth}, Speed: {_moveSpeed}\n" +
                  $"  - CareerSwitcher: {(_careerSwitcher != null ? "OK" : "NULL")}\n" +
                  $"  - CardManager: {(_cardManager != null ? "OK" : "NULL")}\n" +
                  $"  - Components: {(_rigidbody != null && _rigAnimator != null && _interact != null ? "OK" : "INCOMPLETE")}");
    }

    /// <summary>
    /// Resets PlayerData when save is deleted or reset.
    /// Called by GameManager.OnGameReset event.
    /// Clears stale serialized Inspector values.
    /// </summary>
    public void ResetPlayerDataCache()
    {
        if (_playerData != null)
        {
            // Create fresh empty PlayerData to clear stale serialized values in Inspector
            _playerData = new PlayerData(new Currency(), new GameProgressData());
            Debug.Log("<color=yellow>[Player] 🔄 PlayerData cache reset to empty state (Coin: 0, Token: 0, KeyMap: 0)</color>");
        }
    }

    #endregion

#region Movement
public override void Move(Vector2 direction)
{
    if (_isDead) return; 

    float x = direction.x;

    if (direction.x > 0.01f && !_facingRight) Flip();
    else if (direction.x < -0.01f && _facingRight) Flip();

    // 🔥 FIX: Update FaceDir during movement (for throwing)
    if (direction.x > 0.01f) 
    {
        FaceDir = 1; // Facing right
    }
    else if (direction.x < -0.01f)
    {
        FaceDir = -1; // Facing left
    }
    // **********************************

    float speed = _moveSpeed * _speedModifier; // Calculate speed

#if UNITY_2022_3_OR_NEWER
    _rigidbody.linearVelocity = new Vector2(direction.x * speed, _rigidbody.linearVelocity.y);
#else
    _rigidbody.velocity = new Vector2(direction.x * speed, _rigidbody.velocity.y);
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


    #region Throw / PickUp Decide
    public void HandleInteract()
    {
        if (_interact == null) _interact = GetComponent<PlayerInteract>();
        if (_interact == null) return;

        // Other careers → cannot pick up/throw, but don't check again when holding
        if (!IsDuckling)
        {
            Debug.Log("[Player] Only Duckling can pick up/throw items.");
            return;
        }

        // Has item → Throw immediately (no need to check Duckling again)
        if (_interact.HasItem())
        {
            _interact.ThrowItem();
            return;
        }

        // No item → Pick up
        _interact.TryPickUp();
    }
    #endregion


#region Jump & Jump Attack
    public virtual void Jump()
    {
        if (_isDead || !_isGrounded) return;

        // Motorcycle Skill — Jump Higher
        if (CurrentCareerSkill is MotorcycleSkill moto && moto.HasJumpBuff)
        {
            _rigidbody.AddForce(Vector2.up * (_jumpForce * 1.2f), ForceMode2D.Impulse);
            _isGrounded = false;

            if (_rigAnimator != null)
                _rigAnimator.SetTrigger("Jump");

            return; // Prohibit normal jump fallback
        }

        // Default Jump (no buff)
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

        enemy.TakeDamage(JUMP_ATTACK_DAMAGE);

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

        // 1) WinRider: Immortal buff (invulnerable)
        if (_isInvulnerable)
        {
            Debug.Log($"[Player] IGNORED {amount} damage (WinRider Invulnerable)");
            return;
        }

        // 2) MuscleDuck: Skill decides whether to block damage
        if (CurrentCareerID == DuckCareer.Muscle && CurrentCareerSkill != null)
        {
            // Skill will decide whether to block or allow damage
            CurrentCareerSkill.OnTakeDamage(this, amount);
            return; // Must return because the skill handles the rest
        }

        // 3) All other careers → Take damage immediately
        ApplyRawDamage(amount);
    }

    public void ApplyRawDamage(int amount)
    {
        _currentHealth -= amount;

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }

        _healthBarUI?.UpdateHealth(_currentHealth);
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

        _healthBarUI?.UpdateHealth(_currentHealth);

        Debug.Log($"[Player] Healed +{amount}. HP: {_currentHealth}/{_maxHealth}");
    }

    public void Revive(int reviveHP)
    {
        _isDead = false;
        _currentHealth = Mathf.Clamp(reviveHP, 1, _maxHealth);
        // Update UI / animation if available
    }


    /// <summary>
    /// Sets the player's invulnerability state.
    /// </summary>
    public void SetInvulnerable(bool state)
    {
        _isInvulnerable = state;
        // 💡 Add feedback like flashing here if needed
        Debug.Log($"[Player] Invulnerability set to: {state}");
    }


    public override void Die()
    {
        if (_isDead) return;

        
        // ⚠ If there is a death prevention skill → do not Die
        if (CurrentCareerSkill != null && CurrentCareerSkill.OnBeforeDie(this))
        {
            Debug.Log("[Player] Death intercepted by Career Skill.");
            return;
        }

        _isDead = true;

        // 1) Disable own collider → Enemies stop attacking/colliding immediately
        var coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        // 2) Disable Rigidbody interaction → prevent excessive damage registration
    #if UNITY_2022_3_OR_NEWER
        _rigidbody.linearVelocity = Vector2.zero;
    #else
        _rigidbody.velocity = Vector2.zero;
    #endif
        _rigidbody.simulated = false;

        // 3) Disable attack, card, and input systems
        if (_cardManager != null)
        _cardManager.enabled = false;

        this.enabled = false; // Disable Player.cs update for the entire class

        if (_animator != null)
            _animator.SetTrigger("Die");

        Debug.Log("[Player] Player died.");

        StartCoroutine(HandleGameOver());
    }

    private IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Instance.EndGame(); // or LoadScene("GameOver")
    }

    public DuckCareer GetCurrentCareerID()
    {
        return _careerSwitcher != null && _careerSwitcher.CurrentCareer != null
            ? _careerSwitcher.CurrentCareer.CareerID
            : DuckCareer.Duckling;
    }

    public void SetHealthBarUI(HealthBarUI healthBarUI)
        {
            _healthBarUI = healthBarUI;
            if (_healthBarUI != null)
            {
                // Initialize UI immediately if it was just set
                _healthBarUI.InitializeHealth(_maxHealth); 
            }
        }
    #endregion

    public void HookScoreUI(ScoreUI scoreUI, int initialCoin)
    {
        Debug.Log("[Player] HookScoreUI CALLED");

        if (scoreUI == null)
        {
            Debug.Log("[Player] ❌ HookScoreUI FAILED — scoreUI NULL");
            return;
        }

        Debug.Log("[Player] BEFORE HOOK delegate = " + OnCoinCollected);


        // Unsubscribe previous wrapper if exists
        if (_scoreUIHandler != null)
            OnCoinCollected -= _scoreUIHandler;

        // Create a new handler capturing the baseline initialCoin
        _scoreUIHandler = (totalCoins) => {
            int collectedThisRun = Mathf.Max(0, totalCoins - initialCoin);
            scoreUI.UpdateCoins(collectedThisRun);
        };

        // Subscribe the stored handler
        OnCoinCollected += _scoreUIHandler;

        Debug.Log("[Player] AFTER HOOK delegate = " + OnCoinCollected);

        // Initialize UI with zero collected at start
        scoreUI.UpdateCoins(0);
    }



    #region Currency System
    public void AddCoin(int amount)
    {
        if (_currency == null) return;
        _currency.AddCoin(amount);
        Debug.Log($"Coin Added → {_currency.Coin}");
        OnCoinCollected?.Invoke(_currency.Coin);

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
        if (_isDead) return;

        // Get current skill safely
        var skill = CurrentCareerSkill;

        if (skill != null)
        {
            // All careers with skills → use normally
            skill.PerformAttack(this);
        }
        else
        {
            // Fallback: If no career / no skill, use basic attack (Duckling style)
            BasicMeleeAttack();
        }
    }

    /// <summary>
    /// Basic melee attack used as a fallback when no skill is available
    /// </summary>
    private void BasicMeleeAttack()
    {
        const float range = 1.2f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
            {
                ApplyDamage(target, BASIC_ATTACK_DAMAGE);
            }
        }
    }

    public virtual void ChargeAttack(float power)
    {
        Debug.Log($"[Player] Charge attack power: {power}");
        _chargePower = power; // Store power for the skill to read
        _careerSwitcher.CurrentCareer.CareerSkill?.PerformChargeAttack(this);
    }

    public virtual void RangeAttack(Transform target)
    {
        Debug.Log($"[Player] Range attack at {target.name}");
        _careerSwitcher.CurrentCareer.CareerSkill?.PerformRangeAttack(this, target);
    }

    public virtual void ApplyDamage(IDamageable target, int amount)
    {
        if (amount <= 0)
        {
            amount = BASIC_ATTACK_DAMAGE;
        }
        target.TakeDamage(amount);
        Debug.Log($"[Player] Dealt {amount} damage to {target}");
    }

    public virtual void UseSkill()
    {
        if (_isDead) return;

        var skill = CurrentCareerSkill;

        // If no skill (Duckling or career not set) → silent, do nothing, no error
        if (skill == null)
        {
            Debug.Log($"{CurrentCareerID} did't haveskill");
            return;
        }

        skill.UseCareerSkill(this);
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
    public void EnterOverdrive()
    {
        _careerSwitcher.CurrentCareer.CareerSkill?.OnEnterOverdrive(this);
    }

    public void ExitOverdrive()
    {
        _careerSwitcher.CurrentCareer.CareerSkill?.OnExitOverdrive(this);
    }
    public void SetFXProfile(CareerEffectProfile newProfile)
    {
        _fxProfile = newProfile;
    }


    public void OnCareerChanged(DuckCareerData newCareer)
    {
        _playerData.SelectedCareer = newCareer.DisplayName;
        Debug.Log($"[Player] Career switched to: {newCareer.DisplayName}");
    }

    public bool IsDuckling =>
    _careerSwitcher != null && _careerSwitcher.IsDuckling;

    public void UpdatePlayerFormState()
    {
        // Sync data between Data and CareerSwitcher
        if (_careerSwitcher != null)
            _playerData.IsDefaultDuckling = _careerSwitcher.IsDuckling;

        if (_fxProfile == null)
        _fxProfile = Resources.Load<CareerEffectProfile>("ComicFX/Data/FXProfile_Duckling");

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


}