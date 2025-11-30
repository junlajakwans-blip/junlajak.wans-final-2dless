using UnityEngine;
using System.Collections;

/// <summary>
/// DetectiveDuck – Utility / Reveal career (ID 4, Tier B)
/// RevealHiddenItems → Randomly spawns 3 Buff Items.
/// ChargeAttack: Infrared Scanner → All enemies lose detection for 3 sec.
/// BuffMon: None
/// BuffMap: None
/// </summary>
[CreateAssetMenu(menuName = "DUFFDUCK/Skill/DetectiveSkill_Full")]
public class DetectiveSkill : CareerSkillBase
{
    #region 🔹 Fields (Copied from DetectiveDuck.cs)
    [Header("Detective Settings")]
    [SerializeField] private GameObject _scanEffect;
    [SerializeField] private GameObject _magnifyLightPrefab;
    [SerializeField] private float _magnifyRange = 2f;
    [SerializeField] private float _scanRadius = 5f;
    [SerializeField] private float _skillDuration = 25f;
    [SerializeField] private float _skillCooldown = 20f;
    [SerializeField] private float _noDetectDuration = 3f;

    private bool _isSkillActive;
    private bool _isCooldown;
    private Coroutine _routine;
    #endregion


    #region 🔹 UseSkill → RevealHiddenItems
    public override void UseCareerSkill(Player player)
    {
        if (player == null) return;

        if (_isSkillActive || _isCooldown)
        {
            Debug.Log($"[{player.PlayerName}] Skill not ready");
            return;
        }

        _routine = player.StartCoroutine(SpawnBuffOnlyRoutine(player));
    }

    private IEnumerator SpawnBuffOnlyRoutine(Player player)
    {
        _isSkillActive = true;
        Debug.Log($"[{player.PlayerName}] uses skill: RevealHiddenItems! (Spawn Mode)");

        // ---------------------------------------------------------
        // PlayFX Skill 
        // ---------------------------------------------------------
        if (player.FXProfile != null && player.FXProfile.skillFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.skillFX, player.transform.position);
        }
        // ---------------------------------------------------------

        if (_scanEffect != null)
            Object.Instantiate(_scanEffect, player.transform.position, Quaternion.identity);

        // สร้างบัฟ 3 อันรอบตัว
        for (int i = 0; i < 3; i++)
        {
            Vector2 randomPos = (Vector2)player.transform.position + Random.insideUnitCircle * _scanRadius;
            SpawnBuffItem(player, randomPos);
        }

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
        Debug.Log($"🕵 DetectiveSkill cooldown {_skillCooldown}s");
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log($"🕵 DetectiveSkill READY");
    }
    #endregion


    #region 🔹 ChargeAttack → Infrared Scanner (Disable enemy detection)
    public override void PerformChargeAttack(Player player)
    {
        Debug.Log($"[{player.PlayerName}] activates Infrared Scanner!");
        
        // ---------------------------------------------------------
        // PlayFX Charge ( - ใช้ extraFX)
        // ---------------------------------------------------------
        if (player.FXProfile != null && player.FXProfile.extraFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.extraFX, player.transform.position);
        }
        // ---------------------------------------------------------

        player.StartCoroutine(InfraredScannerRoutine(player));
    }

    private IEnumerator InfraredScannerRoutine(Player player)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.CanDetectOverride = false;

        yield return new WaitForSeconds(_noDetectDuration);

        foreach (var enemy in enemies)
            enemy.CanDetectOverride = true;
    }

    #endregion


    #region 🔹 Attack & RangeAttack
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
        // MagnifyLight (2 Block)
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _magnifyRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(target, 15);

        if (_magnifyLightPrefab != null)
            Object.Instantiate(_magnifyLightPrefab, player.transform.position, Quaternion.identity);

        Debug.Log($"[{player.PlayerName}] uses MagnifyLight!");
    }

    public override void PerformRangeAttack(Player player, Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(player.transform.position, target.position) <= _magnifyRange)
            if (target.TryGetComponent<IDamageable>(out var enemy))
                player.ApplyDamage(enemy, 10);
    }
    #endregion


    #region 🔹 Helper: Spawn Buff Items
    private void SpawnBuffItem(Player player, Vector2 position)
    {
        CollectibleSpawner spawner = Object.FindFirstObjectByType<CollectibleSpawner>();
        if (spawner != null)
        {
            spawner.SpawnAtPosition(position);
            Debug.Log($"[{player.PlayerName}] Spawned Buff Item at {position}");
        }
        else
        {
            Debug.LogWarning($"[{player.PlayerName}] CollectibleSpawner not found!");
        }
    }
    #endregion


    #region 🔹 Cleanup when career reverted
    public override void Cleanup(Player player)
    {
        if (_routine != null)
            player.StopCoroutine(_routine);

        _isSkillActive = false;
        _isCooldown = false;
    }
    #endregion
}