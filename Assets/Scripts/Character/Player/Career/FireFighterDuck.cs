using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "DUFFDUCK/Skill/FireFighterSkill_Full")]
public class FireFighterSkill : CareerSkillBase
{
    #region ▬ Fields (Copied from FireFighterDuck.cs)
    [Header("FireFighter Settings")]
    [SerializeField] private GameObject _waterBallPrefab;          
    [SerializeField] private GameObject _splashEffect;             
    [SerializeField] private GameObject _waterSplashSkillEffect;   

    [SerializeField] private float _waterBallRange = 2f;           
    [SerializeField] private float _splashAttackRange = 4f;        
    [SerializeField] private float _skillPushRange = 5f;           
    [SerializeField] private float _skillPushForce = 10f;          

    [SerializeField] private float _skillDuration = 28f;           
    [SerializeField] private float _skillCooldown = 22f;           

    private bool _isSkillActive;
    private bool _isCooldown;

    private EnemySpawner _enemySpawner;
    private Coroutine _skillRoutine;
    #endregion


    #region ▬ Initialize Career Buffs (BuffMap + BuffMon)
    public override void Initialize(Player player)
    {
        // Subscribe cleanup when career ends
        if (player.TryGetComponent<CareerSwitcher>(out var switcher))
            switcher.OnRevertToDefaultEvent += () => Cleanup(player);

        // Map Buff
        if (player.CurrentMapType == MapType.RoadTraffic)  
            ApplyRoadTrafficBuff(player);
        // BuffMon
        _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (_enemySpawner != null)
        {
            // ต้อง Forward parameter player เข้า HandleNewEnemyBuff
            _enemySpawner.OnEnemySpawned += enemy => HandleNewEnemyBuff(enemy, player);

            ApplyBuffsToExistingEnemies(player);
        }
    }
    #endregion


    #region ▬ BuffMap: Road Traffic
    private void ApplyRoadTrafficBuff(Player player)
    {
        RedlightMon[] lights = Object.FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            Debug.Log($"[FireFighterSkill] Forced Redlight green ({light.name})");
        }

        var mapGen = Object.FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
            Debug.Log("[FireFighterSkill] Platform break disabled on map");
    }

    private void RevertRoadTrafficBuff()
    {
        RedlightMon[] lights = Object.FindObjectsByType<RedlightMon>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            Debug.Log($"[FireFighterSkill] Reverting Redlight ({light.name})");
        }

        var mapGen = Object.FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
            Debug.Log("[FireFighterSkill] Platform break re-enabled on map");
    }
    #endregion


    #region ▬ BuffMon: MooPingMon
    private void ApplyBuffsToExistingEnemies(Player player)
    {
        DuckCareerData career = player.CurrentCareerData;
        if (career == null) return;

        Enemy[] mons = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var mon in mons)
            if (mon.EnemyType == EnemyType.MooPingMon)
                mon.ApplyCareerBuff(career);
    }

    private void HandleNewEnemyBuff(Enemy newEnemy, Player player)
    {
        DuckCareerData career = player.CurrentCareerData;
        if (career == null) return;

        if (newEnemy.EnemyType == EnemyType.MooPingMon)
            newEnemy.ApplyCareerBuff(career);
    }
    #endregion

    #region ▬ UseSkill → WaterSplash
    public override void UseCareerSkill(Player player)
    {
        if (_isSkillActive || _isCooldown) return;
        _skillRoutine = player.StartCoroutine(WaterSplashRoutine(player));
    }

    private IEnumerator WaterSplashRoutine(Player player)
    {
        _isSkillActive = true;

        if (_waterSplashSkillEffect != null)
            Object.Instantiate(_waterSplashSkillEffect, player.transform.position, Quaternion.identity);

        ApplySkillEffects(player);

        yield return new WaitForSeconds(_skillDuration);
        _isSkillActive = false;
        StartCooldown(player);
    }

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


    #region ▬ WaterSplash Effects (Break Floor + Push/Stun Enemies)
    private void ApplySkillEffects(Player player)
    {
        var mapGen = Object.FindFirstObjectByType<MapGeneratorBase>();
        if (mapGen != null)
            Debug.Log("[FireFighterSkill] Breaking rightmost floor (TODO)");

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _skillPushRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Enemy enemy))
            {
                if (enemy.transform.position.x > player.transform.position.x)
                {
                    enemy.DisableBehavior(2f);

                    if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
                        rb.AddForce(Vector2.right * _skillPushForce, ForceMode2D.Impulse);
                }
            }
        }
    }
    #endregion


    #region ▬ Attack / Charge / Range
    public override void PerformAttack(Player player)
    {
        if (_waterBallPrefab != null)
            Object.Instantiate(_waterBallPrefab, player.transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _waterBallRange);
        foreach (var hit in hits)
        {
            var go = Object.Instantiate(_waterBallPrefab, player.transform.position + (player.transform.right * 0.5f), Quaternion.identity);
            if (go.TryGetComponent<Projectile>(out var proj))
                proj.SetDamage(15);
        }
    }

    public override void PerformChargeAttack(Player player)
    {
        if (_splashEffect != null)
            Object.Instantiate(_splashEffect, player.transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, _splashAttackRange);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var t) && hit.GetComponent<Player>() == null)
                player.ApplyDamage(t, 20);
    }

    public override void PerformRangeAttack(Player player, Transform target)
    {
        // No dedicated range attack (covered by Attack + Charge)
    }
    #endregion


    #region ▬ Cleanup when career reverted
    public override void Cleanup(Player player)
    {
        _isSkillActive = false;
        _isCooldown = false;

        if (_skillRoutine != null)
            player.StopCoroutine(_skillRoutine);

        // คืน BuffMap
        if (player.CurrentMapType == MapType.RoadTraffic)
            RevertRoadTrafficBuff();

        // คืน BuffMon
        if (_enemySpawner != null)
            _enemySpawner.OnEnemySpawned -= enemy => HandleNewEnemyBuff(enemy, player);
    }

    #endregion
}
