using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; // Ensure Random refers to Unity's Random

public class MooPingMon : Enemy // Removed IMoveable as Enemy already has Move()
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    #region Fields
    [Header("Projectile Attack")]
    [SerializeField] private GameObject _skewerProjectile; // Prefab (ใช้ชื่อเป็น Tag)
    [SerializeField] private Transform _throwPoint;       // Spawn point

    [Header("Attack Delay")]
    [Tooltip("เวลาหน่วงก่อนการโจมตีครั้งแรก (วินาที)")]
    [SerializeField] private float _initialAttackDelay = 5.0f;


    // ChefDuck Buff Flag
    private bool _isThrowingDisabled = false;
    //FireFighter Buff Flag
    private bool _isBuffItemGuaranteed = false;


    private float _nextThrowTime;
    private Vector2 _dir = Vector2.left; // Default direction for the pattern
    private float _patternPhase;
    #endregion

    #region Unity
    
    protected override void Start()
    {
        base.Start();
        
        //  FIX: เพิ่ม _initialAttackDelay เข้าไปในการคำนวณครั้งแรก
         _nextThrowTime = Time.time + _data.MooPingThrowCooldown + _initialAttackDelay;
    
        //ให้มีเวลารอดูนานกว่า Cooldown ปกติ 
        _nextThrowTime = Time.time + _initialAttackDelay;
    }
    
    protected override void Update()
    {
        if (_isDisabled) return;

        // Ensure _target is a Transform
        if (_target == null) _target = FindFirstObjectByType<Player>()?.transform;
        
        Move();                      // pattern motion
        TryAttack();                 // detect + attack
    }
    #endregion

    #region Movement
    public override void Move()
    {
        // MooPingMon จะใช้ MovePattern เสมอ
        MovePattern();
    }

    /// <summary>
    /// Simple sway pattern for endless lanes (x oscillation).
    /// </summary>
    private void MovePattern()
    {
        // Use Data From EnemyData:Unique | Asset: _data.MooPingPatternSpeed
        _patternPhase += Time.deltaTime * _data.MooPingPatternSpeed;
        
        // Use Data From EnemyData:Unique | Asset: _data.MooPingPatternWidth
        float swayX = Mathf.Sin(_patternPhase) * _data.MooPingPatternWidth * Time.deltaTime;
        
        transform.Translate(new Vector2(_dir.x * Speed * Time.deltaTime + swayX, 0f)); 
    }

    public void ChasePlayer(Player player)
    {
        // Not implemented (Keep pattern)
    }

    public void Stop()
    {
        _dir = Vector2.zero;
    }

    public void SetDirection(Vector2 direction)
    {
        _dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
    }
    #endregion


#region Buffs from Career
    /// <summary>
    /// Overrides base method to receive ChefDuck's Buff (Disable ThrowSkewer).
    /// </summary>
    public override void ApplyCareerBuff(DuckCareerData data)
    {
        if (data == null) return;

        // Check which career applied the buff
        if (data.CareerID == DuckCareer.Chef)
        {
            // ChefDuck Buff Logic (Disable ThrowSkewer)
            _isThrowingDisabled = true;
            Debug.Log("[MooPingMon] Chef Buff Applied: ThrowSkewer DISABLED.");
        }
        else if (data.CareerID == DuckCareer.Firefighter)
        {
            // FireFighterDuck Buff Logic (Guaranteed Buff Item Drop)
            _isBuffItemGuaranteed = true;
            Debug.Log("[MooPingMon] FireFighter Buff Applied: Buff Item guaranteed on death.");
        }
    }
#endregion

  
    #region Combat
