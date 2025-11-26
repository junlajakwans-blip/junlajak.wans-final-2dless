using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/ProgrammerSkill_Full")]
public class ProgrammerSkill : CareerSkillBase
{
    #region Summary
    // ProgrammerDuck – Utility / Debuff career (ID 8, Tier A)
    // Skill (BlueScreen): Stun enemies in 4 Block radius (3s)
    // BuffMon: LotteryMon -> +10 Coin, KahootMon -> 25% chance Disable
    // BuffMap: School -> Wall behind pushes slowly
    #endregion

    #region Fields
    [Header("Skill Settings")]
    [SerializeField] private GameObject _codeEffect;
    [SerializeField] private float _stunRadius = 4f;
    [SerializeField] private float _stunDuration = 3f;

    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 27f;
    [SerializeField] private float _skillCooldown = 23f;

    [Header("Attack Settings")]
    [SerializeField] private GameObject _bugBombEffect;
    [SerializeField] private float _bugBombRange = 4f;
    [SerializeField] private float _chargeRange = 6f;

    private bool _isSkillActive;
    private bool _isCooldown;
    private Coroutine _skillRoutine;
    private EnemySpawner _enemySpawner;
    #endregion

    #region Initialize Career Buffs (Map + BuffMon)
    public override void Initialize(Player player)
    {
        // Subscribe for cleanup
        if (player.TryGetComponent<CareerSwitcher>(out var switcher))
            switcher.OnRevertToDefaultEvent += () => Cleanup(player);

        // BuffMap
        if (player.CurrentMapType == MapType.School)
            ApplySchoolMapBuff(player);

        // BuffMon (LotteryMon, KahootMon)
        _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemySpawned += (Enemy newEnemy) => HandleNewEnemyBuff(newEnemy, player);
            ApplyBuffsToExistingEnemies(player);
        }
    }
    #endregion

    #region BuffMap Logic
    private void ApplySchoolMapBuff(Player player)
    {
        var mapGen = Object.FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
            mapGen.SetWallPushSpeed(0.3f);
    }

    private void RevertSchoolMapBuff(Player player)
    {
        var mapGen = Object.FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
            mapGen.SetWallPushSpeed(1.0f);
    }
    #endregion

    #region BuffMon Logic
    private void ApplyBuffsToExistingEnemies(Player player)
    {
        var career = player.CurrentCareerData;
        if (career == null) return;

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var mon in all)
            if (mon.EnemyType == EnemyType.LotteryMon || mon.EnemyType == EnemyType.KahootMon)
                mon.ApplyCareerBuff(career);
    }

    private void HandleNewEnemyBuff(Enemy newEnemy, Player player)
    {
        var career = player.CurrentCareerData;
        if (career == null) return;

        if (newEnemy.EnemyType == EnemyType.LotteryMon || newEnemy.EnemyType == EnemyType.KahootMon)
            newEnemy.ApplyCareerBuff(career);
    }
    #endregion

    #region Cleanup
    public override void Cleanup(Player player)
    {
        _isSkillActive = false;
        _isCooldown = false;

        if (_skillRoutine != null)
            player.StopCoroutine(_skillRoutine);

        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned -= (Enemy e) => HandleNewEnemyBuff(e, player);

        if (player.CurrentMapType == MapType.School)
            RevertSchoolMapBuff(player);
    }
    #endregion

    #region UseSkill (BlueScreen)
    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive || _isCooldown) return;
        _skillRoutine = player.StartCoroutine(BlueScreenRoutine(player));
    }

    private IEnumerator BlueScreenRoutine(Player player)
    {
        _isSkillActive = true;

        // ----------------
        // [SkillFX]
        // ----------------
        if (player.FXProfile != null && player.FXProfile.skillFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.skillFX, player.transform.position);
        }
        // ----------------

        if (_codeEffect != null)
            Object.Instantiate(_codeEffect, player.transform.position, Quaternion.identity);

        ApplyBlueScreenStun(player);

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;

        player.StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
    }
    #endregion

    #region Skill Effects
    private void ApplyBlueScreenStun(Player player)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _stunRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
                enemy.DisableBehavior(_stunDuration);
        }
    }
    #endregion

    #region Attack Implementation
    public override void PerformAttack(Player player)
    {
        // ----------------
        // [AttackFX]
        // ----------------
        if (player.FXProfile != null && player.FXProfile.basicAttackFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.basicAttackFX, player.transform.position);
        }
        // ----------------
        
        if (_bugBombEffect != null)
            Object.Instantiate(_bugBombEffect, player.transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _bugBombRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                target.TakeDamage(15);
    }

    public override void PerformChargeAttack(Player player)
    {
        // ----------------
        // [ChargeFX (ExtraFX)]
        // ----------------
        if (player.FXProfile != null && player.FXProfile.extraFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.extraFX, player.transform.position);
        }
        // ----------------

        if (_bugBombEffect != null)
            Object.Instantiate(_bugBombEffect, player.transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _chargeRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var target) && hit.GetComponent<Player>() == null)
                target.TakeDamage(25);
    }
    #endregion
}