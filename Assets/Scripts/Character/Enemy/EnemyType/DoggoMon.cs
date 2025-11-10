using UnityEngine;
using System.Collections;

public class DoggoMon : Enemy, IMoveable
{
    [Header("Doggo Attributes")]
    [SerializeField] private float _walkSpeed = 1.5f;
    [SerializeField] private float _chaseSpeed = 3.5f;
    [SerializeField] private float _barkRange = 3f;
    [SerializeField] private float _detectRange = 5f;
    [SerializeField] private int _biteDamage = 5;

    [Header("Patrol Settings")]
    [SerializeField] private float _patrolLimitLeft = -5f;
    [SerializeField] private float _patrolLimitRight = 5f;

    private Vector2 _direction = Vector2.right;
    private bool _isChasing = false;
    private bool _isDead = false;
    private bool _isStunned = false;

    private void FixedUpdate()
    {
        if (_isDead || _isStunned) return;

        if (_target == null)
        {
            Move(); // Patrol if no target
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);

        if (distance <= _detectRange)  // detect player
        {
            if (distance <= _barkRange)
            {
                Bark();
                _isChasing = true;
            }
        }

        if (_isChasing)
        {
            ChasePlayer(_target.GetComponent<Player>());
        }
        else
        {
            Move();
        }
    }

    // Normal patrol movement in limited area
    public override void Move()
    {
        transform.position += (Vector3)_direction * _walkSpeed * Time.deltaTime;

        // If walking beyond limit, change direction back
        if (transform.position.x < _patrolLimitLeft)
            _direction = Vector2.right;
        else if (transform.position.x > _patrolLimitRight)
            _direction = Vector2.left;
    }

    // Bark to startle the player when nearby
    public void Bark()
    {
        Debug.Log($"{_enemyType} barks! Player is startled!");
        if (_target != null)
        {
            Player player = _target.GetComponent<Player>();
            if (player != null)
                player.ApplySpeedModifier(0.8f, 2f); //Slow player for 2 seconds
        }
    }

    // When player is within chase range chase and bark
    public void ChasePlayer(Player player)
    {
        if (player == null) return; //if no player no chase

        // Move towards player when nearby
        Vector3 direction = (player.transform.position - transform.position).normalized;
        transform.position += direction * _chaseSpeed * Time.deltaTime;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance < 1.2f)
        {
            Attack();
            player.TakeDamage(_biteDamage);
            _isChasing = false; // Stop chasing after one bite
        }
        else if (distance > _detectRange * 1.5f)
        {
            _isChasing = false; // Stop chasing if out of range
        }
    }

    // bite attack
    public override void Attack()
    {
        Debug.Log($"{_enemyType} bites the player for {_biteDamage} damage!");
    }

    // Stun the enemy briefly when taking damage from player
    public override void TakeDamage(int amount)
    {
        if (_isDead) return;

        Debug.Log($"{_enemyType} takes {amount} damage and is stunned!");
        StartCoroutine(Stun(1.5f));
    }

    private IEnumerator Stun(float duration) //stun self
    {
        _isStunned = true;
        yield return new WaitForSeconds(duration);
        _isStunned = false;
    }

    public override void Die() // Die and remove from scene
    {
        if (_isDead) return;
        _isDead = true;
        StopAllCoroutines();
        Debug.Log($"{_enemyType} has fainted.");
        Destroy(gameObject, 1.2f);
    }

    public void Stop() // Stop movement when die
    {
        _direction = Vector2.zero;
    }

    public void SetDirection(Vector2 direction) // Set movement direction
    {
        _direction = direction.normalized;
    }
}
