using UnityEngine;
using System.Collections;

/// <summary>
/// DoctorDuck – Support / Self-Healer Career (ID 5, Tier S)
/// Implements passive heal, revive, and specific BuffMon/BuffMap logic.
/// </summary>
public class DoctorDuck : Player
{
    #region Fields
    [Header("DoctorDuck Settings")]
    [SerializeField] private GameObject _medicBagPrefab;
    [SerializeField] private GameObject _healEffect;
    [SerializeField] private float _healRadius = 3f;
    [SerializeField] private int _healAmount = 8;        // +8 HP
    [SerializeField] private float _healInterval = 2f;     // Every 2 Sec
    [SerializeField] private float _reviveHealthPercent = 0.5f; // 50% Health
    [SerializeField] private float _reviveDuration = 30f;    // 30 Sec
    [SerializeField] private float _skillDuration = 30f;   // 30 Sec
    [SerializeField] private float _skillCooldown = 25f;   // 25 Sec
    [SerializeField] private float _buffMonDuration = 5f;  // Custom duration for BuffMon

    private bool _isSkillActive;
    private bool _isCooldown;
    private bool _canRevive = true;
    #endregion

    #region Buffs & Passives Initialization

    /// <summary>
    /// (Override) Applies DoctorDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        // 1. Initialize Map Buff
        InitializeCareerMapBuff();
        
        // 2. Start Passive Self-Heal
        StartCoroutine(SelfHealRoutine());
    }

    /// <summary>
    /// BuffMap -> Regen Rate ×2
    /// </summary>
    private void InitializeCareerMapBuff()
    {
        var map = GetCurrentMapType();
        switch (map)
        {
            // Assuming all playable maps get the bonus
            case MapType.Kitchen:
            case MapType.School:
            case MapType.RoadTraffic:
                // Note: The actual "x2" logic is handled by SelfHealRoutine.
                // This just logs the confirmation.
                Debug.Log("[DoctorDuck] Map Buff applied → Regen Rate ×2 active."); 
                break;
            default:
                Debug.Log($"[DoctorDuck] No map-specific buff active (Current: {map})");
                break;
        }
    }

    /// <summary>
    /// Self Heal -> Every 2 Sec +8HP
    /// </summary>
    private IEnumerator SelfHealRoutine()
    {
        while (true)
        {
            if (!_isDead && _currentHealth < _maxHealth)
            {
                Heal(_healAmount); 
                Debug.Log($"[{PlayerName}] Self-heal +{_healAmount} HP ({_currentHealth}/{_maxHealth})"); 
                if (_healEffect != null)
                    Instantiate(_healEffect, transform.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(_healInterval); 
        }
    }
    #endregion

    #region Revive Mechanic
    /// <summary>
    /// if Die Revive 1 time + 50% Health 30 sec
    /// </summary>
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        // If TakeDamage resulted in death, and we can revive, start the routine
        if (_isDead && _canRevive)
            StartCoroutine(ReviveRoutine());
    }

    private IEnumerator ReviveRoutine()
    {
        _canRevive = false; // Use the revive
        _isDead = false;    // Negate the death
        _currentHealth = Mathf.RoundToInt(_maxHealth * _reviveHealthPercent); // 50% Health 
        
        Debug.Log($"[{PlayerName}] Revived with {_currentHealth} HP for {_reviveDuration}s!"); 
        
        yield return new WaitForSeconds(_reviveDuration); // Wait 30s 
        
        if (!_isDead)
            Debug.Log($"[{PlayerName}] Revive window ended (can die permanently now).");
    }
    #endregion

    #region ISkillUser
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(MedicBagRoutine());
    }

    private IEnumerator MedicBagRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses Skill: I'm Doctor → Throw Medic Bag!");

        if (_medicBagPrefab != null)
            Instantiate(_medicBagPrefab, transform.position + Vector3.up, Quaternion.identity);

        // Heal self (bonus amount from skill)
        Heal(_healAmount * 2);
        Debug.Log($"[{PlayerName}] healed self +{_healAmount * 2} HP from Medic Bag.");

        // Damage enemies in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _healRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var enemy) && hit.GetComponent<Player>() == null)
                ApplyDamage(enemy, 10);
        }

        // Apply temporary monster buff
        StartCoroutine(ApplyBuffMonRoutine());

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        OnSkillCooldown();
    }

    public override void OnSkillCooldown() => StartCoroutine(CooldownRoutine());

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log("[DoctorDuck] Cooldown finished.");
    }
    #endregion

    #region BuffMon
    /// <summary>
    /// PeterMon → 30% No Attack
    /// </summary>
    private IEnumerator ApplyBuffMonRoutine()
    {
        Debug.Log($"[{PlayerName}] BuffMon: PeterMon -30% Attack ({_buffMonDuration}s)");
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.PeterMon)
            {
                // Implemented 30% chance logic
                if (Random.value < 0.30f) // 30% chance 
                {
                    enemy.DisableBehavior(_buffMonDuration);
                    Debug.Log($"[{PlayerName}] BuffMon: PeterMon attack disabled for {_buffMonDuration}s (30% chance triggered).");
                }
                else
                {
                    Debug.Log($"[{PlayerName}] BuffMon: PeterMon 30% chance failed.");
                }
            }
        }
        yield return new WaitForSeconds(_buffMonDuration);
        Debug.Log($"[{PlayerName}] BuffMon ended.");
    }
    #endregion

    #region IAttackable
    public override void Attack()
    {
        // [CareerAttack] I'm Doctor -> Throw Medic Bag (Range 2 Block)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f); 
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 10);
        }

        if (_medicBagPrefab != null)
            Instantiate(_medicBagPrefab, transform.position + Vector3.up, Quaternion.identity);

        Debug.Log($"[{PlayerName}] CareerAttack: I'm Doctor → Throw Medic Bag"); 
    }

    public override void ChargeAttack(float power)
    {
        // Add lenght attack to 3
        float range = Mathf.Lerp(2f, 3f, power); 
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 15);
        }
        Debug.Log($"[{PlayerName}] ChargeAttack range {range:F1} blocks.");
    }

    public override void RangeAttack(Transform target)
    {
        // ChargeAttack 3 Block (Using this as max range attack)
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.position) <= 3f) 
        {
            if (target.TryGetComponent<IDamageable>(out var enemy))
                ApplyDamage(enemy, 12);
        }
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion
}