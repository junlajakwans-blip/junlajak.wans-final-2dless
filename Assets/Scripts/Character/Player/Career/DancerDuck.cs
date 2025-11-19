using UnityEngine;
using System.Collections;

/// <summary>
/// DancerDuck – Evasion / Crowd Control career (ID 3, Tier B)
/// StepDance: temporarily undetectable by enemies (2s) + stop moving/damage enemies within 3–4 blocks.
/// 
/// BuffMon: (None) [cite: 3]
/// BuffMap: (None) [cite: 3]
/// </summary>
public class DancerDuck : Player
{
    #region Fields
    [Header("DancerDuck Settings")]
    [SerializeField] private GameObject _danceEffect;
    [SerializeField] private float _speedBoost = 1.75f;
    [SerializeField] private float _hideDuration = 2f;    // StepDance no take any damage 2 sec [cite: 3]
    [SerializeField] private float _stopRange = 3.5f;     // StepDance 3-4 Block [cite: 3]
    [SerializeField] private float _skillDuration = 22f;  // 22 Sec [cite: 3]
    [SerializeField] private float _skillCooldown = 18f;  // 18 Sec [cite: 3]
    [SerializeField] private int _stepDanceDamage = 15; // Damage for the StepDance AOE

    private bool _isSkillActive;
    private bool _isCooldown;
    // NOTE: Removed private _rb and _jumpBounceForce as they are redundant.
    // The base Player class already handles the Rigidbody.
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies DancerDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        // PDF (Page 3) confirms Dancer has no BuffMap or BuffMon.
        // This override is intentionally left blank.
        Debug.Log($"[DancerDuck] No BuffMon or BuffMap to initialize.");
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        Debug.Log($"{PlayerName} uses StepDance!");
        StartCoroutine(StepDanceRoutine());
    }

    /// <summary>
    /// StepDance -> no take any damage -> Stun Enemy in Range [cite: 3]
    /// </summary>
    private IEnumerator StepDanceRoutine()
    {
        _isSkillActive = true;

        if (_danceEffect != null)
            Instantiate(_danceEffect, transform.position, Quaternion.identity);

        // 1. Apply "no take any damage" (by hiding from enemies) for 2s
        HideFromEnemies(_hideDuration);
        
        // 2. Apply Stun (Stop) and Damage (RangeAttack 3-4 Block) [cite: 3]
        AffectNearbyEnemies(); 
        
        // 3. Apply speed boost for the *full skill duration*
        ApplySpeedModifier(_speedBoost, _skillDuration);

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        OnSkillCooldown(); // Call the base override
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
        Debug.Log("[DancerDuck] Cooldown finished!");
    }
    #endregion

    #region Enemy Interaction
    /// <summary>
    /// Makes the player undetectable by enemies for a short duration.
    /// </summary>
    private void HideFromEnemies(float time)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        StartCoroutine(TemporarilyHide(enemies, time));
    }

    private IEnumerator TemporarilyHide(Enemy[] enemies, float time)
    {
        // Set CanDetectOverride to false to proxy "no take any damage" [cite: 3]
        foreach (var enemy in enemies)
            enemy.CanDetectOverride = false;

        yield return new WaitForSeconds(time);

        foreach (var enemy in enemies)
            enemy.CanDetectOverride = true;
    }

    /// <summary>
    /// Applies Stun (Stop) and Damage to nearby enemies (3-4 Block range) [cite: 3]
    /// </summary>
    private void AffectNearbyEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Vector3 playerPos = transform.position;

        foreach (var enemy in enemies)
        {
            if (enemy.DetectPlayer(playerPos))
            {
                float distance = Vector2.Distance(playerPos, enemy.transform.position);
                
                // Check if enemy is in the 3.5f (3-4 block) range
                if (distance <= _stopRange)
                {
                    // FIX: Added damage logic to match PDF "RangeAttack StepDance 3-4 Block" [cite: 3]
                    if (enemy.TryGetComponent<IDamageable>(out var target))
                        ApplyDamage(target, _stepDanceDamage);

                    // Apply Stun (Stop) [cite: 3]
                    if (enemy is IMoveable moveableEnemy)
                        moveableEnemy.Stop();
                }
            }
        }
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // Waving Fan – 2 block attack (ground use) [cite: 3]
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 15);
        }
    }

    public override void ChargeAttack(float power)
    {
        // Waving Fan (Charged Spin) – extend range to 4 blocks
        float attackRange = 4f;
        int baseDamage = 20;
        int scaledDamage = Mathf.RoundToInt(baseDamage * Mathf.Clamp(power, 1f, 2f));

        Debug.Log($"[{PlayerName}] performs Charged Fan Spin! Range: {attackRange} blocks, Damage: {scaledDamage}");

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, scaledDamage);
        }
    }

    public override void RangeAttack(Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.position) <= 3f)
        {
            if (target.TryGetComponent<IDamageable>(out var enemy))
                ApplyDamage(enemy, 15);
        }
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}