private void TryAttack()
    {
        if (_target == null) return;

        float dist = Vector2.Distance(transform.position, _target.transform.position);
        
        if (dist > _detectionRange) return; 

        // Alternate between ThrowSkewer and FanFire by cooldown window
        if (Time.time >= _nextThrowTime)
        {
            // FIX: Check Flag Buff before ThrowSkewer() || if ChefDuck No throwskew
            if (!_isThrowingDisabled) 
            {
                ThrowSkewer();
            } 
            else 
            {
                // if didn't throw check next cooldown
                Debug.Log("[MooPingMon] ThrowSkewer attack skipped due to Chef Buff.");
            }
            
            // Use Data From EnemyData:Unique | Asset: _data.MooPingThrowCooldown
            _nextThrowTime = Time.time + _data.MooPingThrowCooldown;
        }
        else
        {
            // Light pressure cone when waiting cooldown (small chance)
            if (Random.value < 0.15f) FanFire();
        }
    }
    private void ThrowSkewer()
    {
        if (_skewerProjectile == null || _throwPoint == null) return;

        // [FIX 2.1]: ตรวจสอบ Pool Reference ที่ถูก Inject
        if (_poolRef == null) 
        {
            Debug.LogError("[MooPingMon] Object Pool NOT INJECTED! Cannot spawn projectile.");
            return;
        }

        // NOTE: ควรใช้ Object Pooling แทน Instantiate
        string poolTag = _skewerProjectile.name; // ใช้ชื่อ Prefab เป็น Tag
        var go = _poolRef.SpawnFromPool(poolTag, _throwPoint.position, Quaternion.identity);
        
        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 aim = (_target.position - _throwPoint.position).normalized; // NOTE: ควรยิงไปหา target เสมอ
            
            // Use Data From EnemyData:Unique | Asset: _data.MooPingProjectileSpeed
            rb.linearVelocity = aim * _data.MooPingProjectileSpeed;
        }

        if (go.TryGetComponent<Projectile>(out var proj))
        {
            // Use Data From EnemyData:Unique | Asset: _data.MooPingFireDamage
            proj.SetDamage(_data.MooPingFireDamage); 
            
            // [FIX 2.3]: INJECT DEPENDENCY เข้าไปใน Projectile
            proj.SetDependencies(_poolRef, poolTag); 
        }
    }


    /// <summary>
    /// Short AOE smoke/fire puff around front arc
    /// </summary>
    private void FanFire()
    {
        // Use Data From EnemyData:Unique | Asset: _data.MooPingSmokeRadius
        var hits = Physics2D.OverlapCircleAll(transform.position, _data.MooPingSmokeRadius);
        
        foreach (var h in hits)
        {
            if (h.TryGetComponent<Player>(out var player))
            {
                // Use Data From EnemyData:Unique | Asset: _data.MooPingFireDamage
                player.TakeDamage(Mathf.CeilToInt(_data.MooPingFireDamage * 0.5f));
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (_isDisabled) return;
        if (c.gameObject.TryGetComponent<Player>(out var p))
            // Use Data From EnemyData:Unique | Asset: _data.MooPingFireDamage
            p.TakeDamage(Mathf.CeilToInt(_data.MooPingFireDamage * 0.75f));
    }
    #endregion

    #region DisableBehavior
    public override void DisableBehavior(float duration)
    {
        if (_isDisabled) return;
        StartCoroutine(DisableRoutine(duration));
    }

    private IEnumerator DisableRoutine(float t)
    {
        _isDisabled = true;
        var oldDir = _dir;
        _dir = Vector2.zero;
        base.Move(Vector2.zero); // Stop Base move
        yield return new WaitForSeconds(t);
        _dir = oldDir;
        _isDisabled = false;
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        if (_isDead) return;
        _isDead = true;
        
        CollectibleSpawner spawner = _spawnerRef;
        Vector3 enemyDeathPosition = transform.position;
        
        if (spawner != null && _data != null)
        {
            // --- 1. FireFighter Buff Drop Logic ---
            if (_isBuffItemGuaranteed)
            {
                // Drop 1 random buff item (Coffee or GreenTea - assuming CollectibleType has these)
                CollectibleType buffItem = (Random.value < 0.5f) ? CollectibleType.Coffee : CollectibleType.GreenTea;
                spawner.DropCollectible(buffItem, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
                Debug.Log($"[MooPingMon] FireFighter Bonus: Dropped guaranteed {buffItem}.");
            }


            // --- 2. Base Drop Logic (Original MooPingMon chance) ---
            float roll = Random.value;
            float totalChanceForCoffee = _data.MooPingCoinDropChance + _data.MooPingCoffeeDropChance;
            
            // Drop Coin: (roll < 20%)
            if (roll < _data.MooPingCoinDropChance)
            {
                spawner.DropCollectible(CollectibleType.Coin,enemyDeathPosition);
                Debug.Log($"[MooPingMon] Dropped: Coin ({roll:F2})");
            }
            // Drop Coffee: (20% <= roll < 25%)
            else if (roll < totalChanceForCoffee)
            {
                spawner.DropCollectible(CollectibleType.Coffee, enemyDeathPosition);
                Debug.Log($"[MooPingMon] Dropped: Coffee ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[MooPingMon] Cannot drop item: CollectibleSpawner not found or EnemyData missing!");
        }
        OnEnemyDied?.Invoke(this); // Event จะถูกส่งออกไป
    }
    #endregion
}