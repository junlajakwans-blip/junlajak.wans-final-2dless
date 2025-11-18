using UnityEngine;
using System.Collections;

/// <summary>
/// SingerDuck – Crowd Control / High-Tier Utility career (ID 9, Tier S)
/// Skill (Sonic Quack): Stun All in Screen + 50% Chance Meet GoldenMon.
/// BuffMon: None.
/// BuffMap: +2% chance to find GoldenMon after enemy death (Stacking).
/// </summary>
public class SingerDuck : Player
{
    #region Fields
    [Header("SingerDuck Settings")]
    [SerializeField] private GameObject _microphoneEffect; // For UseSkill()
    [SerializeField] private GameObject _highNoteEffect;   // For Attack()
    [SerializeField] private float _stunDuration = 3f;      // Duration for the screen-wide stun

    [Header("Skill Settings")]
    [SerializeField] private float _goldenMonSpawnChance = 0.5f; // 50% Chance Meet GoldenMon

    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 32f;   // 32 Sec
    [SerializeField] private float _skillCooldown = 28f;   // 28 Sec

    [Header("Attack Settings")]
    [SerializeField] private float _highNoteRange = 4f;    // 4 Block
    [SerializeField] private float _divaMinRange = 4f;    // 4-6 Block
    [SerializeField] private float _divaMaxRange = 6f;    // 4-6 Block

    private bool _isSkillActive;
    private bool _isCooldown;
    
    // +2% GoldenMon chance (Stacking) - This bool enables the buff.
    private bool _mapBuffActive = false; 
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies SingerDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        // PDF (Page 9) confirms Singer has no BuffMon.
        // BuffMap -> Add 2% found GoldenMon after other Mon die (Stack)
        
        // This buff is passive. We just log its activation.
        // The actual logic must be handled by the GameManager/EnemySpawner
        // which listens for enemy deaths and checks if this player has this buff.
        _mapBuffActive = true; 
        Debug.Log($"[SingerDuck] BuffMap applied: +2% GoldenMon chance (Stacking) is active (Handled by GameManager).");
    }

    public bool IsMapBuffActive() => _mapBuffActive;
    
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(SonicQuackRoutine());
    }

    /// <summary>
    /// Sonic Quack -> Stun All in Screen Block -> 50% Chance Meet GoldenMon 5 Sec
    /// </summary>
    private IEnumerator SonicQuackRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: Sonic Quack! Duration: {_skillDuration}s");

        if (_microphoneEffect != null)
            Instantiate(_microphoneEffect, transform.position, Quaternion.identity);

        // 1. Stun All in Screen Block
        StunAllEnemies();
        
        // 2. 50% Chance Meet GoldenMon
        TrySpawnGoldenMon();

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        OnSkillCooldown();
    }
    
    public override void OnSkillCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log("[SingerDuck] Cooldown finished!");
    }
    #endregion

    #region Skill Effects
    /// <summary>
    /// Stuns all enemies on screen using DisableBehavior.
    /// </summary>
    private void StunAllEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.DisableBehavior(_stunDuration);
            }
        }
        Debug.Log($"[SingerDuck] Stunned {enemies.Length} enemies for {_stunDuration}s.");
    }

    /// <summary>
    /// 50% chance to spawn a GoldenMon.
    /// </summary>
    private void TrySpawnGoldenMon()
    {
        if (Random.value < _goldenMonSpawnChance) // 50% chance
        {
            Debug.Log("[SingerDuck] 50% chance SUCCESS! Attempting to found GoldenMon...");
            
            // We must find the EnemySpawner (assuming it exists in the scene)
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                Vector3 spawnPos = transform.position + (Vector3.right * 2f); // ตำแหน่ง Spawn (ตัวอย่าง)
                spawner.SpawnSpecificEnemy(EnemyType.GoldenMon, spawnPos);
                
                Debug.Log("[SingerDuck] GoldenMon Spawned!");
            }
            else
            {
                Debug.LogWarning("[SingerDuck] GoldenMon not found!");
            }
        }
        else
        {
            Debug.Log("[SingerDuck] No GoldenMon this time.");
        }
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] High Note (AOE) 4 Block
        Debug.Log($"[{PlayerName}] uses High Note (4 Block)!");
        if (_highNoteEffect != null)
            Instantiate(_highNoteEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _highNoteRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 15); // Example damage
        }
    }

    public override void ChargeAttack(float power)
    {
        // Diva -> Add Lenght Attack (4-6 Block)
        float range = Mathf.Lerp(_divaMinRange, _divaMaxRange, power);
        Debug.Log($"[{PlayerName}] uses Diva Charge Attack! (Range {range:F1} Block)!");
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 25); // Example damage
        }
    }

    public override void RangeAttack(Transform target)
    {
        // Covered by Attack() (4 Block) and ChargeAttack() (4-6 Block)
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}