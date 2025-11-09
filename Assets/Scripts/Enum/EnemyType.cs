using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region EnemyType Enum (9 Enemies)
public enum EnemyType
{
    None = 0,          // No Enemy Selected
    KahootMon = 1,     // School
    GhostWorkMon = 2,  // School
    LottoryMon = 3,    // Traffic
    DoggoMon = 4,      // Traffic
    RedlightMon = 5,   // Traffic
    PeterMon = 6,      // Kitchen
    MamaMon = 7,       // School,Traffic,Kitchen
    MooPingMon = 8,    // School,Traffic,Kitchen
    GoldenMon = 9      // All Maps (Rare)

}
#endregion

#region EnemyType Extensions

/// <summary>
/// Extension methods for logical behavior of EnemyType.
/// </summary>
public static class EnemyTypeExtensions
{
    // Logical Methods
    public static bool IsCommon(this EnemyType enemyType) //Check Is Common Enemy or Not
    {
        switch (enemyType)
        {
            case EnemyType.GhostWorkMon:
            case EnemyType.MamaMon:
            case EnemyType.MooPingMon:
            case EnemyType.LottoryMon:
            case EnemyType.DoggoMon:
            case EnemyType.RedlightMon:
            case EnemyType.PeterMon:
                return true;
            default:
                return false;
        }
    }

    public static bool IsRare(this EnemyType enemyType) //Check Is Rare Enemy or Not
    {
        return enemyType == EnemyType.GoldenMon;
    }

    public static List<MapType> GetAssociatedMaps(this EnemyType enemyType)
    {
        return enemyType switch
        {
            EnemyType.GhostWorkMon => new() { MapType.School },
            EnemyType.LottoryMon => new() { MapType.RoadTraffic },
            EnemyType.DoggoMon => new() { MapType.RoadTraffic },
            EnemyType.RedlightMon => new() { MapType.RoadTraffic },
            EnemyType.PeterMon => new() { MapType.Kitchen },
            EnemyType.MamaMon => new() { MapType.School, MapType.RoadTraffic, MapType.Kitchen },
            EnemyType.MooPingMon => new() { MapType.School, MapType.RoadTraffic, MapType.Kitchen },
            EnemyType.GoldenMon => new() { MapType.All },
            _ => new List<MapType>()
        };
    }


    public static bool CanAppearInMap(this EnemyType enemyType, MapType mapType) //Check If EnemyType Can Appear in Given MapType 
    {
        var associated = enemyType.GetAssociatedMaps();
        return associated.Contains(mapType) || associated.Contains(MapType.All);
    }
}
#endregion



#region EnemyType Debug 
#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// Debug and friendly name helpers for EnemyType.
/// </summary>
public static class EnemyTypeDebugExtensions
{
    public static string ToFriendlyString(this EnemyType enemyType) //Convert EnemyType to Friendly String
    {
        return enemyType switch
        {
            EnemyType.None => "No Enemy",
            EnemyType.KahootMon => "Kahoot Mon",
            EnemyType.GhostWorkMon => "Ghost Work Mon",
            EnemyType.MamaMon => "Mama Mon",
            EnemyType.MooPingMon => "Moo Ping Mon",
            EnemyType.LottoryMon => "Lottory Mon",
            EnemyType.DoggoMon => "Doggo Mon",
            EnemyType.RedlightMon => "Redlight Mon",
            EnemyType.PeterMon => "Peter Mon",
            EnemyType.GoldenMon => "Golden Mon",
            _ => "Unknown Enemy"
        };
    }


    public static string ToDebugString(this EnemyType enemyType) //Debug Info of EnemyType with Associated Maps
    {
        var maps = string.Join(", ", enemyType.GetAssociatedMaps().Select(m => m.ToFriendlyString() ?? m.ToString()));
        return $"{enemyType.ToFriendlyString()} [Maps: {maps}]";
    }

}
#endif

#endregion