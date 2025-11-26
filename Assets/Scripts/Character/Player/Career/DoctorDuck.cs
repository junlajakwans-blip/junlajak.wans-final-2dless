using UnityEngine;
using System.Collections;

/// <summary>
/// DoctorDuck – Support / Self-Healer Career (ID 5, Tier S)
/// Passive self-heal, revive once, MedicBag skill, and BuffMon (PeterMon 30% No Attack).
/// BuffMap: Regen ×2 (handled naturally via heal routine)
/// </summary>
[CreateAssetMenu(menuName = "DUFFDUCK/Skill/DoctorSkill_Full")]
public class DoctorSkill : CareerSkillBase
{
    #region 🔹 Fields (Copied from DoctorDuck.cs)
    [Header("Doctor Settings")]
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

    private Coroutine _healRoutine;
    private Coroutine _skillRoutine;
    #endregion


    #region 🔹 Initialize (Passive Heal + Map Buff)
    public override void Initialize(Player player)
    {
        // Map buff info only logs (x2 regen effect occurs through passive)
        Debug.Log("[DoctorSkill] Map Buff applied → Regen ×2");

        // Start passive self-heal loop
        _healRoutine = player.StartCoroutine(SelfHealRoutine(player));
    }

    private IEnumerator SelfHealRoutine(Player player)
    {
        while (true)
        {
            if (!player.IsDead && player.CurrentHealth < player.MaxHealth)
            {
                player.Heal(_healAmount);
                Debug.Log($"[{player.PlayerName}] self-heal +{_healAmount}");

                if (_healEffect != null)
                    Object.Instantiate(_healEffect, player.transform.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(_healInterval);
        }
    }
    #endregion


    #region 🔹 Revive System
    public override void OnTakeDamage(Player player, int incomingDamage)
    {
        // Called by Player → forward damage result & detect death
        if (player.IsDead && _canRevive)
            player.StartCoroutine(ReviveRoutine(player));
    }

    private IEnumerator ReviveRoutine(Player player)
    {
        _canRevive = false;

        player.Revive(Mathf.RoundToInt(player.MaxHealth * _reviveHealthPercent));
        Debug.Log($"[{player.PlayerName}] Revived for {_reviveDuration}s!");

        yield return new WaitForSeconds(_reviveDuration);
        Debug.Log($"[{player.PlayerName}] revive window expired.");
    }

    public override bool OnBeforeDie(Player player)
    {
        if (!_canRevive) return false;
        _canRevive = false;

        int reviveHP = Mathf.RoundToInt(player.MaxHealth * 0.5f);
        player.Revive(reviveHP);

        Debug.Log($"[{player.PlayerName}] Revived with {reviveHP} HP from Doctor Skill!");
        return true; // แจ้ง Player ว่าไม่ต้องตายต่อ
    }
    #endregion


    #region 🔹 UseSkill → MedicBag
    public override void UseCareerSkill(Player player)
    {
        if (player == null) return;

        if (_isSkillActive || _isCooldown)
        {
            Debug.Log($"[{player.PlayerName}] Skill not ready");
            return;
        }

        _skillRoutine = player.StartCoroutine(MedicBagRoutine(player));
    }

    private IEnumerator MedicBagRoutine(Player player)
    {
        _isSkillActive = true;

        Debug.Log($"[{player.PlayerName}] Skill: I'm Doctor → Throw Medic Bag!");

        // ---------------------------------------------------------
        // PlayFX Skill 
        // ---------------------------------------------------------
        if (player.FXProfile != null && player.FXProfile.skillFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.skillFX, player.transform.position);
        }
        // ---------------------------------------------------------

        if (_medicBagPrefab != null)
            Object.Instantiate(_medicBagPrefab, player.transform.position + Vector3.up, Quaternion.identity);

        // Self-heal burst
        player.Heal(_healAmount * 2);

        // Damage AOE
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _healRadius);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var enemy) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(enemy, 10);

        // BuffMon effect (PeterMon 30% No Attack)
        player.StartCoroutine(ApplyBuffMonRoutine());

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
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log("🩺 DoctorSkill READY");
    }
    #endregion


    #region 🔹 BuffMon (PeterMon 30% No Attack)
    private IEnumerator ApplyBuffMonRoutine()
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.EnemyType == EnemyType.PeterMon)
            {
                if (Random.value < 0.30f)
                {
                    enemy.DisableBehavior(_buffMonDuration);
                    Debug.Log($"🩺 BuffMon Applied → PeterMon no attack for {_buffMonDuration}s");
                }
            }
        }
        yield return new WaitForSeconds(_buffMonDuration);
    }
    #endregion


    #region 🔹 Attack / ChargeAttack / RangeAttack
    public override void PerformAttack(Player player)
    {
        // ---------------------------------------------------------
        // PlayFX Attack 
        // ---------------------------------------------------------
        if (player.FXProfile != null && player.FXProfile.basicAttackFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.basicAttackFX, player.transform.position);
        }
        // ---------------------------------------------------------
        
        //Attack
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, 2f);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(target, 10);

        if (_medicBagPrefab != null)
            Object.Instantiate(_medicBagPrefab, player.transform.position + Vector3.up, Quaternion.identity);
    }

    public override void PerformChargeAttack(Player player)
    {
        float power = player.GetChargePower();
        float range = Mathf.Lerp(2f, 3f, power);

        // ---------------------------------------------------------
        // PlayFX Charge (เพิ่มตรงนี้ - ใช้ extraFX)
        // ---------------------------------------------------------
        if (player.FXProfile != null && player.FXProfile.extraFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.extraFX, player.transform.position);
        }
        // ---------------------------------------------------------

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, range);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(target, 15);
    }

    public override void PerformRangeAttack(Player player, Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(player.transform.position, target.position) <= 3f)
            if (target.TryGetComponent<IDamageable>(out var enemy))
                player.ApplyDamage(enemy, 12);
    }
    #endregion


    #region 🔹 Cleanup when revert → Duckling
    public override void Cleanup(Player player)
    {
        if (_healRoutine != null)
            player.StopCoroutine(_healRoutine);

        if (_skillRoutine != null)
            player.StopCoroutine(_skillRoutine);

        _isSkillActive = false;
        _isCooldown = false;
        _canRevive = true;

        _healRoutine = null;
        _skillRoutine = null;
    }

    #endregion
}