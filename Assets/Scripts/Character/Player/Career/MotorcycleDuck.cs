using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/MotorcycleSkill_Full")]
public class MotorcycleSkill : CareerSkillBase
{
    #region Fields 
    [SerializeField] private GameObject _dashEffect;
    [SerializeField] private float _dashMultiplier = 3f;
    [SerializeField] private float _dashDuration = 0.5f;
    [SerializeField] private float _immunityChance = 0.15f;
    //TODO : Upgrade inFuture [SerializeField] private float _jumpBonus = 1.2f;

    [SerializeField] private float _skillDuration = 24f;
    [SerializeField] private float _skillCooldown = 18f;

    [SerializeField] private float _handlebarRange = 3f;
    [SerializeField] private float _slideRange = 5f;
    [SerializeField] private float _slideKnockback = 8f;

    private bool _isSkillActive;
    public bool IsSkillActive => _isSkillActive;
    private bool _isCooldown;
    private bool _hasJumpBuff;
    public bool HasJumpBuff => _hasJumpBuff;

    private Coroutine _routine;
    private EnemySpawner _enemySpawner;
    private int _redlightCount = 0;
    #endregion

    #region Initialize
    public override void Initialize(Player player)
    {
        // 🔹 Map Buff
        if (player.CurrentMapType == MapType.RoadTraffic)
            ApplyTrafficBuff(true);

        // 🔹 BuffMon
        _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemySpawned += (enemy) => HandleNewEnemy(enemy, player);
            ApplyBuffsToExistingEnemies(player);
        }
    }
    #endregion

    #region Use Skill
    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive || _isCooldown) return;
        _routine = player.StartCoroutine(SkillRoutine(player));
    }

    private IEnumerator SkillRoutine(Player player)
    {
        _isSkillActive = true;

        if (player.FXProfile != null && player.FXProfile.skillFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.skillFX, player.transform.position);
        }

        player.StartCoroutine(DashRoutine(player));
        yield return new WaitForSeconds(_skillDuration);

        _isSkillActive = false;
        StartCooldown(player);
    }

    private IEnumerator DashRoutine(Player player)
    {
        if (_dashEffect != null)
            Object.Instantiate(_dashEffect, player.transform.position, Quaternion.identity);

        // เปิด Invulnerability ก่อนพุ่ง
        player.SetInvulnerable(true); 

        player.ApplySpeedModifier(_dashMultiplier, _dashDuration);
        
        yield return new WaitForSeconds(_dashDuration);

        // ปิด Invulnerability เมื่อ Dash สิ้นสุด
        player.SetInvulnerable(false); 
    }
    #endregion

    #region Cooldown
    private void StartCooldown(Player player)
    {
        player.StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
    }
    #endregion

    #region Attack Override
    public override void PerformAttack(Player player)
    {
        if (player.FXProfile != null && player.FXProfile.basicAttackFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.basicAttackFX, player.transform.position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _handlebarRange);
        foreach (var h in hits)
            if (h.TryGetComponent<IDamageable>(out var t) && h.GetComponent<Player>() == null)
                player.ApplyDamage(t, 20);
    }

    public override void PerformChargeAttack(Player player)
    {
        if (player.FXProfile != null && player.FXProfile.extraFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.extraFX, player.transform.position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _slideRange);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<Enemy>(out var enemy))
            {
                player.ApplyDamage(enemy, 25);
                if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
                    rb.AddForce((enemy.transform.position - player.transform.position).normalized * _slideKnockback, ForceMode2D.Impulse);
            }
        }
    }
    #endregion
    
#region OnTakeDamage (Immune 15%)
public override void OnTakeDamage(Player player, int dmg)
{
    // 1) ตรวจสอบโหมด Immortal (เช่นตอนใช้สกิล)
    if (_isSkillActive && Random.value < _immunityChance)
    {
        Debug.Log($"[{player.PlayerName}] IMMUNE 15% (Motorcycle Skill)");
        return; // block damage
    }

    // 2) ถ้าไม่ block → ให้ player โดน damage จริง
    player.ApplyRawDamage(dmg);

    Debug.Log($"[{player.PlayerName}] MotorcycleSkill: Damage {dmg} passed through.");
}
#endregion

    #region BuffMon (RedlightMon)
    private void ApplyBuffsToExistingEnemies(Player player)
    {
        var career = player.CurrentCareerData;
        if (career == null) return;

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in all)
        {
            if (e.EnemyType == EnemyType.RedlightMon)
            {
                e.ApplyCareerBuff(career);
                e.OnEnemyDied += RemoveRedlight;
                _redlightCount++;
            }
        }
        _hasJumpBuff = _redlightCount > 0;
    }

    private void HandleNewEnemy(Enemy enemy, Player player)
    {
        var career = player.CurrentCareerData;
        if (career == null) return;

        if (enemy.EnemyType == EnemyType.RedlightMon)
        {
            enemy.ApplyCareerBuff(career);
            enemy.OnEnemyDied += RemoveRedlight;
            _redlightCount++;
            _hasJumpBuff = true;
        }
    }

    private void RemoveRedlight(Enemy e)
    {
        _redlightCount--;
        if (_redlightCount <= 0) _hasJumpBuff = false;
        e.OnEnemyDied -= RemoveRedlight;
    }
    #endregion

    #region BuffMap (RoadTraffic)
    private void ApplyTrafficBuff(bool enable)
    {
        RedlightMon[] lights = Object.FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var l in lights) l.ForceSignalState(enable ? "Red" : "Green", enable);
    }
    #endregion

    #region Cleanup
    public override void Cleanup(Player player)
    {
        if (_routine != null) player.StopCoroutine(_routine);
        _isSkillActive = false;
        _isCooldown = false;

        if (player.CurrentMapType == MapType.RoadTraffic)
            ApplyTrafficBuff(false);

        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned -= (enemy) => HandleNewEnemy(enemy, player);
    }
    #endregion
}