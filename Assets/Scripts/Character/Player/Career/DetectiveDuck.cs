using UnityEngine;
using System.Collections;

/// <summary>
/// DetectiveDuck – Utility / Reveal career
/// RevealHiddenItems → Randomly spawns 3 Buff Items within scan radius.
/// 
/// Attack:
/// - JumpAttack (Default)
/// - [CareerAttack] MagnifyLight (short-range cone attack)
/// 
/// ChargeAttack:
/// - Infrared Scanner → All enemies lose detection for 3 sec (no alert / chase)
/// 
/// BuffMon: None
/// BuffMap: None
/// </summary>
public class DetectiveDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("DetectiveDuck Settings")]
    [SerializeField] private GameObject _scanEffect;
    [SerializeField] private GameObject _magnifyLightPrefab;
    [SerializeField] private float _magnifyRange = 2f;
    [SerializeField] private float _scanRadius = 5f;
    [SerializeField] private float _skillDuration = 25f;
    [SerializeField] private float _skillCooldown = 20f;
    [SerializeField] private float _noDetectDuration = 3f;

    private bool _isSkillActive;
    private bool _isCooldown;
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(RevealHiddenItemsRoutine());
    }

    private IEnumerator RevealHiddenItemsRoutine()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: RevealHiddenItems!");

        // สร้างเอฟเฟกต์สแกนในฉาก
        if (_scanEffect != null)
            Instantiate(_scanEffect, transform.position, Quaternion.identity);

        // Detect env in range _scanRadius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _scanRadius);
        int buffSpawned = 0;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("HiddenItem") && buffSpawned < 3)
            {
                // Make item visible (or respawn)
                hit.gameObject.SetActive(true);
                buffSpawned++;
                Debug.Log($"[{PlayerName}] Revealed hidden buff item at {hit.transform.position}");
            }
        }

        if (buffSpawned == 0)
        {
            // No hidden items → random spawn 3 new ones
            for (int i = 0; i < 3; i++)
            {
                Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * _scanRadius;
                SpawnBuffItem(randomPos);
            }
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
    /// Infrared Scanner → Temporarily disables enemy detection for _noDetectDuration seconds.
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
            enemy.CanDetectOverride = false;
        }

        Debug.Log($"[{PlayerName}] All enemies detection disabled for {_noDetectDuration}s");
        yield return new WaitForSeconds(_noDetectDuration);

        foreach (var enemy in enemies)
        {
            enemy.CanDetectOverride = true;
        }
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        // MagnifyLight short cone attack
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _magnifyRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, 15);
        }

        if (_magnifyLightPrefab != null)
            Instantiate(_magnifyLightPrefab, transform.position, Quaternion.identity);

        Debug.Log($"[{PlayerName}] uses MagnifyLight!");
    }

    public override void RangeAttack(Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.position) <= _magnifyRange + 1f)
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
    private void SpawnBuffItem(Vector2 position)
    {
        // เรียกผ่านระบบ CollectibleSpawner จริงในอนาคต
        Debug.Log($"[{PlayerName}] Spawned Buff Item at {position}");
    }
    #endregion
}
