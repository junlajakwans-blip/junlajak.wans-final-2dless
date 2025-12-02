using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class DoggoMon : Enemy // IMoveable is redundant as Enemy already provides Move()
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    [Header("DoggoMon State")]

    private bool _isBarkingDisabled = false;
    private Vector2 _moveDirection = Vector2.left;
    private bool _isChasing;

    protected override void Update()
    {
        if (_isDisabled) return;

        // Detect is player nearby
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
            if (!_isBarkingDisabled) 
            {
                Bark();
            }
            else
            {
                // Optional: สามารถลบ Debug นี้ออกได้เมื่อระบบทำงานสมบูรณ์แล้ว
                Debug.Log("[DoggoMon] Barking attack skipped due to Chef Buff."); 
            }
        }
    }

#region Beahavior
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
        FlipSprite(dir);
        
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
        FlipSprite(_moveDirection);
    }

    private void FlipSprite(Vector2 dir)
    {
        if (dir.x != 0)
        {
            var scale = transform.localScale;
            scale.x = dir.x < 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isDisabled) return;

        if (collision.gameObject.TryGetComponent<Player>(out var player))
            //  Use Data From EnemyData:Unique | Asset: _data.DoggoDamage (เป็น Melee Damage)
            player.TakeDamage(_data.DoggoDamage);
    }
#endregion

#region Buffs
    /// <summary>
    /// Overrides base method to receive ChefDuck's Buff (Disable Barking).
    /// </summary>
    public override void ApplyCareerBuff(DuckCareerData data)
    {
        // Buff is simply a toggle for disabling the bark attack
        _isBarkingDisabled = true;
        Debug.Log("[DoggoMon] Chef Buff Applied: Barking DISABLED.");
    }
#endregion


#region Death Drop
    public override void Die()
    {

        // Cache death position for drop usage
        Vector3 pos = transform.position;

        // Drop logic based on EnemyData
        if (_data != null)
        {
            float roll = Random.value;
            float coinChance = _data.DoggoCoinDropChance; // Chance from ScriptableObject

            // Drop coin only
            if (roll < coinChance)
            {
                // Dispatch drop request to EnemySpawner
                RequestDrop(CollectibleType.Coin);
                Debug.Log($"[DoggoMon] Dropped: Coin (Chance: {coinChance * 100:F0}%)");
            }
        }
        else
        {
            Debug.LogWarning("[DoggoMon] Missing EnemyData. Drop skipped.");
        }

        // Notify spawner and return to pool
        base.Die();
    }

#endregion
}