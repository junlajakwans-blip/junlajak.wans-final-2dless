using UnityEngine;
using System.Collections;

/// <summary>
/// DancerDuck – Evasion / Crowd Control career
/// StepDance: temporarily undetectable by enemies (2s) + stop moving enemies within 3–4 blocks.
/// 
/// BuffMon: (None currently implemented)
/// BuffMap: (None currently; reserved for future maps such as DanceHall or CityStage)
/// </summary>
public class DancerDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("DancerDuck Settings")]
    [SerializeField] private GameObject _danceEffect;
    [SerializeField] private float _speedBoost = 1.75f;
    [SerializeField] private float _hideDuration = 2f;
    [SerializeField] private float _stopRange = 3.5f;
    [SerializeField] private float _skillDuration = 22f;
    [SerializeField] private float _skillCooldown = 18f;

    private bool _isSkillActive;
    private bool _isCooldown;
    private Rigidbody2D _rb;
    private float _jumpBounceForce = 6f;
    #endregion

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        InitializeCareerMapBuff(); // เตรียมระบบ BuffMap เฉพาะอาชีพ (รองรับอนาคต)
    }

    #region Map Buff (Career-Specific, Future Ready)
    /// <summary>
    /// Checks if the current map provides any buff for DancerDuck.
    /// Future-ready design for MapBuff expansion.
    /// </summary>
    private void InitializeCareerMapBuff()
    {
        var map = GetCurrentMapType();

        // no current map buff detected
        // for future maps, can add specific buffs here
        // if (map == MapType.DanceHall) { ... } หรือ MapType.CityStage 

        switch (map)
        {
            default:
                Debug.Log($"[DancerDuck] No Map Buff applied on current map ({map}). Ready for future maps.");
                break;
        }
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        Debug.Log($"{PlayerName} uses StepDance!");
        StartCoroutine(StepDanceRoutine());
    }

    private IEnumerator StepDanceRoutine()
    {
        _isSkillActive = true;

        if (_danceEffect != null)
            Instantiate(_danceEffect, transform.position, Quaternion.identity);

        HideFromEnemies(_hideDuration);
        AffectNearbyEnemies();
        ApplySpeedModifier(_speedBoost, _skillDuration);

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
        Debug.Log("[DancerDuck] Cooldown finished!");
    }
    #endregion

    #region Enemy Interaction
    private void HideFromEnemies(float time)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        StartCoroutine(TemporarilyHide(enemies, time));
    }

    private IEnumerator TemporarilyHide(Enemy[] enemies, float time)
    {
        foreach (var enemy in enemies)
            enemy.CanDetectOverride = false;

        yield return new WaitForSeconds(time);

        foreach (var enemy in enemies)
            enemy.CanDetectOverride = true;
    }

    private void AffectNearbyEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Vector3 playerPos = transform.position;

        foreach (var enemy in enemies)
        {
            if (enemy.DetectPlayer(playerPos))
            {
                float distance = Vector2.Distance(playerPos, enemy.transform.position);
                if (distance <= _stopRange)
                {
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
        // Waving Fan – 2 block attack (ground use)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
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
            if (hit.TryGetComponent<IDamageable>(out var target))
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
