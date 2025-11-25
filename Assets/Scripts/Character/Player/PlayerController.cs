using UnityEngine;

/// <summary>
/// Handles player input and movement.
/// Works with Player, Rigidbody2D, and Animator.
/// </summary>
// RequireComponent นี้ช่วยให้มั่นใจว่า GameObject นี้จะมีคอมโพเนนต์เหล่านี้เสมอ
[RequireComponent(typeof(Player), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 3.5f;

    [Header("Interact Settings")]
    [SerializeField] private PlayerInteract _interact;



    private Rigidbody2D _rigidbody;
    [SerializeField] private bool disableAnimation = true;

    [SerializeField] private Animator _animator;
    private Player _player;
    

    private Vector2 _moveInput; // ใช้เก็บ Input ที่ต้องการใช้ในการเคลื่อนที่

    private void Awake()
    {
        _player = GetComponent<Player>();
        _rigidbody = GetComponent<Rigidbody2D>();

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
            Debug.LogWarning("[PlayerController] Animator not found — animation disabled for now.");
        }
        
        // ตรวจสอบความผิดพลาด (Guard Clauses)
        if (_player == null) Debug.LogError("Player script not found on the same GameObject!");
        if (_rigidbody == null) Debug.LogError("Rigidbody2D not found!");

        _player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        // 1. อ่าน Input ใน Update()
        HandleMovementInput();
        HandleActionInput();
        
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }


    #region Input Handling

    private void HandleMovementInput()
    {
        // --------------------------------------
        // MOVE LEFT / RIGHT (A, D, ←, →)
        // --------------------------------------
        
        // เริ่มต้นด้วยการรีเซ็ต Input แกน X
        _moveInput.x = 0; 

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            _moveInput.x = -1;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            _moveInput.x = 1;
        }
        
        if (_animator != null)
        {
        // อัปเดต Animator ตาม Input ใหม่
        _animator?.SetFloat("MoveX", _moveInput.x);
        // ใช้ค่าสัมบูรณ์ (Abs) สำหรับความเร็วในการเคลื่อนไหวแนวนอน
        _animator?.SetFloat("Speed", Mathf.Abs(_moveInput.x)); 
        }
    }

    private void HandleActionInput()
    {
        // --------------------------------------
        // Attack (w)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.W))
        {
            _player.Attack();
        }

        // --------------------------------------
        // JUMP (space) - ใช้ GetKeyDown เพื่อให้กระโดดแค่ครั้งเดียวเมื่อกดปุ่ม
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _player.Jump();
        }

        // --------------------------------------
        // USE CAREER SKILL (R)
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.R))
        {
            _player.UseSkill();
        }

        // --------------------------------------
        // INTERACT / PICKUP (Q) - หากคุณต้องการใช้ W ในการเก็บของ / ปาของ / เปลี่ยนของ
        // --------------------------------------
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _player.HandleInteract();   // เรียกให้ Player ตัดสินใจ
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
        
        // การคงค่า Y เดิมทำให้ Rigidbody ยังคงได้รับผลกระทบจากแรงโน้มถ่วง (Gravity) และการกระโดด
        float targetSpeedX = _moveInput.x * _moveSpeed;

        // ใช้ Lerp หรือ Slerp เพื่อให้การเคลื่อนไหวดูนุ่มนวลขึ้นเล็กน้อย
        float newVelocityX = Mathf.Lerp(_rigidbody.linearVelocity.x, targetSpeedX, Time.fixedDeltaTime * 10f); // 10f คือค่า Acceleration/Responsiveness

        _rigidbody.linearVelocity = new Vector2(newVelocityX, _rigidbody.linearVelocity.y);
    }

    #endregion

    #region Game Control
    private void TogglePauseGame()
    {
        // ตรวจสอบ Time.timeScale ก่อน
        bool isPaused = Time.timeScale == 0;
        
        // สลับสถานะ: ถ้าหยุดอยู่ (isPaused = true) ให้ตั้งเป็น 1 (เล่นต่อ), ถ้าไม่หยุด (isPaused = false) ให้ตั้งเป็น 0 (หยุด)
        Time.timeScale = isPaused ? 1 : 0; 

        Debug.Log(isPaused ? "[Game] Resumed" : "[Game] Paused");
    }
    #endregion
}