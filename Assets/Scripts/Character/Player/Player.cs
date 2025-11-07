using UnityEngine;

public class Player : MonoBehaviour, ISkillUser
{
    #region Fields
    [SerializeField] protected PlayerData _playerData;
    [SerializeField] protected DuckCareerData _currentCareer;
    [SerializeField] protected Animator _animator;
    [SerializeField] protected Rigidbody2D _rigidbody;

    protected bool _isInvincible;
    protected float _invincibleTime = 1.5f;
    protected float _moveSpeed = 5f;
    protected float _jumpForce = 8f;
    protected bool _isGrounded;
    #endregion

    #region Initialization
    public virtual void Initialize(PlayerData data)
    {
        _playerData = data;
        _playerData.ResetPlayerState();
        _isInvincible = false;
        Debug.Log($"Player initialized: {_playerData.PlayerName}");
    }
    #endregion

    #region Health
    public virtual void TakeDamage(int amount)
    {
        if (_isInvincible) return;

        _playerData.TakeDamage(amount);
        _animator?.SetTrigger("Hit");

        if (_playerData.Health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibleCooldown());
        }
    }

    public virtual void Heal(int amount)
    {
        _playerData.Heal(amount);
        _animator?.SetTrigger("Heal");
    }

    private System.Collections.IEnumerator InvincibleCooldown()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(_invincibleTime);
        _isInvincible = false;
    }
    #endregion

    #region Movement
    public virtual void Move(float direction)
    {
        if (_rigidbody == null) return;

        Vector2 velocity = new Vector2(direction * _moveSpeed, _rigidbody.velocity.y);
        _rigidbody.velocity = velocity;

        if (direction != 0)
            transform.localScale = new Vector3(Mathf.Sign(direction), 1, 1);

        UpdateAnimation();
    }

    public virtual void Jump()
    {
        if (!_isGrounded) return;

        _rigidbody.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        _animator?.SetTrigger("Jump");
        _isGrounded = false;
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            _isGrounded = true;
    }
    #endregion

    #region Career & Skills
    public virtual void SwitchCareer(DuckCareerData newCareer)
    {
        if (newCareer == null) return;

        _currentCareer = newCareer;
        _playerData.SelectedCareer = newCareer.CareerID.ToString();
        Debug.Log($"Switched career to {_currentCareer.DisplayName}");
    }

    public virtual void UseSkill()
    {
        Debug.Log($"{_playerData.PlayerName} used skill of {_currentCareer.CareerID}");
    }

    public virtual void OnSkillCooldown()
    {
        Debug.Log("Skill on cooldown...");
    }
    #endregion

    #region Combat
    public virtual void Attack()
    {
        _animator?.SetTrigger("Attack");
        Debug.Log($"{_playerData.PlayerName} attacks!");
    }
    #endregion

    #region Animation
    protected virtual void UpdateAnimation()
    {
        _animator?.SetBool("IsMoving", Mathf.Abs(_rigidbody.velocity.x) > 0.1f);
    }
    #endregion

    #region Death
    public virtual void Die()
    {
        Debug.Log($"{_playerData.PlayerName} has died!");
        _animator?.SetTrigger("Die");
        this.enabled = false;
    }
    #endregion
}
