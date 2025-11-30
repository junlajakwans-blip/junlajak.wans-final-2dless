using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class GhostWorkMon : Enemy
{
    // NOTE: _data field is inherited from Enemy.cs

    private float _nextTeleportTime;    

    #region Unity Lifecycle
    
    protected override void Start()
    {
        base.Start();
        _nextTeleportTime = Time.time + _data.GhostWorkTeleportCooldown;
        
    }

    protected override void Update()
    {
        if (_isDisabled) return;
        
        if (_target == null) return; 
        
        // Standard movement (float silently)
        Move(); 

        // Try to teleport if target is detected and cooldown is ready
        if (_target != null && Time.time >= _nextTeleportTime)
        {
            TryTeleportAttack();
            _nextTeleportTime = Time.time + _data.GhostWorkTeleportCooldown;
        }
    }
    
    #endregion

    #region Combat
    
    public void TryTeleportAttack()
    {
        //  Use Data From EnemyData:Unique | Asset: _data.GhostWorkHauntRange
        if (DetectPlayer(_target.position, _data.GhostWorkHauntRange))
        {
            StartCoroutine(TeleportRoutine());
        }
    }
    
    private IEnumerator TeleportRoutine()
    {
        // 1. Fade Out
        //  Use Data From EnemyData:Unique | Asset: _data.GhostWorkFadeDuration
        Debug.Log($"{name} fading out for {_data.GhostWorkFadeDuration}s...");
        // TODO: Implement actual visual fade out using Coroutine and Renderer
        yield return new WaitForSeconds(_data.GhostWorkFadeDuration);

        // 2. Calculate New Position
        //  Use Data From EnemyData:Unique | Asset: _data.GhostWorkBaseTeleportDistance
        Vector3 playerPos = _target.position;
        Vector3 teleportOffset = Vector3.left * _data.GhostWorkBaseTeleportDistance;
        transform.position = playerPos + teleportOffset;
        
        // 3. Fade In
        //  Use Data From EnemyData:Unique | Asset: _data.GhostWorkFadeDuration
        Debug.Log($"{name} fading in at new position.");
        // TODO: Implement actual visual fade in
        yield return new WaitForSeconds(_data.GhostWorkFadeDuration);
        
        // 4. Attack Immediately
        Attack();
    }
    
    public override void Attack()
    {
        // Base attack (e.g., small touch damage or debuff)
        if (_target != null && _target.TryGetComponent<Player>(out var player))
        {
            //  Use Data From EnemyData:Unique | Asset: _data.GhostWorkHauntDamage
            player.TakeDamage(_data.GhostWorkHauntDamage);
            Debug.Log($"{name} haunts the player with {_data.GhostWorkHauntDamage} damage!");
        }
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        // Guard: already dead
        if (_isDead) return;
        _isDead = true;

        // Stop behaviors immediately
        StopAllCoroutines();

        // Cache death position if needed for debugging
        Vector3 pos = transform.position;

        // Drop logic based on EnemyData
        if (_data != null)
        {
            float roll = Random.value;
            float coinChance = _data.GhostWorkCoinDropChance;               // 45% (ตัวอย่าง)
            float greenTeaChance = _data.GhostWorkGreenTeaDropChance;       // 15% (ตัวอย่าง)
            float totalGreenTeaChance = coinChance + greenTeaChance;

            // Drop Coin (roll < coinChance)
            if (roll < coinChance)
            {
                RequestDrop(CollectibleType.Coin);
                Debug.Log($"[GhostWorkMon] Dropped: Coin ({coinChance * 100:F0}%)");
            }
            // Drop GreenTea (coinChance <= roll < totalGreenTeaChance)
            else if (roll < totalGreenTeaChance)
            {
                RequestDrop(CollectibleType.GreenTea);
                Debug.Log($"[GhostWorkMon] Dropped: GreenTea ({greenTeaChance * 100:F0}%)");
            }
        }
        else
        {
            Debug.LogWarning("[GhostWorkMon] EnemyData missing. Drop skipped.");
        }

        // Notify spawner and return to pool
        base.Die();
    }

    #endregion
}