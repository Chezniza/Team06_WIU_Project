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

    /*
    * Stamina system reference
    * M 20 Feb
    */
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private float exhaustedSpeed = 2f;

    /*
     * Walkspeed calculations
     * M 20 Feb
     */
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    private float currentSpeed;

    // Walk
    Vector3 moveDirection;
    Vector2 moveInput;

    // Sprint
    bool isSprinting;

    // Jump
    bool isJumping;
    private Vector3 jumpVelocity;
    [SerializeField] public float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 2f;
    private bool _jumpLanded = true;

    // Start is called before the first frame update
    void Start()
    {
        moveInput = Vector2.zero;
        isSprinting = false;
        isJumping = false;
    }

    void Update()
    {
        Walk();
        Sprint();

        if (_characterController.isGrounded && jumpVelocity.y < 0)
            jumpVelocity.y = -2f;

        if (isJumping && _characterController.isGrounded && _jumpLanded)
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

        /*
         * Walkspeed calculations
         * M 20 Feb
         */

        // Check if the character is sprinting - M 20 Feb
        if (isSprinting && moveInput.sqrMagnitude > 0.1f)
        {
            if (!staminaSystem.UseStamina(20f * Time.deltaTime))
            {
                isSprinting = false;
            }
        }

        if (staminaSystem.isExhausted)
        {
            currentSpeed = exhaustedSpeed;
        }
        else
        {
            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        }

        Vector3 finalMove = moveDirection * currentSpeed;
        finalMove.y = jumpVelocity.y;

        _characterController.Move(finalMove * Time.deltaTime);

        



        // Animator states AFTER Move
        bool grounded = _characterController.isGrounded;
        _animator.SetBool("IsJumping", !grounded && jumpVelocity.y > 0);
        _animator.SetBool("IsFalling", !grounded && jumpVelocity.y < 0);
    }

    private void Sprint()
    {
        if (isSprinting)
            _animator.SetBool("IsRun", true);
        else
            _animator.SetBool("IsRun", false);
    }

    private void Walk()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        _animator.SetBool("IsWalking", isMoving);

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        moveDirection = camForward * moveInput.y + camRight * moveInput.x;

        // Send input to Animator for blend tree
        _animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        _animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);

        // Camera rotation (camera forward)
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
        }
    }

    public void UpdateMoveInput(Vector2 v) { moveInput = v; }
    public void UpdateSprintInput(bool v) { isSprinting = v; }
    public void UpdateJumpInput(bool v) { isJumping = v; }

    public void AddVerticalVelocity(float v) { jumpVelocity.y = v; }

    private void jumpLanded() { _jumpLanded = true; }
}