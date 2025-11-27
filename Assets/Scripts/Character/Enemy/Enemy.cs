using UnityEngine;
using System.Collections; // Added for Coroutines

/// <summary>
/// Base class for all enemy types in the game.
/// Implements core attack, detection, and damage logic shared across all enemies.
/// Derived classes (e.g., DoggoMon, PeterMon) override behavior for movement or special skills.
/// </summary>
// NOTE: IAttackable, IDamageable are no longer explicitly needed here if Character implements them.
// Assume Character implements IDamageable.
// We keep IAttackable for specialized attack methods.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : Character, IAttackable 
{
    // ====================================================================================
    // NOTE: Stat Fields in Character.cs and link from EnemyData.asset 
    // ====================================================================================

    [Header("Data Link")]
    [SerializeField] protected EnemyData _data;
    
    #region Fields
        
    [SerializeField] protected Transform _target;
    [SerializeField] protected EnemyType _enemyType = EnemyType.None;
    [SerializeField] public float SpawnWeight = 30f;
    [SerializeField] protected bool _isDisabled = false;


    // Runtime Fields (Loaded from EnemyData)
    //  protected fields from Asset
    protected int _attackPower;
    protected float _detectionRange;
    public bool CanDetectOverride = true; // Logic Flag
    protected bool _isConfused = false;
    protected EnemyState _currentState = EnemyState.Idle;
    #endregion

    #region Properties
    
    public float Speed { get => _moveSpeed; set => _moveSpeed = value; } 
    public int AttackPower { get => _attackPower; set => _attackPower = value; } 
    public float DetectionRange { get => _detectionRange; set => _detectionRange = value; }     
    public EnemyType EnemyType { get => _enemyType; set => _enemyType = value; }
    public System.Action<Enemy> OnEnemyDied;

    //For Dependenciess
    protected Player _playerRef;
    protected CollectibleSpawner _spawnerRef;
    protected CardManager _cardManagerRef;
    protected BuffManager _buffManagerRef;
    protected IObjectPool _poolRef;

    #endregion

#region Unity Lifecycle
    
    protected virtual void Start() 
    {
        InitializeFromData(); 
        
    }

    protected virtual void Update()
    {
        if (_isDead)
        return; 

        // ถ้าตกจากฉาก
        if (transform.position.y < -10f)
        {
            Die();
            return;
        }

        if (_isDisabled) return;
        if (_target == null) return;

        if (DetectPlayer(_target.position))
        {
            Move();
            Attack();
        }
    }

    public bool CanAct()
    {
        return !_isDead && !_isDisabled;
    }

    #endregion

    #region Initialization
    
    /// <summary>
    /// Initializes all enemy runtime stats by loading data from the linked EnemyData ScriptableObject.
    /// </summary>
    public void InitializeFromData()
    {
        if (_data == null) 
        {
            Debug.LogError($"[Enemy:{name}] Missing EnemyData Asset! Using hardcoded defaults.");
            // Fallback: Plan 2 set num insted link Data
            _attackPower = 10;
            _detectionRange = 5f;
            base.Initialize(1); // Character Base HP = 1
            return;
        }

        // Character.Initialize() set _maxHealth and _currentHealth
        base.Initialize(_data.BaseHealth); 
        
        // Set _moveSpeed in Character.cs
        base._moveSpeed = _data.BaseMovementSpeed;
        
        // Link stat
        _attackPower = _data.BaseAttackPower;
        _detectionRange = _data.BaseDetectionRange;
        
        // type from EnemyType ID
        _enemyType = _data.TypeID; 

        Debug.Log($"[Enemy] Initialized from Data: {_data.TypeID}. HP: {_currentHealth}, Speed: {base._moveSpeed}");
    }
    #endregion


    #region  Dependencies
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
    /// Moves toward the player if within detection range.
    /// </summary>
    public virtual void Move()
    {
        if (!CanAct()) return;
        if (_target == null) return;

        Vector3 direction = (_target.position - transform.position).normalized;
        
        //  Base.Move() from Character.cs
        base.Move(direction); 
    }

    /// <summary>
    /// Executes a default melee attack behavior.
    /// </summary>
    public override void Attack()
    {
        if (!CanAct()) return;
        Debug.Log($"[{_enemyType}] attacks the player with power {_attackPower}!");
    }

    /// <summary>
    /// (Default)
    /// For normal Update()
    /// </summary>
    public virtual bool DetectPlayer(Vector3 playerPos)
    {
        
        return DetectPlayer(playerPos, _detectionRange); 
    }

    /// <summary>
    /// (Custom)
    /// For Monter custom Range (ex GhostWorkMon) 
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
    /// </summary>
    public override void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        Debug.Log($"[{_enemyType}] took {amount} damage! Remaining HP: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();   // 
            return;
        }

        UpdateHealthBar();
    }

    //Die form abstract Character

    public override void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // 🔹 ปิดสคริปต์ทันที (หยุด Update → ไม่ Attack / Move / Detect อีก)
        enabled = false;

        // 🔹 ปิดการชน
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        // 🔹 หยุดฟิสิกส์
        if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;

        // 🔹 หยุด Animator (ถ้ามี)
        if (_animator != null) _animator.SetFloat("MoveSpeed", 0);

        // 🔹 เก็บ handler ก่อน invoke
        var handler = OnEnemyDied;
        OnEnemyDied = null; // ❗ กัน invoke ซ้ำ ไม่ลูป ไม่ spam

        handler?.Invoke(this); // ส่งไปที่ Spawner
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
                Debug.Log($"<color=green>[MuscleDuck BuffMap]</color> Enemy attack IGNORED (15% No Attack Chance)!");
                return; // หยุดการโจมตีทั้งหมด
            }
        }
        
        // Of no buff fight with normal way
        target.TakeDamage(amount);
        Debug.Log($"[{_enemyType}] dealt {amount} damage to target!");
    }
    #endregion

    /// <summary>
    /// Applies the Confusion/Chaos status, causing the enemy to attack other enemies.
    /// </summary>
    public void Confuse(float duration)
    {
        if (_isDisabled || _isDead) return;
        
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

#region Fear System
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
    /// Update Fear timer and flee movement
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
            Vector3 direction = (transform.position - _target.position).normalized;
            base.Move(direction); // move AWAY from the player
        }
    }
    #endregion


}
