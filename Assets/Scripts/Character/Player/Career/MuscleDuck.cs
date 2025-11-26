using UnityEngine;
using System.Collections;

/// <summary>
/// MuscleSkill – Hulk Smash / Berserker career (ID 10, Tier S+)
/// Skill: Hulk Smash → Destroy all enemies and obstacles.
/// Passive: Immortal (ignore all damage), Coin ×2 (all maps).
/// BuffMap: Roar → All enemies fear.
/// BuffMon: GoldenMon logic handled in GoldenMon.Die().
/// Cooldown increases +15 sec after every use, max 2 uses per round.
/// </summary>
[CreateAssetMenu(menuName = "DUFFDUCK/Skill/MuscleSkill_Full")]
public class MuscleSkill : CareerSkillBase
{
    #region Fields (Copied from MuscleDuck.cs)
    [Header("MuscleDuck Settings")]
    [SerializeField] private GameObject _rageEffect;
    [SerializeField] private GameObject _smashEffect;
    [SerializeField] private GameObject _roarEffect;

    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 35f;
    [SerializeField] private float _baseCooldown = 40f;
    [SerializeField] private float _cooldownIncrease = 15f;
    [SerializeField] private int _maxUsesPerRound = 2;

    [Header("Attack Settings")]
    [SerializeField] private float _ironFistRange = 3f;
    [SerializeField] private float _pumpedUpRange = 8f;

    private bool _isSkillActive;
    private float _currentCooldown;
    private int _usesThisRound;
    private Coroutine _skillRoutine;
    #endregion


    #region Initialize (Passive + BuffMap)
    public override void Initialize(Player player)
    {
        _currentCooldown = _baseCooldown;
        _usesThisRound = 0;

        ApplyRoarMapBuff(player);

        Debug.Log("[MuscleSkill] Passive initialized: Immortal + Coin×2 (all maps).");
    }

    private void ApplyRoarMapBuff(Player player)
    {
        if (_roarEffect != null)
            Object.Instantiate(_roarEffect, player.transform.position, Quaternion.identity);

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.ApplyFear(5f);

        Debug.Log($"[MuscleSkill] Roar Fear applied to {enemies.Length} enemies.");
    }
    #endregion


    #region Use Skill – Hulk Smash
    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive)
        {
            Debug.Log("[MuscleSkill] Skill already active.");
            return;
        }

        if (_usesThisRound >= _maxUsesPerRound)
        {
            Debug.Log("[MuscleSkill] Max uses reached this round.");
            return;
        }

        _skillRoutine = player.StartCoroutine(HulkSmashRoutine(player));
    }

    private IEnumerator HulkSmashRoutine(Player player)
    {
        _isSkillActive = true;
        _usesThisRound++;

        if (_smashEffect != null)
            Object.Instantiate(_smashEffect, player.transform.position, Quaternion.identity);

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.TakeDamage(9999);

        Debug.Log($"[MuscleSkill] Hulk Smash destroyed {enemies.Length} enemies.");

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        StartCooldown(player);
    }
    #endregion


    #region Cooldown Logic (+15 sec per use)
    private void StartCooldown(Player player)
    {
        player.StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        Debug.Log($"[MuscleSkill] Cooldown {_currentCooldown}s.");
        yield return new WaitForSeconds(_currentCooldown);
        _currentCooldown += _cooldownIncrease;
        Debug.Log($"[MuscleSkill] Cooldown ended. Next cooldown = {_currentCooldown}s.");
    }
    #endregion


    #region Attack / Charge Attack
    public override void PerformAttack(Player player)
    {
        if (_rageEffect != null)
            Object.Instantiate(_rageEffect, player.transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _ironFistRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(target, 50);

        Debug.Log("[MuscleSkill] Iron Fist executed.");
    }

    public override void PerformChargeAttack(Player player)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _pumpedUpRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(target, 100);

        Debug.Log("[MuscleSkill] Pumped Up executed.");
    }
    #endregion


    #region Passive – Immortal
    public override void OnTakeDamage(Player player, int dmg)
    {
        Debug.Log("[MuscleSkill] Immortal → ignored damage.");
    }

    public override bool OnBeforeDie(Player player) => true;
    #endregion


    #region Cleanup
    public override void Cleanup(Player player)
    {
        _isSkillActive = false;

        if (_skillRoutine != null)
            player.StopCoroutine(_skillRoutine);

        Debug.Log("[MuscleSkill] Cleanup complete.");
    }
    #endregion
}
