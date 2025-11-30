using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random; 

public class LotteryMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs
    [Header("LotteryMon Throwable")]
    [SerializeField] private GameObject _goodLuckThrowable; // ไอเทมดีที่ปา
    [SerializeField] private GameObject _badLuckThrowable;  // ไอเทมร้ายที่ปา

    [SerializeField] private float _throwForce = 6f; // ความแรงตอนปา


    #region Fields
    [Header("LotteryMon State")]
    private int _chefCoinBonusMin = 0;
    private int _chefCoinBonusMax = 0;

    private float _nextAttackTime;
    #endregion

    #region Unity Lifecycle
    
    protected override void Start()
    {
        
        base.Start(); 
        
        // 2. Initialize custom timers using loaded data
        // _data.LotteryAttackCooldown from EnemyData
        _nextAttackTime = Time.time + _data.LotteryAttackCooldown;
    }

protected override void Update()
    {
        if (_isDisabled) return;

        if (_target != null && Time.time >= _nextAttackTime)
        {
            Attack();
            // Use Data From EnemyData:Unique | Asset: _data.LotteryAttackCooldown
            _nextAttackTime = Time.time + _data.LotteryAttackCooldown;
        }
        
        // ถ้า _target เป็น null (เช่น Player ตาย) ให้หยุดทำงาน
        if (_target == null) return; 
    }
    #endregion

    #region Combat
    public override void Attack()
    {
        if (_target == null || _target.TryGetComponent<Player>(out var player) == false) return;

        float roll = Random.value;
        
        // Use Data From EnemyData:Unique | Asset: _data.LotteryLuckFactor
        if (roll < _data.LotteryLuckFactor) // Chance for good luck
        {
            ApplyGoodLuck(player);
            Debug.Log($"[{name}] rolled: {roll:F2}. Player wins!");
        }
        else // Chance for bad luck
        {
            ApplyBadLuck(player);
            Debug.Log($"[{name}] rolled: {roll:F2}. Player loses!");
        }
    }

    private void ThrowItem(GameObject prefab, Player player)
    {
        if (prefab == null) return;

        // สร้าง object
        GameObject item = Instantiate(prefab, transform.position, Quaternion.identity);

        // ปาเข้าหาผู้เล่น
        if (item.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            rb.AddForce(dir * _throwForce, ForceMode2D.Impulse);
        }
    }


    public void ApplyGoodLuck(Player player)
    {
        // Use Data From EnemyData:Unique | Asset: LotteryGoodLuckMinCoin และ LotteryGoodLuckMaxCoin
        int coinAmount = Random.Range(_data.LotteryGoodLuckMinCoin, _data.LotteryGoodLuckMaxCoin + 1);
        player.AddCoin(coinAmount);

         ThrowItem(_goodLuckThrowable, player);
        Debug.Log($"[Lottery] {player.name} got lucky: +{coinAmount} Coin! (Roll chance: {_data.LotteryLuckFactor * 100:F0}%)");
    }

public void ApplyBadLuck(Player player)
    {
        if (player == null) return;
        ThrowItem(_badLuckThrowable, player); 
            // Use Data From EnemyData:Unique | Asset: _data.LotteryCurseDuration
        player.ApplySpeedModifier(0.5f, _data.LotteryCurseDuration);
        
        Debug.Log($"[Lottery] {player.name} got cursed: Speed reduced for {_data.LotteryCurseDuration}s!");
    }
    #endregion


    #region Buffs
        /// <summary>
        /// Overrides base method to receive ChefDuck's Coin Bonus Buff.
        /// </summary>
        public override void ApplyCareerBuff(DuckCareerData data)
        {
            if (data != null)
            {

                _chefCoinBonusMin = data.ChefMonCoinMinBonusValue; 
                _chefCoinBonusMax = data.ChefMonCoinMaxBonusValue;
                
                Debug.Log($"[LotteryMon] Chef Buff Applied: +{_chefCoinBonusMin}-{_chefCoinBonusMax} Bonus Coins.");
            }
        }
    #endregion


    #region Death/Drop
    /// <summary>
    /// Drops a guaranteed amount of Coin between min and max upon defeat.
    /// </summary>
    public override void Die()
    {
        // Guard: already dead
        if (_isDead) return;
        _isDead = true;

        Vector3 pos = transform.position;

        if (_data != null)
        {
            // 1) Random coin amount
            int coinAmount = Random.Range(_data.LotteryMinCoinDrop, _data.LotteryMaxCoinDrop + 1);

            for (int i = 0; i < coinAmount; i++)
            {
                // Maple-style scatter: half-circle upward spread
                float angle = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.6f, 1.6f);
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

                RequestDrop(CollectibleType.Coin, pos + offset);
            }

            Debug.Log($"[LotteryMon] Dropped {coinAmount} coins (Maple Scatter).");

            // 2) ChefDuck bonus
            if (_chefCoinBonusMax > 0)
            {
                int bonusAmount = Random.Range(_chefCoinBonusMin, _chefCoinBonusMax + 1);

                for (int i = 0; i < bonusAmount; i++)
                {
                    float angle = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
                    float distance = Random.Range(0.6f, 1.6f);
                    Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

                    RequestDrop(CollectibleType.Coin, pos + offset);
                }

                Debug.Log($"[LotteryMon] Chef Bonus Drop: +{bonusAmount} coins (Maple Scatter).");
            }
        }
        else
        {
            Debug.LogWarning("[LotteryMon] EnemyData missing. Drop skipped.");
        }

        base.Die();
    }

    #endregion
}