using UnityEngine;
using System.Collections.Generic; 

/// <summary>
/// Data container for defining the base statistics and behavior properties of an enemy type.
/// This asset allows designers to adjust stats without modifying code.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_New", menuName = "Duck/Enemy Data")]
public class EnemyData : ScriptableObject
{
    #region Identity & Stats
    
    [Header("Identity & Type")]
    [SerializeField] private EnemyType _typeID = EnemyType.None; // Primary identifier for this enemy
    [SerializeField] private string _displayName = "New Enemy";

    [Header("Base Combat Stats")]
    [SerializeField] private int _baseHealth = 100;
    [SerializeField] private int _baseAttackPower = 10;
    [SerializeField] private float _baseMovementSpeed = 1.5f;
    [SerializeField] private float _baseDetectionRange = 5f;

    [Header("Reward & Visuals")]
    [SerializeField] private int _coinDropAmount = 1; // Coins dropped on death
    [SerializeField] private Sprite _enemySprite;      // Visual representation (if needed for UI/Logic)
    
    // NOTE: สามารถเพิ่ม List ของ Weaknesses/Resistances ที่นี่ได้
    
    #endregion

    #region Properties (Read-Only Accessors)
    
    /// <summary>
    /// The unique identifier for this enemy type (used for logic filtering).
    /// </summary>
    public EnemyType TypeID => _typeID;
    
    /// <summary>
    /// The friendly name of the enemy displayed in logs or UI.
    /// </summary>
    public string DisplayName => _displayName;
    
    /// <summary>
    /// The base amount of health this enemy starts with.
    /// </summary>
    public int BaseHealth => _baseHealth;
    
    /// <summary>
    /// The enemy's base damage output.
    /// </summary>
    public int BaseAttackPower => _baseAttackPower;
    
    /// <summary>
    /// The standard speed of the enemy before any buffs/debuffs are applied.
    /// </summary>
    public float BaseMovementSpeed => _baseMovementSpeed;
    
    /// <summary>
    /// The range within which the enemy can detect the player target.
    /// </summary>
    public float BaseDetectionRange => _baseDetectionRange;
    
    /// <summary>
    /// The number of coins the enemy drops upon defeat.
    /// </summary>
    public int CoinDropAmount => _coinDropAmount;

    /// <summary>
    /// The sprite used to represent the enemy.
    /// </summary>
    public Sprite EnemySprite => _enemySprite;

    #endregion
    
#region Unique Stats (Specific to Derived Classes)

    // ------------------------------------------------------------------------------------------------
    // MamaMon (Boiling Attack & Healing)
    // ------------------------------------------------------------------------------------------------

    [Header("MamaMon Settings (MamaMon.cs)")]
      public int MamaNoodleCount = 3;

    public float MamaAttackCooldown = 2f;

    public float MamaBoilRange = 4f;

    public int MamaBoilDamage = 10;

    public float MamaProjectileSpeed = 5f;

    public int MamaProjectileDamage = 15;

    public float MamaHealChance = 0.1f;

    public float MamaHealCooldown = 8f;


    [Header("MamaMon Drops")] 

    [Tooltip("Probability (0.0 to 1.0) of dropping Coin.")]
    public float MamaCoinDropChance = 0.35f; 

    [Tooltip("Probability (0.0 to 1.0) of dropping GreenTea (must be cumulative with Coin chance).")]
    public float MamaGreenTeaDropChance = 0.10f;


    // ------------------------------------------------------------------------------------------------
    // KahootMon (Block Throw & Speed Boost)
    // ------------------------------------------------------------------------------------------------


    [Header("KahootMon Settings (KahootMon.cs)")]
  
    public float KahootAttackInterval = 3f;
  
    public float KahootBlockSpeed = 6f;
  
    public int KahootBlockDamage = 5;


    [Header("KahootMon Behavior")]
  
    [Tooltip("Duration for DoubleSpeedMode (seconds).")]
    public float KahootSpeedDuration = 3f; 
  
    [Tooltip("Speed multiplier for the Slow Curse (e.g., 0.4 means 40% speed).")]
    public float KahootSlowModifier = 0.4f; 
  
    [Tooltip("Duration for the Slow Curse (seconds).")]
    public float KahootSlowDuration = 2.5f; 
  
    [Tooltip("Small damage dealt during one of the curse options.")]
    public int KahootSmallDamage = 1; 


    [Header("KahootMon Drops")]
  
    [Tooltip("Probability (0.0 to 1.0) of dropping Coin (40%).")]
    public float KahootCoinDropChance = 0.40f;


    // ------------------------------------------------------------------------------------------------
    // LotteryMon (Luck/Curse & Coin Drop Range)
    // ------------------------------------------------------------------------------------------------

    [Header("LotteryMon Settings (LottryMon.cs)")]

    public float LotteryLuckFactor = 0.15f;

    public float LotteryCurseDuration = 4f;

    public float LotteryAttackCooldown = 5f;

 
    [Header("LotteryMon Drop & Rewards")]

    public int LotteryMinCoinDrop = 1;      

    public int LotteryMaxCoinDrop = 40; 

    public int LotteryGoodLuckMinCoin = 1;     

    public int LotteryGoodLuckMaxCoin = 10;



    // ------------------------------------------------------------------------------------------------
    // RedlightMon (Traffic/Cooldown)
    // ------------------------------------------------------------------------------------------------
    
    [Header("RedlightMon Settings (RedlightMon.cs)")]

