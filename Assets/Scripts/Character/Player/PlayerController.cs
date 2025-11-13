using UnityEngine;

/// <summary>
/// Handles player input and movement.
/// Works with Player, Rigidbody2D, and Animator.
/// </summary>
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
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
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
        // --------------------------------------
        //  MOVE LEFT / RIGHT (A, D, ←, →)
        // --------------------------------------
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            _moveInput.x = -1;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            _moveInput.x = 1;
        else
            _moveInput.x = 0;

        // --------------------------------------
        // JUMP (space)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _player.Jump();
        }

        // --------------------------------------
        // USE CAREER SKILL (W)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.W))
        {
            _player.UseSkill();
        }

        // --------------------------------------
        // INTERACT / PICKUP (E)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Only Duckling can interact for now
            if (_player.GetCurrentCareerID() == DuckCareer.Duckling)
                _player.Interact(_player);
        }

        // --------------------------------------
        // THROW ITEM (R)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (_player.GetCurrentCareerID() == DuckCareer.Duckling)
                _player.ThrowItem();
        }

        // --------------------------------------
        // PAUSE GAME (P / ESC)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseGame();
        }
    }
    #endregion

    #region Movement
    private void MovePlayer()
    {
        if (_rigidbody == null) return;

        _rigidbody.linearVelocity = _moveInput * _moveSpeed;
    }
    #endregion

    #region Game Control
    private void TogglePauseGame()
    {
        bool isPaused = Time.timeScale == 0;
        Time.timeScale = isPaused ? 1 : 0;

        Debug.Log(isPaused ? "[Game] Resumed" : "[Game] Paused");
    }
    #endregion
}
