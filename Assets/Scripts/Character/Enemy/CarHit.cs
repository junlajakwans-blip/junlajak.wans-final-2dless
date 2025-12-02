using UnityEngine;

public class CarHit : MonoBehaviour
{
    private string _poolTag;
    private int _damage;
    private IObjectPool _pool;

    /// <summary>
    /// กำหนดค่า pool + tag + damage (เรียกตอน spawn)
    /// </summary>
    public void Init(IObjectPool pool, string poolTag, int damage)
    {
        _pool = pool;
        _poolTag = poolTag;
        _damage = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // เจอ Player → damage + despawn
        if (!other.TryGetComponent<Player>(out var player))
            return;

        player.TakeDamage(_damage);

        // ReturnToPool
        if (_pool != null)
            _pool.ReturnToPool(_poolTag, gameObject);
        else
            Destroy(gameObject); // กัน safety ถ้า pool หาย
    }
}
