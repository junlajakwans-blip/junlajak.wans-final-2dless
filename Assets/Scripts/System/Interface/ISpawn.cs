using UnityEngine;

public interface ISpawn
{

    void Spawn();

    GameObject SpawnAtPosition(Vector3 position);

    void Despawn(GameObject obj);

    int GetSpawnCount();
}
