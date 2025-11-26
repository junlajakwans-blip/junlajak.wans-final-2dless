using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/ChefSkill_Full")]
public class ChefSkill : CareerSkillBase
{
    #region 🔹 Fields (เหมือน ChefDuck เดิม)
    [Header("ChefDuck Settings (Copied from ChefDuck.cs)")]
    [SerializeField] private GameObject _panEffect;
    [SerializeField] private float _burnRange = 5f;
    [SerializeField] private int _burnDamage = 25;
    [SerializeField] private float _speedMultiplier = 1.5f;
    [SerializeField] private float _buffTime = 5f;
    [SerializeField] private float _skillDuration = 26f;
    [SerializeField] private float _skillCooldown = 20f;
    [SerializeField] private int _minCoinBonus = 3;
    [SerializeField] private int _maxCoinBonus = 8;

    private bool _isSkillActive;
    private bool _isCooldown;
    private Coroutine _routine;
    #endregion


    #region 🔹 Skill Logic (UseSkill → Ducklicious → Roast Duckeddon)
    public override void UseCareerSkill(Player player)
    {
        if (player == null) return;

        if (_isSkillActive || _isCooldown)
        {
            Debug.Log($"[{player.PlayerName}] Skill not ready");
            return;
        }

        _routine = player.StartCoroutine(DuckliciousRoutine(player));
    }

    // 🟡 = ChefDuck.DuckliciousRoutine() เดิม
    private IEnumerator DuckliciousRoutine(Player player)
    {
        _isSkillActive = true;
        Debug.Log($"[{player.PlayerName}] Ducklicious activated! ({_skillDuration}s)");

        ApplyBurnDamage(player);
        player.StartCoroutine(CookBuffRoutine(player));

        yield return new WaitForSeconds(_skillDuration);

        _isSkillActive = false;
        StartCooldown(player);
    }
    #endregion


    #region 🔹 Cooldown (เหมือน ChefDuck.cs)
    private void StartCooldown(Player player)
    {
        player.StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        Debug.Log($"🔥 ChefSkill cooldown {_skillCooldown}s");
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log($"🔥 ChefSkill READY");
    }
    #endregion


    #region Attack Logic (Copied from ChefDuck Attack override)
    public override void PerformAttack(Player player)
    {
        //PlayFX
        if (player.FXProfile != null && player.FXProfile.basicAttackFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.basicAttackFX, player.transform.position);
        }
        //Attack

        // Flying Pan 1.5f
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, 1.5f);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var t) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(t, 15);
    }

    public override void PerformChargeAttack(Player player)
    {
        float power = player.GetChargePower();    
        float range = Mathf.Lerp(2f, 4f, power);
        int baseDamage = 20;
        int scaledDamage = Mathf.RoundToInt(baseDamage * Mathf.Clamp(power, 1f, 2f));

        //PlayFX Charge
        if (player.FXProfile != null && player.FXProfile.extraFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.extraFX, player.transform.position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, range);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var t) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(t, scaledDamage);
    }

    public override void PerformRangeAttack(Player player, Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(player.transform.position, target.position) <= 2f)
            if (target.TryGetComponent<IDamageable>(out var enemy))
                player.ApplyDamage(enemy, 15);
    }

    #endregion


    #region 🔹 Burn Damage 5 Blocks (เหมือนเดิม 100%)
    private void ApplyBurnDamage(Player player)
    {
        if (_panEffect != null)
            Object.Instantiate(_panEffect, player.transform.position, Quaternion.identity);
        
                //PlayFX
        if (player.FXProfile != null && player.FXProfile.skillFX != null)
        {
            ComicEffectManager.Instance.Play(player.FXProfile.skillFX, player.transform.position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _burnRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var t) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(t, _burnDamage);
    }
    #endregion


    #region 🔹 BuffMon & BuffMap Logic (ย้ายตรงจาก InitializeCareerBuffs)
    // เรียกตอนใช้สกิล ไม่ได้หายไป
    private IEnumerator CookBuffRoutine(Player player)
    {
        Debug.Log($"[{player.PlayerName}] CookBuff applied ({_buffTime}s)");

        player.ApplySpeedModifier(_speedMultiplier, _buffTime);

        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            switch (enemy.EnemyType)
            {
                case EnemyType.DoggoMon:
                case EnemyType.MooPingMon:
                case EnemyType.PeterMon:
                    enemy.DisableBehavior(_buffTime);
                    break;

                case EnemyType.LotteryMon:
                    int bonus = Random.Range(_minCoinBonus, _maxCoinBonus + 1);
                    player.AddCoin(bonus);
                    break;
            }
        }

        yield return new WaitForSeconds(_buffTime);
    }
    #endregion


    #region 🔹 Cleanup (เมื่อ revert → Duckling)
    public override void Cleanup(Player player)
    {
        if (player == null) return;

        if (_routine != null)
            player.StopCoroutine(_routine);

        _isSkillActive = false;
        _isCooldown = false;
    }

    #endregion
}
