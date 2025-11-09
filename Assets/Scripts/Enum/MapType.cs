using UnityEngine;

#region Show Map Types

public enum MapType
{
    None = 0,          // No Map Selected
    School = 1,     // Classroom Level
    RoadTraffic = 2,   // Road Traffic Level
    Kitchen = 3,       // Kitchen Level
    All = 99           // Used for Enemy/Item that appear in all levels
}
#endregion


#region MapType Extensions

public static class MapTypeExtensions
{
    // Logical Methods
    public static bool IsPlayableMap(this MapType mapType) //Check Is Playable Map or Not
    {
        switch (mapType)
        {
            case MapType.School:
            case MapType.RoadTraffic:
            case MapType.Kitchen:
                return true;
            default:
                return false;
        }
    }

    public static string ToSceneName(this MapType mapType) //Convert MapType to Scene Name
    {
        return mapType switch
        {
            MapType.School => "SchoolScene",
            MapType.RoadTraffic => "RoadScene",
            MapType.Kitchen => "KitchenScene",
            _ => string.Empty
        };
    }

    public static MapType FromSceneName(string sceneName) //Convert Scene Name to MapType
    {
        if (string.IsNullOrEmpty(sceneName)) return MapType.None;

        sceneName = sceneName.ToLower();
        if (sceneName.Contains("school")) return MapType.School;
        if (sceneName.Contains("road")) return MapType.RoadTraffic;
        if (sceneName.Contains("kitchen")) return MapType.Kitchen;

        return MapType.None;
    }

    public static bool IsUniversal(this MapType mapType) //Check Is Universal MapType (Can Spawn in All Maps)
    {
        return mapType == MapType.All;
    }

    #endregion


    #region Debug Part

    public static string ToFriendlyString(this MapType mapType) //Collect Friendly Map Name
    {
        return mapType switch
        {
            MapType.None => "No Map",
            MapType.School => "School",
            MapType.RoadTraffic => "Road Traffic",
            MapType.Kitchen => "Kitchen",
            MapType.All => "All Maps",
            _ => "Unknown Map"
        };
    }

    public static string DebugInfo(this MapType mapType) //Collect Debug Map Name
    {
        string scene = mapType.ToSceneName();
        string friendly = mapType.ToFriendlyString();
        return $"[{mapType}]  Scene='{scene}'  Label='{friendly}'";
    }
    #endregion
}

