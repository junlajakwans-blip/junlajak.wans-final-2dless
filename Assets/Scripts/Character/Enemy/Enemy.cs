using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for all enemy types in the game.
/// Implements core attack, detection, and damage logic shared across all enemies.
/// Derived classes (e.g., DoggoMon, PeterMon) override behavior for movement or special skills.
/// </summary>
// Assume Character implements IDamageable.
// IAttackable is kept for specialized attack methods.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : Character, IAttackable 
{
    #region Fields (Serialized and Runtime)
    
    [Header("Data Link")]
    [SerializeField] protected EnemyData _data;
    
    [Header("Behavior Control")]
    [SerializeField] protected EnemyType _enemyType = EnemyType.None;
    [SerializeField] public float SpawnWeight = 30f;
    [SerializeField] protected bool _isDisabled = false;
    [SerializeField] protected Transform _target;

    // Runtime Fields (Loaded from EnemyData)
    protected int _attackPower;
    protected float _detectionRange;
    public bool CanDetectOverride = true; // Logic Flag: Can detection be overridden by external buffs?
    protected bool _isConfused = false;
    protected EnemyState _currentState = EnemyState.Idle;

    #endregion

    #region Properties
    
    public float Speed { get => _moveSpeed; set => _moveSpeed = value; } 
    public int AttackPower { get => _attackPower; set => _attackPower = value; } 
    public float DetectionRange { get => _detectionRange; set => _detectionRange = value; }     
    public EnemyType EnemyType { get => _enemyType; set => _enemyType = value; }
    
    /// <summary>Event triggered when the enemy dies. Payload: the enemy instance.</summary>
    public System.Action<Enemy> OnEnemyDied;

    // References / Dependencies (Injected by Spawner)
    protected Player _playerRef;
    protected CollectibleSpawner _spawnerRef;
    protected CardManager _cardManagerRef;
    protected BuffManager _buffManagerRef;
    protected IObjectPool _poolRef;

    #endregion

    #region Unity Lifecycle
    
    private void OnEnable()
    {
        Debug.Log($"[Enemy ENABLE] {name} | OnEnemyDied listeners = {OnEnemyDied?.GetInvocationList()?.Length ?? 0}");
    }


    protected virtual void Start() 
    {
        InitializeFromData(); 
    }

    protected virtual void Update()
    {
        if (_isDead) return; 

        // Auto-kill if enemy falls off the expected boundary
        if (transform.position.y < -10f)
        {
            Debug.Log($"[Enemy:{name}] Fell off the map, executing Die().");
            Die();
            return;
        }

        if (_isDisabled) return;
        
        // 1. Update status effects that affect movement (like Fear)
        UpdateFear();

        if (_target == null) return;

        // 2. Check for detection, then move and attack if the enemy is not Feared
        if (!_isFeared && DetectPlayer(_target.position))
        {
            Move();
            Attack();
        }
    }

    /// <summary>
    /// Checks if the enemy is in a state where it can perform actions (not dead or disabled).
    /// </summary>
    public bool CanAct()
    {
        return !_isDead && !_isDisabled;
    }

    #endregion

    #region Initialization & Dependencies
    
    /// <summary>
    /// Initializes all enemy runtime stats by loading data from the linked EnemyData ScriptableObject.
    /// </summary>
    public void InitializeFromData()
    {
        if (_data == null) 
        {
            Debug.LogError($"[Enemy:{name}] Missing EnemyData Asset! Using hardcoded defaults.");
            // Fallback: Use hardcoded defaults
            _attackPower = 10;
            _detectionRange = 5f;
            base.Initialize(1); // Character Base HP = 1
            return;
        }

        // Initialize Character Base Class (Max Health, Current Health)
        base.Initialize(_data.BaseHealth); 
        
        // Set base movement speed in Character.cs
        base._moveSpeed = _data.BaseMovementSpeed;
        
        // Link core stats
        _attackPower = _data.BaseAttackPower;
        _detectionRange = _data.BaseDetectionRange;
        
        // Set type from EnemyType ID
        _enemyType = _data.TypeID; 

        Debug.Log($"[Enemy] Initialized from Data: {_data.TypeID}. HP: {_currentHealth}, Speed: {base._moveSpeed}");
    }
    
    /// <summary>
    /// Injects necessary manager dependencies from the EnemySpawner.
    /// Sets the Player's transform as the default target.
    /// </summary>
    public void SetDependencies(Player player, CollectibleSpawner spawner, CardManager cardManager, BuffManager buffManager, IObjectPool pool)
    {
        _playerRef = player;
        // The _target field in Enemy.cs is used for movement/detection. Set it here once.
        _target = player?.transform; 

        _spawnerRef = spawner;
        _cardManagerRef = cardManager;
        _buffManagerRef = buffManager;
        _poolRef = pool;
    }

    #endregion

    #region Core Logic
    
    /// <summary>
    /// Moves toward the player if within detection range (or moves away if feared).
    /// </summary>
    public virtual void Move()
    {
        if (!CanAct() || _target == null) return;
        
        // Movement while Feared is handled by UpdateFear(), so we skip here if Feared.
        if (_isFeared) return;

        Vector3 direction = (_target.position - transform.position).normalized;
        
        // Base.Move() handles the actual physics movement
        base.Move(direction); 
    }

    /// <summary>
    /// Executes a default melee attack behavior.
    /// </summary>
    public override void Attack()
    {
        if (!CanAct()) return;
        // Specific attack logic for derived classes goes here (e.g., cooldowns, collision check)
        Debug.Log($"[{_enemyType}] attacks the player with power {_attackPower}!");
    }

    /// <summary>
    /// Checks if the player is within the enemy's default detection range.
    /// </summary>
    public virtual bool DetectPlayer(Vector3 playerPos)
    {
        return DetectPlayer(playerPos, _detectionRange); 
    }

    /// <summary>
    /// Checks if the player is within a custom detection range.
    /// Used by specific enemy types or temporary effects.
    /// </summary>
    public virtual bool DetectPlayer(Vector3 playerPos, float customRange) 
    {
        if (!CanDetectOverride) return false;
        float distance = Vector3.Distance(transform.position, playerPos);
        return distance <= customRange;
    }

    /// <summary>
    /// Applies passive or active buff effects tied to a player career (e.g., Coin bonus, Debuffs).
    /// This method is designed to be overridden by specific enemy types.
    /// </summary>
    /// <param name="data">The career data asset containing buff values.</param>
    public virtual void ApplyCareerBuff(DuckCareerData data)
    {
        // Default enemies have no career-specific buff interaction by default.
    }

    /// <summary>
    /// Disables all movement and attack behavior for a set duration.
    /// </summary>
    public virtual void DisableBehavior(float duration)
    {
        if (_isDisabled) return;
        StartCoroutine(DisableRoutine(duration));
    }

    private System.Collections.IEnumerator DisableRoutine(float time)
    {
        _isDisabled = true;
        base.Move(Vector2.zero); // stop Movement
        yield return new WaitForSeconds(time);
        _isDisabled = false;
    }


    /// <summary>
    /// Reduces health when hit by damage.
    /// Overrides the method from Character.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        Debug.Log($"[{_enemyType}] took {amount} damage! Remaining HP: {_currentHealth}");
        if (_currentHealth <= 0)
        {
            Die();
            Debug.Log($"<color=red>[Enemy] TakeDamage Die() → {name}</color>");
            return;
        }

    }
    
    #endregion

    #region Death & Pooling Helpers
    
    /// <summary>
    /// Executes the death sequence, disabling the enemy and triggering the death event.
    /// Overrides the abstract method from Character.
    /// </summary>
    public override void Die()
    {
        // Allow one-time execution even if a subclass set _isDead before calling base.Die()
        if (_isDead && OnEnemyDied == null) return;
        _isDead = true;

        // Disable Update / Movement / Attack
        enabled = false;

        // Disable collision
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        // Stop physics
        if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;

        // Stop animation
        if (_animator != null) _animator.SetFloat("MoveSpeed", 0);

        var handler = OnEnemyDied;
        if (handler != null)
        {
            handler.Invoke(this); // HandleEnemyDied 
        }
        Debug.Log($"<color=red>[Enemy] Die() → {name} Alredy Die </color>");
    }

    /// <summary>
    /// Resets the enemy state when returned to the pool (Despawned).
    /// This ensures the enemy is clean and ready for immediate reuse by the EnemySpawner's Dedicated Pool.
    /// </summary>
    public void ResetStateForPooling()
    {
        // 1. Reset Flags
        _isDead = false; 
        _isDisabled = false;
        _isConfused = false;
        _isFeared = false;
        _fearTimer = 0f;
        _currentState = EnemyState.Idle; 

        // 2. Reset Component states (re-enable components disabled in Die()/TakeDamage())
        enabled = true; // Re-enable Script
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = true; // Re-enable collider

        // 3. Reset Health/Stats
        _currentHealth = _maxHealth; // Restore full health

        // 4. Stop any running Coroutines
        StopAllCoroutines();

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

    /// <summary>
    /// Applies damage to the target (Player), checking for global passive buffs like MuscleDuck's 'No Attack' chance.
    /// </summary>
    public virtual void ApplyDamage(IDamageable target, int amount)
    {
        // Check Global/Map Buffs before attack
        Player player = _playerRef;
        
        // BuffMap: MuscleDuck (Roar Passive) -> 15% No Attack Chance
        if (player != null && player.GetCurrentCareerID() == DuckCareer.Muscle)
        {
            float noAttackChance = 0.15f; // 15%
            if (Random.value < noAttackChance)
            {
                Debug.Log($"[MuscleDuck BuffMap] Enemy attack IGNORED (15% No Attack Chance)!");
                return; // Stop all damage delivery
            }
        }
        
        // If no buff interjects, deal damage normally
        target.TakeDamage(amount);
        Debug.Log($"[{_enemyType}] dealt {amount} damage to target!");
    }
    #endregion

    #region Status Effects (Confuse & Fear)

    /// <summary>
    /// Applies the Confusion/Chaos status, causing the enemy to attack other enemies.
    /// </summary>
    public void Confuse(float duration)
    {
        if (_isDisabled || _isDead) return;
        
        // Stop the routine if it's already running to restart the duration
        StopCoroutine(nameof(ConfuseRoutine)); 
        StartCoroutine(ConfuseRoutine(duration));
    }

    private IEnumerator ConfuseRoutine(float duration)
    {
        _isConfused = true;
        Debug.Log($"[{name}] is CONFUSED! Targeting allies.");
        
        // (Optional: Animation/Visual Effect here)
        
        yield return new WaitForSeconds(duration);
        
        _isConfused = false;
        Debug.Log($"[{name}] is no longer confused.");

    }

    private bool _isFeared = false;
    private float _fearTimer = 0f;

    /// <summary>
    /// Force this enemy to run away from the player for a set duration.
    /// </summary>
    public void ApplyFear(float duration)
    {
        _isFeared = true;
        _fearTimer = duration;
        Debug.Log($"[{name}] FEARED for {duration} seconds!");
    }

    /// <summary>
    /// Updates the Fear timer and controls the enemy's fleeing movement.
    /// This runs every Update loop if the enemy is feared.
    /// </summary>
    private void UpdateFear()
    {
        if (!_isFeared) return;

        _fearTimer -= Time.deltaTime;
        if (_fearTimer <= 0f)
        {
            _isFeared = false;
            return;
        }

        if (_target != null)
        {
            // Move AWAY from the player
            Vector3 direction = (transform.position - _target.position).normalized;
            base.Move(direction); 
        }
    }
    #endregion

    #region Drop System

    /// <summary>
    /// Structure used to pass collectible drop information from the enemy to the spawner in an event.
    /// </summary>
    public struct DropRequest
    {
        public CollectibleType Type;
        public Vector3 Position;
        public DropRequest(CollectibleType t, Vector3 p)
        {
            Type = t;
            Position = p;
        }
    }

    /// <summary>
    /// Event triggered by the enemy (usually upon death) to request the spawner drops a collectible.
    /// </summary>
    public event System.Action<DropRequest> OnRequestDrop;

    /// <summary>
    /// Sends a request to the CollectibleSpawner to drop the specified type at the enemy's current position.
    /// </summary>
    /// <param name="type">The type of collectible to drop.</param>
    protected void RequestDrop(CollectibleType type)
    {
        OnRequestDrop?.Invoke(new DropRequest(type, transform.position));
    }

    /// <summary>
    /// Sends a drop request with a custom position (used for scatter / burst drop).
    /// </summary>
    protected void RequestDrop(CollectibleType type, Vector3 position)
    {
        OnRequestDrop?.Invoke(new DropRequest(type, position));
    }


    #endregion

}
