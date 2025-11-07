using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Animator _animator;

    private Vector2 _moveInput;
    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ReadInput();
        HandleActionInput();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    #region Input Handling
    private void ReadInput()
    {
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");
        _moveInput.Normalize();

        _animator?.SetFloat("MoveX", _moveInput.x);
        _animator?.SetFloat("MoveY", _moveInput.y);
        _animator?.SetFloat("Speed", _moveInput.sqrMagnitude);
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _player.Attack();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            _player.UseSkill();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _player.Interact(_player);
        }
    }
    #endregion

    #region Movement
    private void MovePlayer()
    {
        if (_rigidbody == null) return;

        _rigidbody.velocity = _moveInput * _moveSpeed;
    }
    #endregion
}
