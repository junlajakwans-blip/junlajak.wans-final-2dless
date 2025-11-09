using UnityEngine;

/// <summary>
/// Defines a contract for any object or system that can spawn and despawn entities.
/// Typically implemented by systems such as <see cref="EnemySpawner"/>, <see cref="ItemSpawner"/>, or other managers.
/// </summary>
public interface ISpawn
{
    /// <summary>
    /// Spawns an object at a default or randomized position.
    /// </summary>
    void Spawn();

    /// <summary>
    /// Spawns an object at the specified position in the scene.
    /// </summary>
    /// <param name="position">The world position where the object should be spawned.</param>
    /// <returns>The <see cref="GameObject"/> that was spawned.</returns>
    GameObject SpawnAtPosition(Vector3 position);

    /// <summary>
    /// Despawns (or disables) the specified object, returning it to the pool or destroying it.
    /// </summary>
    /// <param name="obj">The <see cref="GameObject"/> to be despawned.</param>
    void Despawn(GameObject obj);

    /// <summary>
    /// Returns the number of currently active spawned objects.
    /// Useful for performance control or spawn limits.
    /// </summary>
    /// <returns>The number of active spawned objects.</returns>
    int GetSpawnCount();
}
