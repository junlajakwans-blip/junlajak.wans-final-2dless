using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/DancerSkill_Full")]
public class DancerSkill : CareerSkillBase
{
    #region 🔹 Fields (คัดจาก DancerDuck เดิม)
    [Header("Dancer Settings (Copied from DancerDuck.cs)")]
    [SerializeField] private GameObject _danceEffect;
    [SerializeField] private float _speedBoost = 1.75f;
    [SerializeField] private float _hideDuration = 2f;   // No take any damage 2 sec
    [SerializeField] private float _stopRange = 3.5f;    // StepDance range 3–4 blocks
    [SerializeField] private float _skillDuration = 22f;
    [SerializeField] private float _skillCooldown = 18f;
    [SerializeField] private int _stepDanceDamage = 15;

    private bool _isSkillActive;
    private bool _isCooldown;
    private Coroutine _routine;
    #endregion


    #region 🔹 Skill Logic (UseSkill → StepDance)
    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive || _isCooldown)
        {
            Debug.Log($"[{player.PlayerName}] Skill not ready");
            return;
        }

        Debug.Log($"[{player.PlayerName}] uses StepDance!");
        _routine = player.StartCoroutine(StepDanceRoutine(player));
    }

    private IEnumerator StepDanceRoutine(Player player)
    {
        _isSkillActive = true;

        // FX
        if (_danceEffect != null)
            Object.Instantiate(_danceEffect, player.transform.position, Quaternion.identity);

        // 1) Hide from enemies (no damage taken)
        HideFromEnemies(player, _hideDuration);

        // 2) Stun & damage nearby enemies
        AffectNearbyEnemies(player);

        // 3) Speed boost for full duration
        player.ApplySpeedModifier(_speedBoost, _skillDuration);

        yield return new WaitForSeconds(_skillDuration);

        _isSkillActive = false;
        StartCooldown(player);
    }
    #endregion


    #region 🔹 Cooldown
    private void StartCooldown(Player player)
    {
        player.StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        Debug.Log($"💃 DancerSkill cooldown {_skillCooldown}s");
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log($"💃 DancerSkill READY");
    }
    #endregion


    #region 🔹 Enemy Interaction Logic
    private void HideFromEnemies(Player player, float time)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        player.StartCoroutine(TemporarilyHideRoutine(enemies, time));
    }

    private IEnumerator TemporarilyHideRoutine(Enemy[] enemies, float time)
    {
        foreach (var enemy in enemies)
            enemy.CanDetectOverride = false;

        yield return new WaitForSeconds(time);

        foreach (var enemy in enemies)
            enemy.CanDetectOverride = true;
    }

    private void AffectNearbyEnemies(Player player)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Vector3 pos = player.transform.position;

        foreach (var enemy in enemies)
        {
            float distance = Vector2.Distance(pos, enemy.transform.position);
            if (distance <= _stopRange)
            {
                // Damage
                if (enemy.TryGetComponent<IDamageable>(out var target))
                    player.ApplyDamage(target, _stepDanceDamage);

                // Stun / Stop movement
                if (enemy is IMoveable moveableEnemy)
                    moveableEnemy.Stop();
            }
        }
    }
    #endregion


    #region 🔹 Attack Overrides
    public override void PerformAttack(Player player)
    {
        // Ground Waving Fan — 2 Block AoE
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, 2f);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var t) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(t, 15);
    }

    public override void PerformChargeAttack(Player player)
    {
        // Charged Fan Spin — 4 Block AoE
        float attackRange = 4f;
        int baseDamage = 20;
        int scaledDamage = Mathf.RoundToInt(baseDamage * Mathf.Clamp(player.GetChargePower(), 1f, 2f));

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, attackRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var t) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(t, scaledDamage);
    }

    public override void PerformRangeAttack(Player player, Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(player.transform.position, target.position) <= 3f)
            if (target.TryGetComponent<IDamageable>(out var enemy))
                player.ApplyDamage(enemy, 15);
    }
    #endregion


    #region 🔹 Cleanup (เมื่อ revert → Duckling)
    public override void Cleanup(Player player)
    {
        if (_routine != null)
            player.StopCoroutine(_routine);

        _isSkillActive = false;
        _isCooldown = false;
    }
    #endregion
}
