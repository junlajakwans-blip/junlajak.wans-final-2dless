// DoggoMon.cs

using UnityEngine;
using System.Collections;

public class DoggoMon : Enemy, IMoveable
{
    [Header("DoggoMon Settings")]
    [SerializeField] private float _chaseSpeed = 2.5f;
    [SerializeField] private float _barkRange = 1.25f;
    [SerializeField] private int _damage = 15;

    private Vector2 _moveDirection = Vector2.left;
    private bool _isChasing;

    protected override void Update()
    {
        if (_isDisabled) return;

        // Detect is player nearby
        if (_target == null)
            _target = FindFirstObjectByType<Player>()?.transform;

        if (_target == null) return;


        if (DetectPlayer(_target.position))
            _isChasing = true;

        if (_isChasing)
        {
            ChasePlayer(_target.GetComponent<Player>());
        }
        else
        {
            Move();

            Bark();
        }
    }

    // Move Default
    public override void Move()
    {
        if (!_isDisabled)
            transform.Translate(_moveDirection * _speed * Time.deltaTime);
    }

    public void ChasePlayer(Player player)
    {
        if (_isDisabled || player == null) return;

        Vector2 dir = (player.transform.position - transform.position).normalized;
        transform.Translate(dir * _chaseSpeed * Time.deltaTime);
    }

    private void Bark()
    {
        if (_target == null || !CanDetectOverride || _isDisabled) return;

        float dist = Vector2.Distance(transform.position, _target.position);
        if (dist <= _barkRange)
        {
            Debug.Log("[DoggoMon] Bark! Player in range!");
            _target.GetComponent<Player>()?.TakeDamage(1); 
        }
    }

    public void Stop()
    {
        _moveDirection = Vector2.zero;
        _isChasing = false;
    }

    public void SetDirection(Vector2 direction)
    {
        _moveDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isDisabled) return;

        if (collision.gameObject.TryGetComponent<Player>(out var player))
            player.TakeDamage(_damage);
    }

    public override void DisableBehavior(float duration)
    {
        if (_isDisabled) return;
        StartCoroutine(DisableRoutine(duration));
    }

    private IEnumerator DisableRoutine(float time)
    {
        _isDisabled = true;
        CanDetectOverride = false; 
        var oldDir = _moveDirection;
        _moveDirection = Vector2.zero;
        _isChasing = false;

        yield return new WaitForSeconds(time);

        _moveDirection = oldDir;
        CanDetectOverride = true;
        _isDisabled = false;
    }

    public override void Die()
    {
        // 1. Call Base Class For Event OnEnemyDied And Destroy this Object 
        base.Die(); 

        // 2. Drop Item
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 30% chance (0.00% <= roll < 30.00%)
            if (roll < 0.30f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[DoggoMon] Dropped: Coin ({roll:F2})");
            }
            // Drop Coffee with 3% chance (30.00% <= roll < 33.00%)
            else if (roll < 0.33f)
            {
                spawner.DropCollectible(CollectibleType.Coffee, transform.position);
                Debug.Log($"[DoggoMon] Dropped: Coffee ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[DoggoMon] Cannot drop item: CollectibleSpawner not found in scene!");
        }
    }
}