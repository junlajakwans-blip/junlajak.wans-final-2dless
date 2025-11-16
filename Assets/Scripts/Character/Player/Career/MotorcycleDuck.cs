using UnityEngine;
using System.Collections;

/// <summary>
/// MotorcycleDuck – Movement / Evasion career (ID 7, Tier B)
/// Skill (Street Duck): Forward Dash + 15% damage immunity.
/// BuffMon: RedlightMon -> Jump Higher.
/// BuffMap: Road Traffic -> Always Green Light.
/// </summary>
public class MotorcycleDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("Motorcycle Settings")]
    [SerializeField] private GameObject _dashEffect;
    [SerializeField] private float _dashMultiplier = 3f;   // Speed multiplier for the dash
    [SerializeField] private float _dashDuration = 0.5f;   // How long the dash burst lasts
    [SerializeField] private float _immunityChance = 0.15f; // 15% Chance no take any damage
    [SerializeField] private float _jumpBonus = 1.2f;      // Jump Higher (BuffMon)

    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 24f;   // 24 Sec
    [SerializeField] private float _skillCooldown = 18f;   // 18 Sec

    [Header("Attack Settings")]
    [SerializeField] private float _handlebarRange = 3f;   // 3 Block
    [SerializeField] private float _slideRange = 5f;       // 5 Block
    [SerializeField] private float _slideKnockback = 8f;   // Knockback Enemy

    private bool _isSkillActive; // Is the 24s skill duration active?
    private bool _isCooldown;
    private bool _isDashing;     // Is the 0.5s dash burst active?
    private bool _hasJumpBuff;   // Is BuffMon active?
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies MotorcycleDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        var map = GetCurrentMapType();

        // 1. BuffMap Logic
        // Road Traffic -> Always Green Light
        if (map == MapType.RoadTraffic)
        {
            ApplyRoadTrafficBuff();
            Debug.Log("[MotorcycleDuck] Map Buff applied: Road Traffic (All Green).");
        }

        // 2. BuffMon Logic (Passive check)
        // RedlightMon -> Jump Higher
        // We check if any RedlightMon are present to activate the passive jump buff
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.RedlightMon)
            {
                _hasJumpBuff = true;
                Debug.Log("[MotorcycleDuck] BuffMon applied: RedlightMon detected (Jump Higher).");
                break; // Only need to find one
            }
        }
    }

    private void ApplyRoadTrafficBuff()
    {
        // Find all RedlightMon and force them to Green
        RedlightMon[] trafficLights = FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in trafficLights)
        {
            // TODO: Requires a public method on RedlightMon to set state
            // light.ForceState("Green"); 
        }
        Debug.Log("[MotorcycleDuck] Forcing all traffic lights to Green (TODO).");
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(StreetDuckSkillRoutine());
    }

    /// <summary>
    /// Street Duck -> Forward Dash -> Immune
    /// </summary>
    private IEnumerator StreetDuckSkillRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: Street Duck! Duration: {_skillDuration}s");

        // 1. Apply Forward Dash (short burst)
        StartCoroutine(DashRoutine());

        // 2. Wait for the full skill duration (24s)
        yield return new WaitForSeconds(_skillDuration);
        
        _isSkillActive = false;
        OnSkillCooldown();
    }
    
    /// <summary>
    /// The short dash action (0.5s)
    /// </summary>
    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        if (_dashEffect != null)
            Instantiate(_dashEffect, transform.position, Quaternion.identity);

        // Temporarily boost speed
        ApplySpeedModifier(_dashMultiplier, _dashDuration);

        yield return new WaitForSeconds(_dashDuration);
        _isDashing = false;
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
        Debug.Log("[MotorcycleDuck] Cooldown finished!");
    }
    #endregion

    #region Immunity & Movement
    /// <summary>
    /// [Immune] If use skill 15% Chance no take any damage
    /// </summary>
    public override void TakeDamage(int damage)
    {
        // Check if skill is active and if the 15% chance succeeds
        if (_isSkillActive && Random.value < _immunityChance)
        {
            Debug.Log($"[{PlayerName}] IMMUNE! Dodged {damage} damage (15% chance).");
            return; // Skip damage
        }
        
        base.TakeDamage(damage);
    }

    /// <summary>
    /// BuffMon -> Jump Higher
    /// </summary>
    public override void Jump()
    {
        // Apply jump buff if active
        if (_hasJumpBuff)
        {
            _rigidbody.AddForce(Vector2.up * (_jumpForce * _jumpBonus), ForceMode2D.Impulse);
        }
        else
        {
            base.Jump(); // Use default jump force
        }
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] Handlebar Swing (3 Block)
        Debug.Log($"[{PlayerName}] uses Handlebar Swing (3 Block)!");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _handlebarRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 20); // Example damage
        }
    }

    public override void ChargeAttack(float power)
    {
        // Power Slide -> Knockback Enemy (5 Block)
        Debug.Log($"[{PlayerName}] uses Power Slide (5 Block)!");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _slideRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
            {
                // Apply damage
                ApplyDamage(enemy, 25);
                
                // Apply Knockback
                if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 direction = (enemy.transform.position - transform.position).normalized;
                    rb.AddForce(direction * _slideKnockback, ForceMode2D.Impulse);
                }
            }
        }
    }

    public override void RangeAttack(Transform target)
    {
        // Handlebar Swing 3 Block, Power Slide 5 Block
        // This is covered by Attack() and ChargeAttack()
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}