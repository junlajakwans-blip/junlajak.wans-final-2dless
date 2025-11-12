using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class KahootMon : Enemy
{
    #region Fields
    [Header("KahootMon Settings")]
    [SerializeField] private List<string> _questionList = new List<string>();
    [SerializeField] private string[] _blockColors = new string[4];
    [SerializeField] private float _attackInterval = 3f;
    [SerializeField] private string _activeQuestion;
    
    // Status effect map (Color -> Effect description or key)
    // Example: Red block -> Slow, Blue block -> Damage
    [SerializeField] private Dictionary<Color, string> _statusEffects = new(); 
    
    [Header("Projectile/Block")]
    [SerializeField] private GameObject _blockPrefab; // Prefab of the fired block (must have Projectile.cs)
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _blockSpeed = 6f;
    [SerializeField] private int _blockDamage = 5;

    private float _nextAttackTime;
    #endregion

    #region Unity Lifecycle
    public void Start()
    {
        _nextAttackTime = Time.time + _attackInterval;
        // Optional: Initialize _statusEffects here if not done in Inspector
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
            _nextAttackTime = Time.time + _attackInterval;
        }
    }
    #endregion

    #region Combat
    public override void Attack()
    {
        Debug.Log($"[{name}] starts Quiz Attack!");
        ShowQuestion();
        
        // Randomly choose a color block to fire
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
        // Simple random color generation for demonstration (assuming blocks are red, blue, green, yellow)
        Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
        return colors[Random.Range(0, colors.Length)];
    }

    public void FireBlock(Color color)
    {
        if (_blockPrefab == null || _firePoint == null || _target == null) return;
        
        Debug.Log($"KahootMon fires block of color {color}");
        
        // Instantiate Block (Needs pooling later)
        var go = Instantiate(_blockPrefab, _firePoint.position, Quaternion.identity);

        if (go.TryGetComponent<Rigidbody2D>(out var rb) && _target != null)
        {
            // Aim at the player
            Vector2 aim = ((Vector2)_target.position - (Vector2)_firePoint.position).normalized;
            rb.linearVelocity = aim * _blockSpeed;
        }

        if (go.TryGetComponent<Projectile>(out var proj))
        {
            // Set base damage and the color/effect key
            proj.SetDamage(_blockDamage);
            // Assuming Projectile has a SetColor or SetEffectKey method for complex logic
            // proj.SetColor(color); 
        }
    }

    /// <summary>
    /// Applies a random joke/curse effect to the player, simulating a quiz penalty.
    /// This method is designed to "gently troll" the player.
    /// </summary>
    public void ActivateEffect(Player player)
    {
        if (player == null || BuffManager.Instance == null) return;

        float roll = Random.value;
        
        if (roll < 0.33f) // 33% chance: Slow Curse
        {
            player.ApplySpeedModifier(0.4f, 2.5f); // 40% speed for 2.5 seconds
            Debug.Log($"[Curse] {player.name} got SLOWED! Too many wrong answers!");
        }
        else if (roll < 0.66f) // 33% chance: Temporary Blindness (Reduced Detection)
        {
            // Hypothetical: If player has a detection range/FOV property
            // player.ApplyDetectionModifier(0.2f, 3f); 
            player.TakeDamage(1); // Small, annoying damage
            Debug.Log($"[Curse] {player.name} got a small ZAP! Lost 1 HP!");
        }
        else // 34% chance: Double Speed Mode (Self-inflicted Troll/Pressure)
        {
            DoubleSpeedMode(); 
            Debug.Log($"[Troll] {name} is putting on PRESSURE! Double Speed Mode!");
        }
    }

    public void DoubleSpeedMode()
    {
        // Check if speed has already been doubled to prevent stacking
        if (_speed < base._speed * 1.9f) // Assuming base._speed is the original speed
        {
            _speed *= 2;
            // Use a Coroutine to revert speed after a short duration
            StartCoroutine(RevertSpeedRoutine(3f)); 
            Debug.Log($"KahootMon enters DOUBLE SPEED MODE! Current speed: {_speed}");
        }
    }

    private IEnumerator RevertSpeedRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        _speed /= 2;
        Debug.Log($"KahootMon speed returned to normal.");
    }
    #endregion

    #region Death/Drop
    /// <summary>
    /// Implements item drop logic upon defeat.
    /// </summary>
    public override void Die()
    {
        base.Die();
        
        CollectibleSpawner spawner = FindFirstObjectByType<CollectibleSpawner>();
        
        if (spawner != null)
        {
            float roll = Random.value;
            
            // Drop Coin with 40% chance
            if (roll < 0.40f)
            {
                spawner.DropCollectible(CollectibleType.Coin, transform.position);
                Debug.Log($"[KahootMon] Dropped: Coin ({roll:F2})");
            }
            // Drop CardPickup with 10% chance
            else if (roll < 0.50f) 
            {
                spawner.DropCollectible(CollectibleType.CardPickup, transform.position);
                Debug.Log($"[KahootMon] Dropped: CardPickup ({roll:F2})");
            }
        }
        else
        {
            Debug.LogWarning("[KahootMon] CollectibleSpawner not found! Cannot drop items.");
        }
    }
    #endregion
}