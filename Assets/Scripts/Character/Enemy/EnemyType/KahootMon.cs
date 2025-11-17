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
        
        if (_target == null)
        {
            var player = FindFirstObjectByType<Player>();
            if (player != null) _target = player.transform;
        }

        if (_target != null && Time.time >= _nextAttackTime)
        {
            Attack();
            _nextAttackTime = Time.time + _data.KahootAttackInterval;
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

    // ... (ShowQuestion และ GetRandomBlockColor methods ถูกละไว้) ...

    public void FireBlock(Color color)
    {
        if (_blockPrefab == null || _firePoint == null || _target == null) return;
        
        Debug.Log($"KahootMon fires block of color {color}");
        
        var go = Instantiate(_blockPrefab, _firePoint.position, Quaternion.identity);

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

        if (player == null || BuffManager.Instance == null) return;

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
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
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
            Debug.LogWarning("[KahootMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}