    [Tooltip("Cooldown (seconds) between car spawns.")]
    public float RedlightCarCooldown = 3.0f;

    [Tooltip("Interval (seconds) before the traffic light state switches.")]
    public float RedlightSwitchInterval = 5.0f; 

    [Tooltip("Number of cars spawned per attack.")]
    public int RedlightSpawnCarCount = 2; 

    [Tooltip("Maximum number of car sprite/prefab variants (e.g., Car_1 to Car_N).")]
    public int RedlightMaxCarTypes = 3;

  
    [Header("RedlightMon Drops")]

    [Tooltip("Probability (0.0 to 1.0) of dropping Coin (30%).")]
    public float RedlightCoinDropChance = 0.30f;

    [Tooltip("Probability (0.0 to 1.0) of dropping Coffee (5%).")] 
    public float RedlightCoffeeDropChance = 0.05f;

    // ------------------------------------------------------------------------------------------------
    // GoldenMon (Special Drop Logic)
    // ------------------------------------------------------------------------------------------------

    [Header("GoldenMon Settings (GoldenMon.cs)")]

    [Tooltip("Number of platforms the GoldenMon destroys on its special attack.")]
    public int GoldenMonBreakPlatformCount = 2;

    [Tooltip("Multiplier for the base coin drop amount (for massive coin rewards).")]
    public int GoldenMonCoinDropMultiplier = 5;

    [Tooltip("Min coins dropped before multiplier.")]
    public int GoldenMonBaseMinCoin = 10;

    [Tooltip("Max coins dropped before multiplier.")]
    public int GoldenMonBaseMaxCoin = 20;
    

    [Header("GoldenMon Card Drop")]

    [Tooltip("Type of card guaranteed to drop (always Career for GoldenMon).")]
    public CardType GoldenGuaranteedCardType = CardType.Career; 

    [Tooltip("Probability (0.0 to 1.0) of dropping a Card Pickup (should be 1.0 for Guaranteed).")]
    public float GoldenCardDropChance = 1.0f; 


    // ------------------------------------------------------------------------------------------------
    // GhostWorkMon (Teleport)
    // ------------------------------------------------------------------------------------------------

    [Header("GhostWorkMon Settings (GhostWorkMon.cs)")]

    public float GhostWorkFadeDuration = 1.5f;

    public float GhostWorkHauntRange = 6f;

    public float GhostWorkTeleportCooldown = 5f;

    public float GhostWorkBaseTeleportDistance = 2f; 

    public int GhostWorkHauntDamage = 5; 


    [Header("GhostWorkMon Drops")]

    public float GhostWorkCoinDropChance = 0.45f;

    public float GhostWorkGreenTeaDropChance = 0.15f;

    // ------------------------------------------------------------------------------------------------
    // MooPingMon (Pattern Movement & Projectile)
    // ------------------------------------------------------------------------------------------------

    [Header("MooPingMon Settings (MooPingMon.cs)")]

    public int MooPingFireDamage = 20;

    public float MooPingSmokeRadius = 2.5f;

    public float MooPingThrowCooldown = 2.2f;

    public float MooPingPatternSpeed = 2.0f;

    public float MooPingPatternWidth = 1.5f;


    [Tooltip("The initial speed of the skewer projectile.")]
    public float MooPingProjectileSpeed = 5f;

 
    [Header("MooPingMon Drops")] 

    [Tooltip("Probability (0.0 to 1.0) of dropping Coin (20%).")]
    public float MooPingCoinDropChance = 0.20f; 

    [Tooltip("Probability (0.0 to 1.0) of dropping Coffee (5%).")]
    public float MooPingCoffeeDropChance = 0.05f; 


    // ------------------------------------------------------------------------------------------------
    // PeterMon (Hover & Range Attack)
    // ------------------------------------------------------------------------------------------------


    [Header("PeterMon Settings (PeterMon.cs)")]
  
    public float PeterHoverAmplitude = 0.25f;
  
    public float PeterHoverSpeed = 2f;
  
    public float PeterAttackRange = 4.5f;
  
    public float PeterAttackCooldown = 2.5f;
  
    public int PeterProjectileDamage = 10;
  
    public float PeterProjectileSpeed = 4f;


    [Header("PeterMon Drops")] 
  
    [Tooltip("Probability (0.0 to 1.0) of dropping Coin (25%).")]
    public float PeterCoinDropChance = 0.25f; 
  
    [Tooltip("Probability (0.0 to 1.0) of dropping GreenTea (5%).")]
    public float PeterGreenTeaDropChance = 0.05f; 

    // ------------------------------------------------------------------------------------------------
    // DoggoMon (Chase & Bark)
    // ------------------------------------------------------------------------------------------------

    [Header("DoggoMon Settings (DoggoMon.cs)")]

    public float DoggoChaseSpeed = 2.5f; 

    public float DoggoBarkRange = 1.25f; 

    public int DoggoDamage = 15;

    public int DoggoHauntRange = 5;

    [Header("DoggoMon Drops")] 

    [Tooltip("Probability (0.0 to 1.0) of dropping Coin (30%).")]
    public float DoggoCoinDropChance = 0.30f;

#endregion


#region Utility Methods

    /// <summary>
    /// Generates a debug string showing the core stats of this enemy.
    /// </summary>
    public string GetStatsDebugString()
    {
        return $"{_displayName} ({TypeID}): HP={_baseHealth}, ATK={_baseAttackPower}, SPD={_baseMovementSpeed:F1}";
    }

#endregion
}