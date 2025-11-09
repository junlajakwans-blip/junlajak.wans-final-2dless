using UnityEngine;

/// <summary>
/// Defines a common interface for object pooling systems.
/// Used to efficiently spawn and recycle GameObjects during gameplay.
/// </summary>
public interface IObjectPool
{
    /// <summary>
    /// Initializes the object pool and prepares all objects for use.
    /// Should be called once during game setup or scene load.
    /// </summary>
    void InitializePool();

    /// <summary>
    /// Spawns an object from the pool using its tag name.
    /// </summary>
    /// <param name="objectTag">The tag or key name of the pooled object.</param>
    /// <param name="position">The world position to spawn the object at.</param>
    /// <param name="rotation">The rotation to apply to the spawned object.</param>
    /// <returns>The <see cref="GameObject"/> that was spawned.</returns>
    GameObject SpawnFromPool(string objectTag, Vector3 position, Quaternion rotation);

    /// <summary>
    /// Returns a spawned object back to its corresponding pool.
    /// </summary>
    /// <param name="objectTag">The tag or key name of the object pool.</param>
    /// <param name="obj">The <see cref="GameObject"/> instance to return.</param>
    void ReturnToPool(string objectTag, GameObject obj);

    /// <summary>
    /// Clears or resets all objects in the pool.
    /// Used when unloading scenes or performing a full system cleanup.
    /// </summary>
    void ClearPool();
}
