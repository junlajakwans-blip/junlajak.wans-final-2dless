using UnityEngine;
[System.Serializable]
public class PlayerInputConfig
{
    public KeyCode left;
    public KeyCode right;
    public KeyCode jump;
    public KeyCode attack;
    public KeyCode skill;
    public KeyCode interact;
}

[RequireComponent(typeof(Player), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rigidbody;

    [SerializeField] private bool disableAnimation = true;
    [SerializeField] private Animator _animator;

    private Player _player;
    private Vector2 _moveInput;

    // NEW: Player ID (1 = P1, 2 = P2)
    
    [SerializeField] private int _playerID = 1;

    [SerializeField] private PlayerInputConfig _inputP1;
    [SerializeField] private PlayerInputConfig _inputP2;
    private PlayerInputConfig _input;

    public void SetPlayerID(int id)
    {
        _playerID = id;
        _input = (_playerID == 1) ? _inputP1 : _inputP2;
    }

    private void Awake()
    {
        _player = GetComponent<Player>();
        _rigidbody = GetComponent<Rigidbody2D>();
        
        SetPlayerID(_playerID);

        if (disableAnimation)
        {
            _animator = null;
        }
        else
        {
            _animator = GetComponentInChildren<Animator>();
        }

        if (_animator == null)
        {
            Debug.LogWarning("[PlayerController] Animator not found.");
        }

        if (_player == null) Debug.LogError("Player script not found!");
        if (_rigidbody == null) Debug.LogError("Rigidbody2D not found!");
    }

    private void Update()
    {
        if (_input == null) return;

        HandleMovementInput();
        HandleActionInput();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    #region Input

    // private void HandleMovementInput()
    // {
    //     _moveInput.x = 0;

    //     // แยก input ตาม PlayerID
    //     if (_playerID == 1)
    //     {
    //         if (Input.GetKey(KeyCode.A)) _moveInput.x = -1;
    //         else if (Input.GetKey(KeyCode.D)) _moveInput.x = 1;
    //     }
    //     else if (_playerID == 2)
    //     {
    //         if (Input.GetKey(KeyCode.LeftArrow)) _moveInput.x = -1;
    //         else if (Input.GetKey(KeyCode.RightArrow)) _moveInput.x = 1;
    //     }

    //     if (_animator != null)
    //     {
    //         _animator.SetFloat("MoveX", _moveInput.x);
    //         _animator.SetFloat("Speed", Mathf.Abs(_moveInput.x));
    //     }
    // }
    private void HandleMovementInput()
    {
        _moveInput.x = 0;


        if (Input.GetKey(_input.left)) 
            _moveInput.x = -1;
        else if (Input.GetKey(_input.right)) 
            _moveInput.x = 1;

        if (_animator != null)
        {
            _animator.SetFloat("MoveX", _moveInput.x);
            _animator.SetFloat("Speed", Mathf.Abs(_moveInput.x));
        }
    }

    // private void HandleActionInput()
    // {
    //     // PLAYER 1 CONTROLS
    //     if (_playerID == 1)
    //     {
    //         if (Input.GetKeyDown(KeyCode.W))
    //         {
    //             DebugAction("P1 ATTACK");
    //             _player.Attack();
    //         }

    //         if (Input.GetKeyDown(KeyCode.Space))
    //         {
    //             DebugAction("P1 JUMP");
    //             _player.Jump();
    //         }

    //         if (Input.GetKeyDown(KeyCode.R))
    //         {
    //             DebugAction("P1 SKILL");
    //             _player.UseSkill();
    //         }

    //         if (Input.GetKeyDown(KeyCode.E))
    //         {
    //             DebugAction("P1 INTERACT");
    //             _player.HandleInteract();
    //         }
    //     }

    //     // PLAYER 2 CONTROLS
    //     else if (_playerID == 2)
    //     {
    //         if (Input.GetKeyDown(KeyCode.UpArrow))
    //         {
    //             DebugAction("P2 ATTACK");
    //             _player.Attack();
    //         }

    //         if (Input.GetKeyDown(KeyCode.Keypad0))
    //         {
    //             DebugAction("P2 JUMP");
    //             _player.Jump();
    //         }

    //         if (Input.GetKeyDown(KeyCode.Keypad1))
    //         {
    //             DebugAction("P2 SKILL");
    //             _player.UseSkill();
    //         }

    //         if (Input.GetKeyDown(KeyCode.Keypad2))
    //         {
    //             DebugAction("P2 INTERACT");
    //             _player.HandleInteract();
    //         }
    //     }

    //     // Pause (ใช้ร่วมกัน)
    //     if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         TogglePauseGame();
    //     }
    // }

    private void HandleActionInput()
    {
        // ใช้ input config แทนทั้งหมด
        if (Input.GetKeyDown(_input.jump))
        {
            DebugAction($"P{_playerID} JUMP");
            _player.Jump();
        }

        if (Input.GetKeyDown(_input.attack))
        {
            DebugAction($"P{_playerID} ATTACK");
            _player.Attack();
        }

        if (Input.GetKeyDown(_input.skill))
        {
            DebugAction($"P{_playerID} SKILL");
            _player.UseSkill();
        }

        if (Input.GetKeyDown(_input.interact))
        {
            DebugAction($"P{_playerID} INTERACT");
            _player.HandleInteract();
        }

        // ให้ Player1 คุม pause คนเดียว
        if (_playerID == 1)
        {
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseGame();
            }
        }
    }

    private void DebugAction(string msg)
    {
        Debug.Log($"<color=#ffc800><b>[INPUT]</b></color> {msg}");
    }

    #endregion

    #region Movement

    private void MovePlayer()
    {
        if (_player == null) return;
        _player.Move(new Vector2(_moveInput.x, 0));
    }

    #endregion

    #region Pause

    private void TogglePauseGame()
    {
        bool isPaused = Time.timeScale == 0;
        Time.timeScale = isPaused ? 1 : 0;

        Debug.Log(isPaused ? "[Game] Resumed" : "[Game] Paused");
    }

    #endregion
}