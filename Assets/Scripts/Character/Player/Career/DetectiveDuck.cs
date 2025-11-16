using UnityEngine;
using System.Collections;

/// <summary>
/// DetectiveDuck – Utility / Reveal career (ID 4, Tier B)
/// RevealHiddenItems → Randomly spawns 3 Buff Items.
/// ChargeAttack: Infrared Scanner → All enemies lose detection for 3 sec.
/// BuffMon: None
/// BuffMap: None
/// </summary>
public class DetectiveDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("DetectiveDuck Settings")]
    [SerializeField] private GameObject _scanEffect;
    [SerializeField] private GameObject _magnifyLightPrefab;
    [SerializeField] private float _magnifyRange = 2f;      // 2 Block
    [SerializeField] private float _scanRadius = 5f;
    [SerializeField] private float _skillDuration = 25f;   // 25 Sec
    [SerializeField] private float _skillCooldown = 20f;   // 20 Sec
    [SerializeField] private float _noDetectDuration = 3f; // 3 Sec

    private bool _isSkillActive;
    private bool _isCooldown;
    #endregion

    #region Buffs (Map & Monster)

    /// <summary>
    /// (Override) Applies DetectiveDuck-specific buffs when the career is initialized.
    /// This method is called by the base Player.Initialize() method.
    /// </summary>
    protected override void InitializeCareerBuffs()
    {
        // PDF (Page 4) confirms Detective has no BuffMap or BuffMon.
        // This override is intentionally left blank.
        Debug.Log($"[DetectiveDuck] No BuffMon or BuffMap to initialize.");
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(RevealHiddenItemsRoutine());
    }

    /// <summary>
    /// Reveal HiddenItems -> Random Spawn Buff Item 3 piece
    /// </summary>
    private IEnumerator RevealHiddenItemsRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: RevealHiddenItems!");

        if (_scanEffect != null)
            Instantiate(_scanEffect, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _scanRadius);
        int buffSpawned = 0;

        // 1. Reveal existing hidden items (assuming tag "HiddenItem")
        foreach (var hit in hits)
        {
            if (hit.CompareTag("HiddenItem") && buffSpawned < 3)
            {
                hit.gameObject.SetActive(true);
                buffSpawned++;
                Debug.Log($"[{PlayerName}] Revealed hidden buff item at {hit.transform.position}");
            }
        }

        // 2. If no hidden items were found (or not enough), spawn new ones
        // ("Random Spawn Buff Item 3 piece" - implies spawning if not found)
        for (int i = buffSpawned; i < 3; i++)
        {
            Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * _scanRadius;
            // Call the helper to spawn an item via the spawner
            SpawnBuffItem(randomPos); 
        }
        

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
        Debug.Log("[DetectiveDuck] Cooldown finished!");
    }
    #endregion
    
    #region CareerAbility: Infrared Scanner (ChargeAttack)
    /// <summary>
    /// Infrared Scanner -> All Mon No Detect Range 3 Sec
    /// </summary>
    public override void ChargeAttack(float power)
    {
        Debug.Log($"[{PlayerName}] activates Infrared Scanner!");
        StartCoroutine(InfraredScannerRoutine());
    }

    private IEnumerator InfraredScannerRoutine()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.CanDetectOverride = false;
        }

        Debug.Log($"[{PlayerName}] All enemies detection disabled for {_noDetectDuration}s");
        yield return new WaitForSeconds(_noDetectDuration);

        // Re-enable detection
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.CanDetectOverride = true;
        }
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // [CareerAttack] MagnifyLight (2 Block)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _magnifyRange); // _magnifyRange = 2f
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                ApplyDamage(target, 15);
        }

        if (_magnifyLightPrefab != null)
            Instantiate(_magnifyLightPrefab, transform.position, Quaternion.identity);

        Debug.Log($"[{PlayerName}] uses MagnifyLight!");
    }

    public override void RangeAttack(Transform target)
    {
        // MagnifyLight 2 Block [LightRay]
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.position) <= _magnifyRange) // Use _magnifyRange
        {
            if (target.TryGetComponent<IDamageable>(out var enemy))
                ApplyDamage(enemy, 10);
        }
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion

    #region Helper
    /// <summary>
    /// Spawns a buff item by calling the CollectibleSpawner.
    /// </summary>
    private void SpawnBuffItem(Vector2 position)
    {
        // FIX: Call the actual Spawner (using the quick find method)
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        if (spawner != null)
        {
            // Assuming SpawnAtPosition spawns a random item (Coin, Buff, etc.)
            // To guarantee ONLY buff items, CollectibleSpawner would need a specific method.
            spawner.SpawnAtPosition(position);
            Debug.Log($"[{PlayerName}] Spawned Buff Item at {position}");
        }
        else
        {
            Debug.LogWarning($"[{PlayerName}] CollectibleSpawner not found! Cannot spawn item.");
        }
    }
    #endregion
}