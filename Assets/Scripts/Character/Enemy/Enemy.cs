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

    public bool IsDead => _currentHealth <= 0; // _currentHealth from Character.cs
    
    #endregion

#region Unity Lifecycle
    
    protected virtual void Start() 
    {
        InitializeFromData(); 
        
    }

    protected virtual void Update()
    {
        if (_target == null) return;

        if (DetectPlayer(_target.position))
        {
            if (_isDisabled) return;
            Move();
            Attack();
        }
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


    #region Core Logic
    
    
    /// <summary>
    /// Moves toward the player if within detection range.
    /// </summary>
    public virtual void Move()
    {
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
        Debug.Log($"[{_enemyType}] attacks the player with power {_attackPower}!");
    }

    /// <summary>
    /// (รDefault)
    /// For normal Update()
    /// </summary>
    public virtual bool DetectPlayer(Vector3 playerPos)
    {
        // เรียกใช้ร่าง 2 โดยใช้ค่า default _detectionRange
        return DetectPlayer(playerPos, _detectionRange); 
    }

    /// <summary>
    /// (ร่างที่ 2: Custom)
    /// For Monter custom Range (เช่น GhostWorkMon) 
    /// </summary>
    public virtual bool DetectPlayer(Vector3 playerPos, float customRange) 
    {
    if (!CanDetectOverride) return false;
    float distance = Vector3.Distance(transform.position, playerPos);
    return distance <= customRange;
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
        //Call Take Damage from Character
        base.TakeDamage(amount); 
        
        Debug.Log($"[{_enemyType}] took {amount} damage! Remaining HP: {_currentHealth}"); // ใช้ _currentHealth จาก Base
        
    }
    
    //Die form abstract Character
    public override void Die() 
    {
        Debug.Log($"[{_enemyType}] has been defeated.");

        OnEnemyDied?.Invoke(this);
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
}
