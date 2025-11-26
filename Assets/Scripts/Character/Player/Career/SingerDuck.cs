using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/SingerSkill_Full")]
public class SingerSkill : CareerSkillBase
{
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
    private bool _mapBuffActive;
    private Coroutine _routine;
    private CareerSwitcher _switcherRef;

    public override void Initialize(Player player)
    {
        _mapBuffActive = true;

        if (player.TryGetComponent(out _switcherRef))
        {
            _switcherRef.OnRevertToDefaultEvent -= HandleRevert; // ป้องกันสมัครซ้ำ
            _switcherRef.OnRevertToDefaultEvent += HandleRevert;
        }
    }

    private void HandleRevert()
    {
        // ถูกเรียกโดย CardManager / Switcher เมื่อกลับ Duckling
        _mapBuffActive = false;
        _isSkillActive = false;
        _isCooldown = false;
    }

    public override void Cleanup(Player player)
    {
        _mapBuffActive = false;
        _isSkillActive = false;
        _isCooldown = false;

        if (_routine != null)
            player.StopCoroutine(_routine);

        // ถอน event ที่สมัครไว้
        if (_switcherRef != null)
            _switcherRef.OnRevertToDefaultEvent -= HandleRevert;
    }

    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive || _isCooldown) return;
        _routine = player.StartCoroutine(SonicQuackRoutine(player));
    }

    private IEnumerator SonicQuackRoutine(Player player)
    {
        _isSkillActive = true;

        if (player.FXProfile?.skillFX != null)
            ComicEffectManager.Instance.Play(player.FXProfile.skillFX, player.transform.position);

        if (_micFx != null)
            Object.Instantiate(_micFx, player.transform.position, Quaternion.identity);

        StunAllEnemies();
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

    private void StunAllEnemies()
    {
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            e.DisableBehavior(_stunDuration);
    }

    private void TrySpawnGoldenMon(Player player)
    {
        if (Random.value >= _goldenMonSpawnChance) return;

        var sp = Object.FindFirstObjectByType<EnemySpawner>();
        if (sp != null)
        {
            Vector3 pos = player.transform.position + Vector3.right * 2f;
            sp.SpawnSpecificEnemy(EnemyType.GoldenMon, pos);
        }
    }

    public override void PerformAttack(Player player)
    {
        if (player.FXProfile?.basicAttackFX != null)
            ComicEffectManager.Instance.Play(player.FXProfile.basicAttackFX, player.transform.position);

        if (_highNoteFx != null)
            Object.Instantiate(_highNoteFx, player.transform.position, Quaternion.identity);

        foreach (var h in Physics2D.OverlapCircleAll(player.transform.position, _highNoteRange))
            if (h.TryGetComponent<IDamageable>(out var t) && !h.TryGetComponent<Player>(out _))
                t.TakeDamage(15);
    }

    public override void PerformChargeAttack(Player player)
    {
        if (player.FXProfile?.extraFX != null)
            ComicEffectManager.Instance.Play(player.FXProfile.extraFX, player.transform.position);

        float power = player.GetChargePower();
        float range = Mathf.Lerp(_divaMinRange, _divaMaxRange, power);

        foreach (var h in Physics2D.OverlapCircleAll(player.transform.position, range))
            if (h.TryGetComponent<IDamageable>(out var t) && !h.TryGetComponent<Player>(out _))
                t.TakeDamage(25);
    }
}
