using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Base Player class for all player characters. and on rumtime.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour, ISkillUser, IDamageable, IAttackable, IInteractable, ICollectable
{
    #region Fields
    [SerializeField] protected PlayerData _playerData;
    [SerializeField] protected DuckCareerData _currentCareer;
    [SerializeField] protected Animator _animator;
    [SerializeField, Tooltip("Physics body for movement and collision")] protected Rigidbody2D _rigidbody = null;

    private WaitForSeconds _speedWait;
    private WaitForSeconds _invincibleWait;
    private static readonly WaitForSeconds DEFAULT_INVINCIBLE_WAIT = new WaitForSeconds(1.5f);
    private WaitForSeconds _instanceWait;

    protected bool _isInvincible;
    protected float _invincibleTime = 1.5f;
    protected float _moveSpeed = 5f;
    private float _speedModifier = 1f;
    private Coroutine _speedRoutine;
    protected float _jumpForce = 8f;
    protected bool _isGrounded;
    private float _healRate = 1.0f;

    public bool CanInteract => throw new System.NotImplementedException();
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

            _currentHealth -= amount;
            _currentHealth = Mathf.Max(_currentHealth, 0);
            _animator?.SetTrigger("Hit");

            if (_currentHealth <= 0)
            {
                Die();
                return;
            }

            // Lazy create invincibility delay
            _invincibleWait ??= new WaitForSeconds(_invincibleTime);
            StartCoroutine(InvincibleCooldown());
        }

    public virtual void Heal(int amount) //Can heal during game for skill career or item
    {
        if (amount <= 0) return;

        int finalHeal = Mathf.RoundToInt(amount * _healRate);
        _currentHealth = Mathf.Min(_currentHealth + finalHeal, _maxHealth);

        _animator?.SetTrigger("Heal");
        Debug.Log($"[Player] Healed {finalHeal} HP (Total: {_currentHealth}/{_maxHealth})")
    }

    private IEnumerator InvincibleCooldown()
    {
        _isInvincible = true;

        // Auto choose best WaitForSeconds object
        if (Mathf.Approximately(_invincibleTime, 1.5f))
        {
            yield return DEFAULT_INVINCIBLE_WAIT; 
        }
        else
        {
            _instanceWait ??= new WaitForSeconds(_invincibleTime); // lazy init
            yield return _instanceWait;
        }

        _isInvincible = false;
    }
        #endregion


        #region Movement
    public virtual void Move(float direction)
    {
        if (_rigidbody == null) return;

        Vector2 velocity = new Vector2(direction * _moveSpeed * _speedModifier, _rigidbody.linearVelocity.y);
        _rigidbody.linearVelocity = velocity;

        if (direction != 0)
            transform.localScale = new Vector3(Mathf.Sign(direction), 1, 1);

        UpdateAnimation();
    }


    public void ApplySpeedModifier(float multiplier, float duration)
    {
        if (_speedRoutine != null)
            StopCoroutine(_speedRoutine);

        _speedWait = new WaitForSeconds(duration);
        _speedRoutine = StartCoroutine(SpeedModifierRoutine(multiplier));
    }

    private IEnumerator SpeedModifierRoutine(float multiplier)
    {
        _speedModifier = multiplier;
        yield return _speedWait; // ใช้ cache เดิม
        _speedModifier = 1f;
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

    public void Heal(int amount)
    {
        _health = Mathf.Min(_maxHealth, _health + amount);
        Debug.Log($"{_playerName} healed {amount}. HP: {_health}/{_maxHealth}");
    }


    #region Animation
    protected virtual void UpdateAnimation()
    {
        _animator?.SetBool("IsMoving", Mathf.Abs(_rigidbody.linearVelocity.x) > 0.1f);
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


    #region Interface Implementations
    public void ChargeAttack(float power)
    {
        throw new System.NotImplementedException();
    }

    public void RangeAttack(Transform target)
    {
        throw new System.NotImplementedException();
    }

    public void ApplyDamage(IDamageable target, int amount)
    {
        throw new System.NotImplementedException();
    }

    public void Interact(Player player)
    {
        throw new System.NotImplementedException();
    }

    public void ShowPrompt()
    {
        throw new System.NotImplementedException();
    }

    public void Collect(Player player)
    {
        throw new System.NotImplementedException();
    }

    public void OnCollectedEffect()
    {
        throw new System.NotImplementedException();
    }

    public string GetCollectType()
    {
        throw new System.NotImplementedException();
    }
    #endregion

}
