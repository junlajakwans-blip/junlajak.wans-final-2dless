using UnityEngine;
using System.Collections;

/// <summary>
/// ChefDuck – Career Buff / AoE playstyle
/// (ISkillUser, IAttackable, IDamageable)
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

    #region ISkillUser Implementation
    public override void UseSkill()
    {
        if (_isSkillActive || _isCooldown) return;
        StartCoroutine(RoastDuckeddon());
    }

    private IEnumerator RoastDuckeddon()
    {
        _isSkillActive = true;
        Debug.Log($"{PlayerName} uses skill: Ducklicious → Roast Duckeddon!");

        // Show pan effect
        if (_panEffect != null)
            Instantiate(_panEffect, transform.position, Quaternion.identity);

        // Start CookBuff concurrently (increase Speed, BuffMap, BuffMon)
        StartCoroutine(CookBuffRoutine());

        // Deal AoE damage
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _burnRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                ApplyDamage(target, _burnDamage);
            }
        }

        // Random bonus Coin (like LotteryMon)
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

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        yield return new WaitForSeconds(_skillCooldown);
        _isCooldown = false;
        Debug.Log("[ChefDuck] Cooldown finished!");
    }
    #endregion


    #region  CookBuffRoutine : Secondary buff (increase Speed and BuffMap/Mon)
    private IEnumerator CookBuffRoutine()
    {
        Debug.Log($"[{PlayerName}] starts CookBuff!");
        ApplySpeedModifier(_speedMultiplier, _buffTime);

        // Example of enabling BuffMap / BuffMon
        // (In a real system, might send event to Manager to handle)
        EnableMapBuff("Kitchen", _buffTime);
        DisableEnemyBehavior("DoggoMon", _buffTime);
        DisableEnemyBehavior("MooPingMon", _buffTime);
        DisableEnemyBehavior("PeterMon", _buffTime);
        DisableEnemyBehavior("LotteryMon", _buffTime);

        yield return new WaitForSeconds(_buffTime);
        Debug.Log($"[{PlayerName}] CookBuff ended!");
    }

    // --------------------------------------------------------
    // ฟังก์ชันช่วยจำลอง BuffMap / BuffMon
    // --------------------------------------------------------
    private void EnableMapBuff(string mapName, float time)
    {
        Debug.Log($"[MapBuff] {mapName} - Wall behind push slowly for {time}s");
    }

    private void DisableEnemyBehavior(string enemyName, float time)
    {
        Debug.Log($"[BuffMon] {enemyName} disabled for {time}s");
    }
    #endregion


    #region IAttackable Implementation
    public override void Attack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                ApplyDamage(target, 15);
            }
        }
    }

    public override void ChargeAttack(float power)
    {
        float range = Mathf.Lerp(2f, 4f, power);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                ApplyDamage(target, 20);
            }
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _burnRange);
    }
#endregion
}
