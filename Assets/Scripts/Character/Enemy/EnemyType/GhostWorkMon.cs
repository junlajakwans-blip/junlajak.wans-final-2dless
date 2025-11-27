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
        if (_isDead) return;
        _isDead = true;
        StopAllCoroutines();
        
        CollectibleSpawner spawner = _spawnerRef;
         Vector3 enemyDeathPosition = transform.position;
        
        if (spawner != null && _data != null)
        {
            float roll = Random.value;
            
            //  Drop Chance from Asset
            float coinChance = _data.GhostWorkCoinDropChance;
            float greenTeaChance = _data.GhostWorkGreenTeaDropChance;
            float totalGreenTeaChance = coinChance + greenTeaChance;
            
            // Drop Coin: (roll < 45%)
            if (roll < coinChance)
            {
                spawner.DropCollectible(CollectibleType.Coin, enemyDeathPosition);
            }
            // Drop GreenTea: (45% <= roll < 60%)
            else if (roll < totalGreenTeaChance) 
            {
                spawner.DropCollectible(CollectibleType.GreenTea, enemyDeathPosition);
            }
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[GhostWorkMon] CollectibleSpawner NOT INJECTED! Cannot drop items.");
        }
        OnEnemyDied?.Invoke(this); // Event จะถูกส่งออกไป
    }
    #endregion
}