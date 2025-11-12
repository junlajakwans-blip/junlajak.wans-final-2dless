using UnityEngine;
using System.Collections;

public class GhostWorkMon : Enemy
{
    #region Fields
    [Header("GhostWorkMon Settings")]
    [SerializeField] private float _fadeDuration = 1.5f;
    [SerializeField] private float _hauntRange = 6f;
    [SerializeField] private float _teleportCooldown = 5f;
    [SerializeField] private float _baseTeleportDistance = 2f; // Distance behind player to teleport

    private float _nextTeleportTime;
    #endregion

    #region Unity Lifecycle
    public void Start()
    {
        // Set high HP to be a persistent annoyance (Damage Sponge)
        // Assuming base health is 1, set it much higher.
        _health = 10; 
        _nextTeleportTime = Time.time + _teleportCooldown;
    }

    protected override void Update()
    {
        if (_isDisabled) return;
        
        // Ensure target is set
        if (_target == null)
        {
            var player = FindFirstObjectByType<Player>();
            if (player != null) _target = player.transform;
        }

        // Standard movement (float silently)
        Move(); 

        // Try to teleport if target is detected and cooldown is ready
        if (_target != null && Time.time >= _nextTeleportTime)
        {
            TryTeleportAttack();
            _nextTeleportTime = Time.time + _teleportCooldown;
        }

        // Base attack logic (inherited from Enemy.cs)
        // base.Update() handles the Attack() call if DetectPlayer() is true.
    }
    #endregion

    #region Movement/Teleport
    public override void Move()
    {
        if (_target != null)
        {
            // Simple float toward target (inherited from base Enemy)
            base.Move();
        }
        Debug.Log($"{name} floats silently toward the player...");
    }

    private void TryTeleportAttack()
    {
        if (_target == null) return;

        // Teleport is the main attack trigger
        if (DetectPlayer(_target.position))
        {
            // Calculate a position slightly behind the player
            Vector3 teleportPos = _target.position + (transform.position - _target.position).normalized * _baseTeleportDistance;
            
            // Limit teleport range to prevent abuse
            if (Vector3.Distance(transform.position, _target.position) < _hauntRange)
            {
                // Start fade routine before teleporting (for visual effect)
                StartCoroutine(TeleportRoutine(teleportPos));
            }
        }
    }

    private IEnumerator TeleportRoutine(Vector3 targetPos)
    {
        // 1. Fade out (Visuals needed: e.g., Renderer.material.color.a = 0)
        // TODO: Implement fade out visuals
        Debug.Log($"[{name}] fades out...");
        _isDisabled = true;
        yield return new WaitForSeconds(_fadeDuration / 2f);
        
        // 2. Teleport
        transform.position = targetPos;
        Debug.Log($"{name} teleported behind you!");
        
        // 3. Fade in and attack immediately
        // TODO: Implement fade in visuals
        yield return new WaitForSeconds(_fadeDuration / 2f);
        _isDisabled = false;
        
        Attack(); // Attack immediately after reappearing
    }

    // Teleport method modified for internal routine use
    public void Teleport(Vector3 target)
    {
        // Logic moved to TeleportRoutine for visual timing
        // This method can be removed or kept as a utility if needed elsewhere.
    }
    #endregion

    #region Combat
    public override void Attack()
    {
        // Base attack (e.g., small touch damage or debuff)
        if (_target != null && _target.TryGetComponent<Player>(out var player))
        {
            // Apply small damage to be annoying
            player.TakeDamage(5); 
            Debug.Log($"{name} haunts the player with 5 damage!");
        }
    }
    #endregion

    #region Death/Drop
    public override void Die()
    {
        base.Die();

        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 45% chance
            if (roll < 0.45f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[GhostMon] Dropped: Coin ({roll:F2})");
            }
            // Drop GreenTea with 15% chance
            else if (roll < 0.60f) 
            {
                spawner.DropCollectible(CollectibleType.GreenTea, transform.position);
                Debug.Log($"[GhostMon] Dropped: GreenTea ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[GhostMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}