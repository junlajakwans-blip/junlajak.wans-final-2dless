using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region Fields
    [SerializeField] protected float _speed;
    [SerializeField] protected int _attackPower;
    [SerializeField] protected float _detectionRange;
    [SerializeField] protected Transform _target;
    [SerializeField] protected EnemyType _enemyType;
    #endregion

    #region Properties
    public float Speed { get => _speed; set => _speed = value; }
    public int AttackPower { get => _attackPower; set => _attackPower = value; }
    public float DetectionRange { get => _detectionRange; set => _detectionRange = value; }
    public EnemyType EnemyType { get => _enemyType; set => _enemyType = value; }
    #endregion

    #region Unity Methods
    protected virtual void Update()
    {
        if (_target == null) return;

        if (DetectPlayer(_target.position))
        {
            Move();
            Attack();
        }
    }
    #endregion

    #region Core Methods
    public virtual void Move()
    {
        if (_target == null) return;

        Vector3 direction = (_target.position - transform.position).normalized;
        transform.position += direction * _speed * Time.deltaTime;
    }

    public virtual void Attack()
    {
        Debug.Log($"{_enemyType} attacks with {_attackPower} power!");
    }

    public virtual void TakeDamage(int amount)
    {
        Debug.Log($"{_enemyType} takes {amount} damage!");
    }

    public virtual bool DetectPlayer(Vector3 playerPos)
    {
        float distance = Vector3.Distance(transform.position, playerPos);
        return distance <= _detectionRange;
    }

    public virtual void Die()
    {
        Debug.Log($"{_enemyType} has died.");
        Destroy(gameObject);
    }
    #endregion
}
