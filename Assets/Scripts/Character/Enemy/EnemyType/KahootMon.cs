using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random; 

public class KahootMon : Enemy
{
    // NOTE: _data field (EnemyData) is inherited from Enemy.cs

    #region Fields
    [Header("KahootMon References")]
    [SerializeField] private List<string> _questionList = new List<string>(); 
    [SerializeField] private string[] _blockColors = new string[4]; 
    [SerializeField] private string _activeQuestion;
    [SerializeField] private Dictionary<Color, string> _statusEffects = new(); 
    
    [Header("Projectile/Block")]
    [SerializeField] private GameObject _blockPrefab;
    [SerializeField] private Transform _firePoint;

    private float _nextAttackTime;

    // ProgrammerDuck Buff Field
    private float _disableChanceFromBuff = 0f;
    #endregion

    #region Unity Lifecycle
    
    protected override void Start()
    {
        base.Start(); 
        _nextAttackTime = Time.time + _data.KahootAttackInterval;
    }

    protected override void Update()
    {
        if (_isDisabled) return;
        
        if (_target == null) return; 

        if (_target != null && Time.time >= _nextAttackTime)
        {
            Attack();
            _nextAttackTime = Time.time + _data.KahootAttackInterval;
        }
    }
    #endregion


#region Buffs
    /// <summary>
    /// Overrides base method to receive Career Buffs.
    /// </summary>
    public override void ApplyCareerBuff(DuckCareerData data)
    {
        if (data == null) return;

        // Check for ProgrammerDuck Buff (25% chance to disable)
        if (data.CareerID == DuckCareer.Programmer)
        {
            // 1. Get the chance value from the Career Data Asset
            _disableChanceFromBuff = data.KahootMonDisableChance; 
            
            Debug.Log($"[KahootMon] Programmer Buff Applied: {_disableChanceFromBuff * 100:F0}% chance to disable on spawn.");
            
            // 2. Apply the chance check immediately upon receiving the buff
            float roll = Random.value; // Random value between 0.0 and 1.0
            
            if (roll < _disableChanceFromBuff) // e.g., if roll < 0.25 (25% chance)
            {
                // Disable KahootMon for a long duration (e.g., 60 seconds)
                const float disableDuration = 60f; 
                DisableBehavior(disableDuration); 
                Debug.Log($"[KahootMon] Programmer Buff SUCCESS: Disabled for {disableDuration}s. (Roll: {roll:F2})");
            }
            else
            {
                Debug.Log($"[KahootMon] Programmer Buff failed. (Roll: {roll:F2})");
            }
        }
    }
    #endregion


    #region Combat
    public override void Attack()
    {
        Debug.Log($"[{name}] starts Quiz Attack!");
        ShowQuestion();
        
        Color randomColor = GetRandomBlockColor();
        FireBlock(randomColor); 
    }

    public void ShowQuestion()
    {
        if (_questionList.Count == 0) return;
        
        int randomIndex = Random.Range(0, _questionList.Count);
        _activeQuestion = _questionList[randomIndex];
        Debug.Log($"KahootMon asks: {_activeQuestion}");
    }

    private Color GetRandomBlockColor()
    {
        // Logic remains the same, assuming _blockColors array is managed in Inspector
        Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
        return colors[Random.Range(0, colors.Length)];
    }


    public void FireBlock(Color color)
    {
        // [FIX 2.1]: เปลี่ยนการตรวจสอบ Prefab เป็นการตรวจสอบ Pool Reference
        if (_blockPrefab == null || _firePoint == null || _target == null || _poolRef == null) return; 
        
        Debug.Log($"KahootMon fires block of color {color}");
        
        // [FIX 2.2]: ใช้ SpawnFromPool แทน Instantiate
        string poolTag = _blockPrefab.name; // ใช้ชื่อ Prefab เป็น Tag
        var go = _poolRef.SpawnFromPool(poolTag, _firePoint.position, Quaternion.identity);

        if (go.TryGetComponent<Rigidbody2D>(out var rb) && _target != null)
        {
            Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
            rb.linearVelocity = aim * _data.KahootBlockSpeed;
        }

        if (go.TryGetComponent<Projectile>(out var proj))
        {
            proj.SetDamage(_data.KahootBlockDamage);
            
        }
    }

    public void ActivateEffect(Player player)
    {

        if (player == null || _buffManagerRef == null) return;

        float roll = Random.value;
        
        if (roll < 0.33f) // 33% chance: Slow Curse
        {
            player.ApplySpeedModifier(_data.KahootSlowModifier, _data.KahootSlowDuration); 
            Debug.Log($"[Curse] {player.name} got SLOWED! Too many wrong answers!");
        }
        else if (roll < 0.66f) // 33% chance: Small Damage
        {
            player.TakeDamage(_data.KahootSmallDamage); 
            Debug.Log($"[Curse] {player.name} got a small ZAP! Lost {_data.KahootSmallDamage} HP!");
        }
        else // 34% chance: Double Speed Mode
        {
            DoubleSpeedMode(); 
            Debug.Log($"[Troll] {name} is putting on PRESSURE! Double Speed Mode!");
        }
    }

    public void DoubleSpeedMode()
    {
        if (Speed < _data.BaseMovementSpeed * 1.9f) 
        {
            Speed *= 2; 
            StartCoroutine(RevertSpeedRoutine(_data.KahootSpeedDuration)); 
            Debug.Log($"KahootMon enters DOUBLE SPEED MODE! Current speed: {Speed}");
        }
    }

    private IEnumerator RevertSpeedRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Speed /= 2;
        Debug.Log($"KahootMon speed returned to normal.");
    }
    #endregion

    #region Death/Drop
    /// <summary>
    /// Implements item drop logic upon defeat. Only drops Coin.
    /// </summary>
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = _spawnerRef;
        
        if (spawner != null && _data != null)
        {
            float roll = Random.value;
            float coinChance = _data.KahootCoinDropChance;

            //  Drop Coin 
            if (roll < coinChance)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[KahootMon] Dropped: Coin (Chance: {coinChance * 100:F0}%)");
            }
        }
        else if (spawner == null)
        {
            Debug.LogWarning("[KahootMon] CollectibleSpawner NOT INJECTED! Cannot drop items.");
        }
    }
    #endregion
}