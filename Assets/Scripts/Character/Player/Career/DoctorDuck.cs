using UnityEngine;
using System.Collections;

/// <summary>
/// DoctorDuck – Support / Healer career
/// I'm Doctor → Throw Medic Bag to heal allies and damage nearby enemies.
///
/// BuffMon:
/// - PeterMon → 30% No Attack
///
/// BuffMap:
/// - RegenRate ×2
///
/// Passive:
/// - Self-Heal +8 HP every 2 sec
/// - Revive once with 50% HP after death (lasts 30 sec)
/// </summary>
public class DoctorDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("DoctorDuck Settings")]
    [SerializeField] private GameObject _medicBagPrefab;
    [SerializeField] private GameObject _healEffect;
    [SerializeField] private float _healRadius = 3f;
    [SerializeField] private int _healAmount = 8;
    [SerializeField] private float _healInterval = 2f;
    [SerializeField] private float _reviveHealthPercent = 0.5f;
    [SerializeField] private float _reviveDuration = 30f;
    [SerializeField] private float _skillDuration = 30f;
    [SerializeField] private float _skillCooldown = 25f;
    [SerializeField] private float _buffMonDuration = 5f;

    private bool _isSkillActive;
    private bool _isCooldown;
    private bool _canRevive = true;
    #endregion

    private void Start()
    {
        InitializeCareerMapBuff();
        StartCoroutine(SelfHealRoutine());
    }

    #region Map Buff (Regen Rate x2)
    /// <summary>
    /// Applies DoctorDuck-specific map buff (Regen ×2) when active.
    /// </summary>
    private void InitializeCareerMapBuff()
    {
        var map = GetCurrentMapType();

        switch (map)
        {
            case MapType.Kitchen:
            case MapType.School:
            case MapType.RoadTraffic:
                // เพิ่ม regen rate 2x เฉพาะในแมพใดก็ได้
                Debug.Log("[DoctorDuck] Map Buff applied: Regen Rate ×2 active.");
                break;

            default:
                Debug.Log($"[DoctorDuck] No map-specific buff active. (Current: {map})");
                break;
        }
    }
    #endregion

    #region Passive Heal
    /// <summary>
    /// Self-heal +8 HP every 2 sec while alive.
    /// </summary>
    private IEnumerator SelfHealRoutine()
    {
        while (true)
        {
            if (!_isDead && Health < MaxHealth)
            {
                Heal(_healAmount);
                Debug.Log($"[{PlayerName}] healed self for {_healAmount} HP (current {Health}/{MaxHealth}).");

                if (_healEffect != null)
                    Instantiate(_healEffect, transform.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(_healInterval);
        }
    }
    #endregion

    #region Revive Mechanic
    /// <summary>
    /// Override TakeDamage to include revive logic.
    /// </summary>
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if (_isDead && _canRevive)
        {
            StartCoroutine(ReviveRoutine());
        }
    }

    private IEnumerator ReviveRoutine()
    {
        _canRevive = false;
        _isDead = false;
        Health = Mathf.RoundToInt(MaxHealth * _reviveHealthPercent);
        Debug.Log($"[{PlayerName}] revived with {Health} HP for {_reviveDuration}s!");

        yield return new WaitForSeconds(_reviveDuration);

        if (!_isDead)
        {
            // timeout revive — if die after revive will not trigger again
            Debug.Log($"[{PlayerName}] revive duration ended.");
        }
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(ThrowMedicBagRoutine());
    }

    private IEnumerator ThrowMedicBagRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: I'm Doctor → Throw Medic Bag!");

        if (_medicBagPrefab != null)
            Instantiate(_medicBagPrefab, transform.position + Vector3.up, Quaternion.identity);

        // Heal nearby allies
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _healRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Player>(out var ally))
            {
                ally.Heal(_healAmount * 2);
                Debug.Log($"[{PlayerName}] healed ally {ally.name} for {_healAmount * 2} HP!");
            }
            else if (hit.TryGetComponent<IDamageable>(out var enemy))
            {
                ApplyDamage(enemy, 10);
            }
        }

        StartCoroutine(ApplyBuffMonRoutine());

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
        Debug.Log("[DoctorDuck] Cooldown finished!");
    }
    #endregion

    #region BuffMon Routine
    /// <summary>
    /// BuffMon: PeterMon → 30% No Attack
    /// </summary>
    private IEnumerator ApplyBuffMonRoutine()
    {
        Debug.Log($"[{PlayerName}] BuffMon applied: PeterMon reduced attack by 30%!");

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.PeterMon)
            {
                //TODO:Peter 30% No Attack
                Debug.Log($"[{PlayerName}] BuffMon applied: {enemy.EnemyType} attack reduced by 30% for {_buffMonDuration}s");
                //enemy.DisableBehavior(_buffMonDuration);
            }
        }

        yield return new WaitForSeconds(_buffMonDuration);
        Debug.Log($"[{PlayerName}] BuffMon ended.");
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // Default jump attack + throw medic bag if available
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, 10);
        }

        if (_medicBagPrefab != null)
            Instantiate(_medicBagPrefab, transform.position + Vector3.up, Quaternion.identity);

        Debug.Log($"[{PlayerName}] performs CareerAttack: I'm Doctor → Throw Medic Bag!");
    }

    public override void ChargeAttack(float power)
    {
        float range = Mathf.Lerp(2f, 3f, power);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, 15);
        }

        Debug.Log($"[{PlayerName}] ChargeAttack extended to {range} blocks!");
    }

    public override void RangeAttack(Transform target)
    {
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
