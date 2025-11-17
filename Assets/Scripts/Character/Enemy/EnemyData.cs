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
    
    #region Utility Methods

    /// <summary>
    /// Generates a debug string showing the core stats of this enemy.
    /// </summary>
    public string GetStatsDebugString()
    {
        return $"{_displayName} ({_typeID}): HP={_baseHealth}, ATK={_baseAttackPower}, SPD={_baseMovementSpeed:F1}";
    }

    #endregion
}