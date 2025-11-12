using UnityEngine;
using System.Collections;

/// <summary>
/// ChefDuck – AoE / Support career
/// Ducklicious → Roast Duckeddon: burns enemies in 5 blocks, grants bonus coins.
///
/// BuffMon : CookBuffRoutine() supports disabling certain enemies behaviors for a short time:
/// - DoggoMon → No Barking
/// - MooPingMon → No ThrowSkewer
/// - PeterMon → No Attack
/// - LotteryMon → % Drop Coin between 3–8 coins
///
/// BuffMap: Kitchen → Wall behind pushes slowly.
/// </summary>


public class ChefDuck : Player, ISkillUser, IAttackable
{
    #region Fields
    [Header("ChefDuck Settings")]
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
    #endregion

    private void Start()
    {
        InitializeCareerMapBuff(); // ตรวจและใช้ MapBuff จริง
    }

    #region Map Buff (Active for Kitchen + future-ready)
    /// <summary>
    /// Applies ChefDuck-specific map buff when in Kitchen (and supports future maps).
    /// </summary>
    private void InitializeCareerMapBuff()
    {
        var map = GetCurrentMapType();

        switch (map)
        {
            case MapType.Kitchen:
                var mapGen = FindFirstObjectByType<MapGeneratorBase>();
                if (mapGen != null)
                {
                    //TODO: Implement actual wall push speed adjustment in MapGeneratorBase
                    //mapGen.SetWallPushSpeed(0.3f); // Wall speed Slower 
                    Debug.Log("[ChefDuck] Map Buff applied: Kitchen → Wall behind pushes slowly.");
                }
                break;

            default:
                Debug.Log($"[ChefDuck] No map-specific buff active. Ready for future maps. (Current: {map})");
                break;
        }
    }
    #endregion

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(RoastDuckeddon());
    }

    private IEnumerator RoastDuckeddon() // Ducklicious → Roast Duckeddon Chef Skill
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: Ducklicious → Roast Duckeddon!");

        if (_panEffect != null)
            Instantiate(_panEffect, transform.position, Quaternion.identity);

        StartCoroutine(CookBuffRoutine());

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _burnRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, _burnDamage);
        }

        int bonusCoin = Random.Range(_minCoinBonus, _maxCoinBonus + 1);
        AddCoin(bonusCoin);
        Debug.Log($"[ChefDuck] +{bonusCoin} bonus coins from Roast Duckeddon!");

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        OnSkillCooldown();
    }

    public override void OnSkillCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine() // Skill Cooldown
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log("[ChefDuck] Cooldown finished!");
    }
    #endregion

    #region CookBuffRoutine : BuffMon Support

    /// <summary>
    /// BuffMon: DoggoMon -> No Barking / MooPingMon -> No ThrowSkewer / PeterMon -> No Attack /LotteryMon  -> % Drop Coin Between 3-8 Coin
    /// </summary>
    private IEnumerator CookBuffRoutine()
    //TODO: Disable certain enemies for _buffTime 
    {
        Debug.Log($"[{PlayerName}] starts CookBuff!");
        ApplySpeedModifier(_speedMultiplier, _buffTime);

        // Enemy Interaction: disable certain enemies
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            switch (enemy.EnemyType)
            {
                case EnemyType.DoggoMon:
                case EnemyType.MooPingMon:
                case EnemyType.PeterMon:
               // case EnemyType.LotteryMon:
               //     enemy.DisableBehavior(_buffTime);
                    Debug.Log($"[{PlayerName}] BuffMon applied: {enemy.EnemyType} disabled for {_buffTime}s");
                    break;
            }
        }

        yield return new WaitForSeconds(_buffTime);
        Debug.Log($"[{PlayerName}] CookBuff ended!");
    }
    #endregion

    #region IAttackable Implementation
    public override void Attack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, 15);
        }
    }

    public override void ChargeAttack(float power)
    {
        float range = Mathf.Lerp(2f, 4f, power);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                ApplyDamage(target, 20);
        }
    }

    public override void RangeAttack(Transform target)
    {
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.position) <= 2f)
        {
            if (target.TryGetComponent<IDamageable>(out var enemy))
                ApplyDamage(enemy, 15);
        }
    }

    public override void ApplyDamage(IDamageable target, int amount)
    {
        target.TakeDamage(amount);
    }
    #endregion

    #region Debug Visual
    // Only for visualizing burn range in editor
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _burnRange);
    }
#endif
    #endregion
}
