using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class GoldenMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    [Header("GoldenMon Attack Settings")]
    [SerializeField] private float _attackInterval = 0.85f; // Small delay between swings
    private float _nextAttackTime;

    [Header("Dance Movement")]
    [SerializeField] private float _swayAmplitude = 0.35f;
    [SerializeField] private float _swaySpeed = 3f;

    [Header("Dance Wave Attack")]
    [SerializeField] private float _waveRadius = 2.25f;
    [SerializeField] private int _waveHits = 3;
    [SerializeField] private float _waveHitInterval = 0.22f;
    private bool _isAttackingWave;

    [Header("Platform Break")]
    [SerializeField] private float _breakRadius = 6f;
    [Header("Visual")]
    [SerializeField] private Vector3 _baseScale = new Vector3(0.25f, 0.25f, 0.25f);

    protected override void Start()
    {
        base.Start();
        _nextAttackTime = Time.time + _attackInterval; // stagger first hit
        // Ensure consistent scene scale
        transform.localScale = new Vector3(_baseScale.x, _baseScale.y, _baseScale.z);
    }

    #region Movement/Attack
    public override void Move()
    {
        if (!CanAct() || _target == null) return;

        // Face the player by flipping local scale
        float dir = Mathf.Sign(_target.position.x - transform.position.x);
        float facing = dir == 0 ? Mathf.Sign(transform.localScale.x) : Mathf.Sign(dir);
        transform.localScale = new Vector3(_baseScale.x * facing, _baseScale.y, _baseScale.z);

        // Dance forward with a small sway (wave) on Y
        float sway = Mathf.Sin(Time.time * _swaySpeed) * _swayAmplitude;
        Vector2 moveDir = new Vector2(dir, sway).normalized;
        transform.Translate(moveDir * Speed * Time.deltaTime);
    }

    public void DanceAttack()
    {
        if (_isAttackingWave || !CanAct() || _playerRef == null) return;
        StartCoroutine(DanceWaveRoutine());
    }

    public void BreakPlatform()
    {
        int toBreak = Mathf.Max(1, _data.GoldenMonBreakPlatformCount);
        var hits = Physics2D.OverlapCircleAll(transform.position, _breakRadius);

        // Find closest normal platforms
        System.Array.Sort(hits, (a, b) =>
            Vector2.SqrMagnitude(a.transform.position - transform.position)
                .CompareTo(Vector2.SqrMagnitude(b.transform.position - transform.position)));

        int broken = 0;
        foreach (var hit in hits)
        {
            if (broken >= toBreak) break;
            if (hit == null || !hit.CompareTag("Platform")) continue;

            GameObject platform = hit.gameObject;

            // Make sure it has break behaviour and correct tag
            var breakable = platform.GetComponent<BreakPlatform>();
            if (breakable == null)
            {
                breakable = platform.AddComponent<BreakPlatform>();
                // Ensure a rigidbody exists for the BreakPlatform routine
                var rb = platform.GetComponent<Rigidbody2D>() ?? platform.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }

            platform.tag = "BreakPlatform";
            breakable.StartBreak();
            broken++;
        }

        Debug.Log($"[GoldenMon] Forced break on {broken}/{toBreak} platforms (radius {_breakRadius}).");
    }

    public override void Attack()
    {
        if (!CanAct() || _playerRef == null) return;

        if (Time.time < _nextAttackTime) return;
        _nextAttackTime = Time.time + _attackInterval;

        float distance = Vector2.Distance(transform.position, _playerRef.transform.position);
        if (distance > _detectionRange) return;

        // Use wave attack so hits are not spammy but feel like pulses
        DanceAttack();
    }

    private IEnumerator DanceWaveRoutine()
    {
        _isAttackingWave = true;

        for (int i = 0; i < _waveHits; i++)
        {
            if (!CanAct() || _playerRef == null) break;

            float dist = Vector2.Distance(transform.position, _playerRef.transform.position);
            if (dist <= _waveRadius)
            {
                ApplyDamage(_playerRef, _attackPower);
                Debug.Log($"[GoldenMon] Dance wave hit {i + 1}/{_waveHits} for {_attackPower} damage.");
            }

            yield return new WaitForSeconds(_waveHitInterval);
        }

        _isAttackingWave = false;
    }

    /// <summary>
    /// Drops a guaranteed amount of golden coins based on EnemyData multipliers.
    /// </summary>
    public void DropGoldenCoins()
    {
        CollectibleSpawner spawner = _spawnerRef;
        if (spawner == null)
        {
            Debug.LogWarning("[GoldenMon] CollectibleSpawner NOT INJECTED for coin drop!");
            return;
        }

        //  Use Data From EnemyData:Unique | Asset: Min/Max Coin ก่อนคูณ และตัวคูณ
        int baseCoins = Random.Range(_data.GoldenMonBaseMinCoin, _data.GoldenMonBaseMaxCoin + 1);
        int coins = baseCoins * _data.GoldenMonCoinDropMultiplier;
        
        Debug.Log($"{name} drops {coins} GOLD coins! (Base: {baseCoins}, Multiplier: {_data.GoldenMonCoinDropMultiplier})");

        // Spawn individual coins (scattered position)
        for (int i = 0; i < coins; i++)
        {
            spawner.DropCollectible(CollectibleType.Coin, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0));
        }
    }
    #endregion

#region Death/Drop
    /// <summary>
    /// Guaranteed drop of Career Card + massive Coin drop + potential Token bonus.
    /// </summary>
    public override void Die()
    {

        Player player = _playerRef;
        Vector3 pos = transform.position;

        // 1) Guaranteed drop: Career Card
        RequestDrop(CollectibleType.CardPickup);
        Debug.Log("[GoldenMon] Dropped Career Card (Guaranteed).");

        // 2) Token drop only if current player career is MuscleDuck
        if (player != null && player.GetCurrentCareerID() == DuckCareer.Muscle)
        {
            RequestDrop(CollectibleType.Token);
            Debug.Log("[GoldenMon] Dropped Token (MuscleDuck Bonus).");
        }

        // 3) Massive Coin Drop (assumes DropGoldenCoins internally uses RequestDrop)
        DropGoldenCoins();
        Debug.Log("[GoldenMon] Triggered massive Coin burst.");

        // 4) Notify spawner (this must be last)
        base.Die();
    }

    #endregion
}
