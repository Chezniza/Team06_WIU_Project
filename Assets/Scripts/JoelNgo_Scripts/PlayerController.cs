using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private Animator _animator;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private AttackHandler _attackHandler;
    [SerializeField] private Damageable damageable;
    // Camera
    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;

    // Input actions
    private InputActionAsset _inputActions;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _lightAttack;
    private InputAction _heavyAttack;
    private InputAction _blockAction;

    // Walk
    Vector3 moveDirection;
    // Jump
    private Vector3 jumpVelocity;
    [SerializeField] public float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 2f;
    private bool _jumpLanded = true;

    // Start is called before the first frame update
    void Start()
    {
        _inputActions = _playerInput.actions;
        _sprintAction = _playerInput.actions["Sprint"];
        _jumpAction = _playerInput.actions["Jump"];
        _lightAttack = _playerInput.actions["Attack"];
        _heavyAttack = _playerInput.actions["HeavyAttack"];
        _blockAction = _playerInput.actions["Block"];
    }
    void Update()
    {
        // Reset the scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (damageable.GetHealth() <= 0)
            return;

        Walk();
        Sprint();

        if (_characterController.isGrounded && jumpVelocity.y < 0)
            jumpVelocity.y = -2f;

        if (_jumpAction.WasPressedThisFrame() && _characterController.isGrounded && _jumpLanded)
        {
            // Prevent jumping while attacking and blocking
            if (_attackHandler.IsAttacking() || _attackHandler.IsBlocking())
                return;

            jumpVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _animator.SetBool("IsJumping", true);
            _jumpLanded = false; // will be reset by the landing animation
        }

        jumpVelocity.y += gravity * Time.deltaTime;
        moveDirection.y = jumpVelocity.y;

        // Prevent movement while attacking
        if (_attackHandler.IsAttacking() || _attackHandler.IsBlocking())
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
        }

        _characterController.Move(moveDirection * Time.deltaTime);

        // Animator states AFTER Move
        bool grounded = _characterController.isGrounded;
        _animator.SetBool("IsJumping", !grounded && jumpVelocity.y > 0);
        _animator.SetBool("IsFalling", !grounded && jumpVelocity.y < 0);

        // Attack input
        if (_lightAttack.WasPressedThisFrame())
            _attackHandler.RequestLightAttack();

        if (_heavyAttack.WasPressedThisFrame())
            _attackHandler.RequestHeavyAttack();

        // Block input
        if (_blockAction.IsPressed())
            _attackHandler.StartBlock();
        else
            _attackHandler.StopBlock();
    }

    private void Sprint()
    {
        if (_sprintAction.IsPressed())
            _animator.SetBool("IsRun", true);
        else
            _animator.SetBool("IsRun", false);
    }

    private void Walk()
    {
        Vector2 input = _inputActions["Move"].ReadValue<Vector2>();
        bool isMoving = input.sqrMagnitude > 0.01f;
        _animator.SetBool("IsWalking", isMoving);

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        moveDirection = camForward * input.y + camRight * input.x;

        // Send input to Animator for blend tree
        _animator.SetFloat("MoveX", input.x, 0.1f, Time.deltaTime);
        _animator.SetFloat("MoveY", input.y, 0.1f, Time.deltaTime);

        // Camera rotation (camera forward)
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
        }
    }

    public void AddVerticalVelocity(float v) { jumpVelocity.y = v; }

    private void jumpLanded() { _jumpLanded = true; }

    private void CameraInput()
    {
        // Camera blending
        if (_inputActions["Previous"].IsPressed())
        {
            _virtualCamera.gameObject.SetActive(true);
            _freeLookCamera.gameObject.SetActive(false);
        }
        else if (_inputActions["Next"].IsPressed())
        {
            _virtualCamera.gameObject.SetActive(false);
            _freeLookCamera.gameObject.SetActive(true);
        }
    }
}