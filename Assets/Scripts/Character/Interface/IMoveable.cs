using UnityEngine;

/// <summary>
/// Defines movement behavior for entities that can move within the game world.
/// Used by certain enemy types (e.g., DoggoMon, PeterMon, GoldenMon).
/// </summary>
public interface IMoveable
{
    /// <summary>Executes continuous or patterned movement behavior.</summary>
    void ChasePlayer(Player player);

    /// <summary>Stops current movement immediately.</summary>
    void Stop();

    /// <summary>Sets the movement direction for this entity.</summary>
    void SetDirection(Vector2 direction);
}
