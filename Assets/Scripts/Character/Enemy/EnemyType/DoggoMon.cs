using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class DoggoMon : Enemy // IMoveable is redundant as Enemy already provides Move()
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    [Header("DoggoMon State")]

    
    private Vector2 _moveDirection = Vector2.left;
    private bool _isChasing;

    protected override void Update()
    {
        if (_isDisabled) return;

        // Detect is player nearby
        if (_target == null)
            _target = FindFirstObjectByType<Player>()?.transform;

        if (_target == null) return;

        //  DetectPlayer uses _detectionRange loaded from Enemy.cs
        if (DetectPlayer(_target.position, _data.DoggoHauntRange))
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

    // Move Default (uses inherited Speed property, linked to _data.BaseMovementSpeed)
    public override void Move()
    {
        if (!_isDisabled)
            // Speed Property is inherited and loaded from _data.BaseMovementSpeed
            transform.Translate(_moveDirection * Speed * Time.deltaTime); 
    }

    public void ChasePlayer(Player player)
    {
        if (_isDisabled || player == null) return;

        Vector2 dir = (player.transform.position - transform.position).normalized;
        
        //  Use Data From EnemyData:Unique | Asset: _data.DoggoChaseSpeed
        transform.Translate(dir * _data.DoggoChaseSpeed * Time.deltaTime);
    }

    private void Bark()
    {
        if (_target == null || !CanDetectOverride || _isDisabled) return;

        float dist = Vector2.Distance(transform.position, _target.position);
        
        //  Use Data From EnemyData:Unique | Asset: _data.DoggoBarkRange
        if (dist <= _data.DoggoBarkRange)
        {
            Debug.Log("[DoggoMon] Bark! Player in range!");
            // NOTE: Bark attack is small, fixed damage (1)
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
            //  Use Data From EnemyData:Unique | Asset: _data.DoggoDamage (เป็น Melee Damage)
            player.TakeDamage(_data.DoggoDamage);
    }

    // ... (DisableBehavior methods omitted for brevity) ...

    public override void Die()
    {
        base.Die(); 

        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null && _data != null)
        {
            float roll = Random.value;
            
            //  Drop Chance from Asset: _data.DoggoCoinDropChance
            float coinChance = _data.DoggoCoinDropChance;
            
            // Drop Coin เท่านั้น
            if (roll < coinChance)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[DoggoMon] Dropped: Coin (Chance: {coinChance * 100:F0}%)");
            }
            
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[DoggoMon] Cannot drop item: CollectibleSpawner not found in scene!");
        }
    }
}