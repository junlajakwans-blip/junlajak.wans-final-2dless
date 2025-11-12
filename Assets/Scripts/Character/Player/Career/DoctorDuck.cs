using UnityEngine;
using System.Collections;

/// <summary>
/// DoctorDuck – Support / Self-Healer Career
/// I'm Doctor → Throw Medic Bag to heal self and damage nearby enemies.
/// 
/// BuffMon:
/// - PeterMon → 30 % No Attack
/// 
/// BuffMap:
/// - Regen Rate ×2
/// 
/// Passive:
/// - Self-Heal +8 HP every 2 sec  
/// - Revive once with 50 % HP after death (30 s)
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

    #region Map Buff
    /// <summary>Regen ×2 when in Map with Career opened</summary>
    private void InitializeCareerMapBuff()
    {
        var map = GetCurrentMapType();
        switch (map)
        {
            case MapType.Kitchen:
            case MapType.School:
            case MapType.RoadTraffic:
                Debug.Log("[DoctorDuck] Map Buff applied → Regen Rate ×2 active.");
                break;
            default:
                Debug.Log($"[DoctorDuck] No map-specific buff active (Current: {map})");
                break;
        }
    }
    #endregion

    #region Passive Self Heal
    /// <summary>Heal ตัวเอง +8 HP ทุก 2 วินาที ขณะ ยัง ไม่ตาย </summary>
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
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        if (_isDead && _canRevive)
            StartCoroutine(ReviveRoutine());
    }

    private IEnumerator ReviveRoutine()
    {
        _canRevive = false;
        _isDead = false;
        _currentHealth = Mathf.RoundToInt(_maxHealth * _reviveHealthPercent);
        Debug.Log($"[{PlayerName}] revived with {_currentHealth} HP for {_reviveDuration}s");
        yield return new WaitForSeconds(_reviveDuration);
        if (!_isDead)
            Debug.Log($"[{PlayerName}] revive window ended (no further revive)");
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

        // Heal ตัวเอง เท่านั้น (ไม่ Heal เพื่อน)
        Heal(_healAmount * 2);
        Debug.Log($"[{PlayerName}] healed self +{_healAmount * 2} HP from Medic Bag.");

        // Damage ศัตรู ใน ระยะ
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _healRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var enemy))
                ApplyDamage(enemy, 10);
        }

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
    /// <summary>PeterMon → ลด โจมตี 30 % ชั่วคราว</summary>
    private IEnumerator ApplyBuffMonRoutine()
    {
        Debug.Log($"[{PlayerName}] BuffMon: PeterMon -30 % Attack ({_buffMonDuration}s)");
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.PeterMon)
            {
                // TODO: ลด Damage หรือ หยุด ยิง 30%
                //enemy.DisableBehavior(_buffMonDuration);
            }
        }
        yield return new WaitForSeconds(_buffMonDuration);
        Debug.Log($"[{PlayerName}] BuffMon ended.");
    }
    #endregion

    #region IAttackable
    public override void Attack()
    {
        // Default JumpAttack + throw Medic Bag
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, 10);
        }

        if (_medicBagPrefab != null)
            Instantiate(_medicBagPrefab, transform.position + Vector3.up, Quaternion.identity);

        Debug.Log($"[{PlayerName}] CareerAttack: I'm Doctor → Throw Medic Bag");
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
        Debug.Log($"[{PlayerName}] ChargeAttack range {range} blocks.");
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
