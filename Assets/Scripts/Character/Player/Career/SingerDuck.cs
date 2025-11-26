using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/SingerSkill_Full")]
public class SingerSkill : CareerSkillBase
{
    #region Summary
    // Singer – Crowd Control / High-Tier Utility career (ID 9, Tier S)
    // Skill (Sonic Quack): Stun all enemies on screen + 50% chance to spawn GoldenMon
    // BuffMon: None
    // BuffMap: +2% GoldenMon spawn chance after enemy death (stacking)
    #endregion

    #region Fields
    [Header("FX")]
    [SerializeField] private GameObject _micFx;
    [SerializeField] private GameObject _highNoteFx;

    [Header("Skill Settings")]
    [SerializeField] private float _stunDuration = 3f;
    [SerializeField] private float _goldenMonSpawnChance = 0.5f;

    [Header("Career Timing")]
    [SerializeField] private float _skillDuration = 32f;
    [SerializeField] private float _skillCooldown = 28f;

    [Header("Attack Settings")]
    [SerializeField] private float _highNoteRange = 4f;
    [SerializeField] private float _divaMinRange = 4f;
    [SerializeField] private float _divaMaxRange = 6f;

    private bool _isSkillActive;
    private bool _isCooldown;
    private bool _mapBuffActive;   // +2% GoldenMon Chance
    private Coroutine _routine;
    #endregion

    #region Initialize (BuffMap)
    public override void Initialize(Player player)
    {
        // Singer ไม่มี BuffMon
        // เปิด BuffMap ให้ระบบ GameManager / EnemySpawner ตรวจสอบตอนศัตรูตาย
        _mapBuffActive = true;

        if (player.TryGetComponent<CareerSwitcher>(out var switcher))
            switcher.OnRevertToDefaultEvent += () => Cleanup(player);
    }

    public bool IsMapBuffActive => _mapBuffActive;
    #endregion

    #region Cleanup
    public override void Cleanup(Player player)
    {
        _isSkillActive = false;
        _isCooldown = false;
        _mapBuffActive = false;

        if (_routine != null)
            player.StopCoroutine(_routine);
    }
    #endregion

    #region UseSkill (Sonic Quack)
    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive || _isCooldown) return;
        _routine = player.StartCoroutine(SonicQuackRoutine(player));
    }

    private IEnumerator SonicQuackRoutine(Player player)
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

        if (_micFx != null)
            Object.Instantiate(_micFx, player.transform.position, Quaternion.identity);

        StunAllEnemies(player);
        TrySpawnGoldenMon(player);

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
    private void StunAllEnemies(Player player)
    {
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            e.DisableBehavior(_stunDuration);   // ใช้ stun ดีกว่า confuse
    }

    private void TrySpawnGoldenMon(Player player)
    {
        if (Random.value >= _goldenMonSpawnChance) return;

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            Vector3 pos = player.transform.position + Vector3.right * 2f;
            spawner.SpawnSpecificEnemy(EnemyType.GoldenMon, pos);
        }
    }
    #endregion

    #region Attack (High Note / Diva)
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

        if (_highNoteFx != null)
            Object.Instantiate(_highNoteFx, player.transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _highNoteRange);
        foreach (var h in hits)
            if (h.TryGetComponent<IDamageable>(out var t) && h.GetComponent<Player>() == null)
                t.TakeDamage(15);
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

        float power = player.GetChargePower();
        float range = Mathf.Lerp(_divaMinRange, _divaMaxRange, power);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, range);
        foreach (var h in hits)
            if (h.TryGetComponent<IDamageable>(out var t) && h.GetComponent<Player>() == null)
                t.TakeDamage(25);
    }
    #endregion
}