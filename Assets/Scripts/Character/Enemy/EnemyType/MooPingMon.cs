using UnityEngine;
using System.Collections;

public class MooPingMon : Enemy, IMoveable
{
    #region Fields
    [Header("MooPingMon")]
    [SerializeField] private int _fireDamage = 20;
    [SerializeField] private float _smokeRadius = 2.5f; // for FanFire()
    [SerializeField] private float _detectRange = 6f;

    [SerializeField] private GameObject _skewerProjectile; // prefab
    [SerializeField] private Transform _throwPoint;       // spawn point
    [SerializeField] private float _projectileSpeed = 5f;
    [SerializeField] private float _throwCooldown = 2.2f;

    [SerializeField] private float _patternSpeed = 2.0f; // MovePattern speed
    [SerializeField] private float _patternWidth = 1.5f; // horizontal sway

    private float _nextThrowTime;
    private Vector2 _dir = Vector2.left;
    private float _patternPhase;
    #endregion

    #region Unity
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
        MovePattern();
    }

    // Simple sway pattern for endless lanes (x oscillation)
    private void MovePattern()
    {
        _patternPhase += Time.deltaTime * _patternSpeed;
        float swayX = Mathf.Sin(_patternPhase) * _patternWidth * Time.deltaTime;
        transform.Translate(new Vector2(_dir.x * _speed * Time.deltaTime + swayX, 0f));
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

    #region Combat
    private void TryAttack()
    {
        if (_target == null) return;

        float dist = Vector2.Distance(transform.position, _target.transform.position);
        if (dist > _detectRange) return;

        // Alternate between ThrowSkewer and FanFire by cooldown window
        if (Time.time >= _nextThrowTime)
        {
            ThrowSkewer();
            _nextThrowTime = Time.time + _throwCooldown;
        }
        else
        {
            // Light pressure cone when waiting cooldown (small chance)
            if (Random.value < 0.15f) FanFire();
        }
    }

    private void ThrowSkewer()
    {
        if (_skewerProjectile == null || _throwPoint == null) return; // Removed || _target == null as aiming is not required

        // NOTE: Instantiating is generally poor practice with Pooling.
        var go = Instantiate(_skewerProjectile, _throwPoint.position, Quaternion.identity);
        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 aim = _dir; // Throw in the monster's current direction
            rb.linearVelocity = aim * _projectileSpeed;
        }

        // FIX: Set damage value using the Projectile component
        if (go.TryGetComponent<Projectile>(out var proj))
            proj.SetDamage(_fireDamage); 
    }

    // Short AOE smoke/fire puff around front arc
    private void FanFire()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _smokeRadius);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<Player>(out var player))
                player.TakeDamage(Mathf.CeilToInt(_fireDamage * 0.5f));
        }
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (_isDisabled) return;
        if (c.gameObject.TryGetComponent<Player>(out var p))
            p.TakeDamage(Mathf.CeilToInt(_fireDamage * 0.75f));
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
        yield return new WaitForSeconds(t);
        _dir = oldDir;
        _isDisabled = false;
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        // 1. Send OnEnemyDied event and self-destruct (or return to pool)
        base.Die();
        
        // 2. Drop Item Logic: Find the Spawner and command it to drop.
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 20% chance
            if (roll < 0.20f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[MooPingMon] Dropped: Coin ({roll:F2})");
            }
            // Drop Coffee with 5% chance (0.20f <= roll < 0.25f)
            else if (roll < 0.25f)
            {
                spawner.DropCollectible(CollectibleType.Coffee, transform.position);
                Debug.Log($"[MooPingMon] Dropped: Coffee ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[MooPingMon] Cannot drop item: CollectibleSpawner not found!");
        }
    }
    #endregion
}