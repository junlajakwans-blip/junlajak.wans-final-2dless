using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor for EnemyData assets.
/// This script renders the Inspector dynamically based on the selected _typeID.
/// </summary>
[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : Editor
{
    // Base Properties
    private SerializedProperty _typeID;
    private SerializedProperty _displayName;
    private SerializedProperty _baseHealth;
    private SerializedProperty _baseAttackPower;
    private SerializedProperty _baseMovementSpeed;
    private SerializedProperty _baseDetectionRange;
    private SerializedProperty _coinDropAmount;
    private SerializedProperty _enemySprite;

    private void OnEnable()
    {
        // Link base properties
        _typeID = serializedObject.FindProperty("_typeID");
        _displayName = serializedObject.FindProperty("_displayName");
        _baseHealth = serializedObject.FindProperty("_baseHealth");
        _baseAttackPower = serializedObject.FindProperty("_baseAttackPower");
        _baseMovementSpeed = serializedObject.FindProperty("_baseMovementSpeed");
        _baseDetectionRange = serializedObject.FindProperty("_baseDetectionRange");
        _coinDropAmount = serializedObject.FindProperty("_coinDropAmount");
        _enemySprite = serializedObject.FindProperty("_enemySprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Draw Base Properties
        EditorGUILayout.PropertyField(_typeID);
        EditorGUILayout.PropertyField(_displayName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Base Combat Stats", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_baseHealth);
        EditorGUILayout.PropertyField(_baseAttackPower);
        EditorGUILayout.PropertyField(_baseMovementSpeed);
        EditorGUILayout.PropertyField(_baseDetectionRange);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Reward & Visuals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_coinDropAmount);
        EditorGUILayout.PropertyField(_enemySprite);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("================================", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space();


        // 2. Draw Unique Stats based on selected TypeID
        EnemyType selectedType = (EnemyType)_typeID.enumValueIndex;

        switch (selectedType)
        {
            case EnemyType.MamaMon:
                DrawMamaMonProperties();
                break;
            case EnemyType.KahootMon:
                DrawKahootMonProperties();
                break;
            case EnemyType.LotteryMon: // 🚨 แก้ไขเป็น LotteryMon
                DrawLotteryMonProperties();
                break;
            case EnemyType.RedlightMon:
                DrawRedlightMonProperties();
                break;
            case EnemyType.GoldenMon:
                DrawGoldenMonProperties();
                break;
            case EnemyType.GhostWorkMon:
                DrawGhostWorkMonProperties();
                break;
            case EnemyType.MooPingMon:
                DrawMooPingMonProperties();
                break;
            case EnemyType.PeterMon:
                DrawPeterMonProperties();
                break;
            case EnemyType.DoggoMon:
                DrawDoggoMonProperties();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // --- Helper Methods to draw each section ---

    private void DrawMamaMonProperties()
    {
        EditorGUILayout.LabelField("MamaMon Settings (MamaMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaNoodleCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaAttackCooldown"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaBoilRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaBoilDamage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaProjectileSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaProjectileDamage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaHealChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaHealCooldown"));
        
        EditorGUILayout.LabelField("MamaMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaCoinDropChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MamaGreenTeaDropChance"));
    }

    private void DrawKahootMonProperties()
    {
        EditorGUILayout.LabelField("KahootMon Settings (KahootMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootAttackInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootBlockSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootBlockDamage"));

        EditorGUILayout.LabelField("KahootMon Behavior", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootSpeedDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootSlowModifier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootSlowDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootSmallDamage"));

        EditorGUILayout.LabelField("KahootMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("KahootCoinDropChance"));
    }

    private void DrawLotteryMonProperties()
    {
        EditorGUILayout.LabelField("LotteryMon Settings (LotteryMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryLuckFactor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryCurseDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryAttackCooldown"));
        
        EditorGUILayout.LabelField("LotteryMon Drop & Rewards", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryMinCoinDrop"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryMaxCoinDrop"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryGoodLuckMinCoin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LotteryGoodLuckMaxCoin"));
    }

    private void DrawRedlightMonProperties()
    {
        EditorGUILayout.LabelField("RedlightMon Settings (RedlightMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RedlightCarCooldown"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RedlightSwitchInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RedlightSpawnCarCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RedlightMaxCarTypes"));

        EditorGUILayout.LabelField("RedlightMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RedlightCoinDropChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("RedlightCoffeeDropChance"));
    }

    private void DrawGoldenMonProperties()
    {
        EditorGUILayout.LabelField("GoldenMon Settings (GoldenMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GoldenMonBreakPlatformCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GoldenMonCoinDropMultiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GoldenMonBaseMinCoin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GoldenMonBaseMaxCoin"));

        EditorGUILayout.LabelField("GoldenMon Card Drop", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GoldenGuaranteedCardType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GoldenCardDropChance"));
    }
    
    private void DrawGhostWorkMonProperties()
    {
        EditorGUILayout.LabelField("GhostWorkMon Settings (GhostWorkMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkFadeDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkHauntRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkTeleportCooldown"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkBaseTeleportDistance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkHauntDamage"));

        EditorGUILayout.LabelField("GhostWorkMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkCoinDropChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GhostWorkGreenTeaDropChance"));
    }
    
    private void DrawMooPingMonProperties()
    {
        EditorGUILayout.LabelField("MooPingMon Settings (MooPingMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingFireDamage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingSmokeRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingThrowCooldown"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingPatternSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingPatternWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingProjectileSpeed"));

        EditorGUILayout.LabelField("MooPingMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingCoinDropChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MooPingCoffeeDropChance"));
    }
    
    private void DrawPeterMonProperties()
    {
        EditorGUILayout.LabelField("PeterMon Settings (PeterMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterHoverAmplitude"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterHoverSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterAttackRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterAttackCooldown"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterProjectileDamage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterProjectileSpeed"));
        
        EditorGUILayout.LabelField("PeterMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterCoinDropChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PeterGreenTeaDropChance"));
    }
    
    private void DrawDoggoMonProperties()
    {
        EditorGUILayout.LabelField("DoggoMon Settings (DoggoMon.cs)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DoggoChaseSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DoggoBarkRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DoggoDamage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DoggoHauntRange"));

        EditorGUILayout.LabelField("DoggoMon Drops", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DoggoCoinDropChance"));
    }
}