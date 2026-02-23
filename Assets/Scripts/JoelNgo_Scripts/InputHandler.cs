using Unity.Cinemachine;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputHandler : MonoBehaviour
{
    // References
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Damageable damageable;
    [SerializeField] private AttackHandler _attackHandler;

    // Camera
    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;

    // Input actions
    private InputActionAsset _inputActions;
    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _lightAttack;
    private InputAction _heavyAttack;
    private InputAction _blockAction;

    // Attack repeat delay
    private float holdAttackTimer;
    [SerializeField] private float holdAttackInterval = 0.3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _inputActions = _playerInput.actions;

        _moveAction = _inputActions["Move"];
        _sprintAction = _inputActions["Sprint"];
        _jumpAction = _inputActions["Jump"];
        _lightAttack = _inputActions["Attack"];
        _heavyAttack = _inputActions["HeavyAttack"];
        _blockAction = _inputActions["Block"];
    }

    // Update is called once per frame
    void Update()
    {
        CameraInput();

        // Reset the scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Stop inputs when player is dead
        if (damageable.GetHealth() <= 0)
            return;

        // Movement inputs
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        _playerController.UpdateMoveInput(moveInput);

        // Sprint input
        bool isSprinting = _sprintAction.IsPressed() ? true : false;
        _playerController.UpdateSprintInput(isSprinting);

        // Jump input
        bool isJumping = _jumpAction.WasPressedThisFrame() ? true : false;
        _playerController.UpdateJumpInput(isJumping);

        // Attack input
        AttackInputs();

        // Block input
        if (_blockAction.IsPressed())
            _attackHandler.StartBlock();
        else
            _attackHandler.StopBlock();

        // Change weapon
        if (Input.GetKeyDown(KeyCode.V))
        {
            _attackHandler.CycleWeapon();
        }
    }

    private void AttackInputs()
    {
        // Light attack
        if (_lightAttack.IsPressed())
        {
            holdAttackTimer -= Time.deltaTime;

            if (holdAttackTimer <= 0f)
            {
                _attackHandler.RequestLightAttack();
                holdAttackTimer = holdAttackInterval;
            }
        }
        else
        {
            holdAttackTimer = 0f;
        }

        // Heavy attack
        if (_heavyAttack.WasPressedThisFrame())
            _attackHandler.RequestHeavyAttack();
    }

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
