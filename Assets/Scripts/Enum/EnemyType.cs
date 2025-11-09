using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Show list Enemy Types Current 8 Enemies
public enum EnemyType
{
    None = 0,          // No Enemy Selected

    GhostWorkMon = 1,  // Appears in School maps
    MamaMon = 2,       // Also appears in Kitchen and School
    MooPingMon = 3,    // Also appears in Kitchen and School

    LottoryMon = 4,    // Appears in Road Traffic
    DoggoMon = 5,      // Appears in Road Traffic
    RedlightMon = 6,   // Appears in Road Traffic

    PeterMon = 7,      // Appears in Kitchen

    GoldenMon = 8      // Appears in All Maps (Rare) Can drop Card Career

}
#endregion

#region EnemyType Extensions

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
            EnemyType.MamaMon => new() { MapType.School, MapType.Kitchen },
            EnemyType.MooPingMon => new() { MapType.School, MapType.RoadTraffic },
            EnemyType.LottoryMon => new() { MapType.RoadTraffic },
            EnemyType.DoggoMon => new() { MapType.RoadTraffic },
            EnemyType.RedlightMon => new() { MapType.RoadTraffic },
            EnemyType.PeterMon => new() { MapType.Kitchen },
            EnemyType.GoldenMon => new() { MapType.All },
            _ => new() { MapType.None }
        };
    }
    

    public static bool IsAppearsInMap(this EnemyType enemyType, MapType mapType) //Check If EnemyType Can Appear in Given MapType 
    {
        var associated = enemyType.GetAssociatedMaps();
        return associated.Contains(mapType) || associated.Contains(MapType.All);
    }
#endregion
}



#region EnemyType Debug 

public static class EnemyTypeDebugExtensions
{
    public static string ToFriendlyString(this EnemyType enemyType) //Convert EnemyType to Friendly String
    {
        return enemyType switch
        {
            EnemyType.None => "No Enemy",
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


    public static string ToDebugString(this EnemyType enemyType)
    {
        var maps = string.Join(", ", enemyType.GetAssociatedMaps().Select(m => m.ToFriendlyString()));
        return $"{enemyType.ToFriendlyString()} [Maps: {maps}]";
    }
    
}

#endregion