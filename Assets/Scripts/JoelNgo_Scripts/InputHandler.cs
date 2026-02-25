using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputHandler : MonoBehaviour
{
    // References
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Damageable damageable;
    // Combat
    [SerializeField] private AttackHandler _attackHandler;
    [SerializeField] private WeaponController _weaponController;
    [SerializeField] private BlockController _blockController;

    // Camera
    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private CinemachineCamera _freeLookCamera;
    [SerializeField] private CinemachineCamera _FPCamera;

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

    // Track active camera mode
    private enum CameraMode { ThirdPerson, FreeLook, FirstPerson }
    private CameraMode _currentCamera = CameraMode.ThirdPerson;

    // Any system can call LockControls / UnlockControls to freeze the player
    private bool _controlsLocked = false;

    public void LockControls()
    {
        _controlsLocked = true;

        // Immediately stop any movement/actions in progress
        _playerController.UpdateMoveInput(Vector2.zero);
        _playerController.UpdateSprintInput(false);
        _playerController.UpdateJumpInput(false);
        _blockController.StopBlock();
    }

    public void UnlockControls()
    {
        _controlsLocked = false;
    }

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

    void Update()
    {
        // Camera cycling always works (doesn't affect gameplay)
        CameraInput();

        // Inventory toggle always works
        // (InventoryUI handles its own toggle via PlayerInput directly)

        // Scene reset always works
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (_controlsLocked)
        {
            // Zero out movement so player doesn't slide
            _playerController.UpdateMoveInput(Vector2.zero);
            _playerController.UpdateSprintInput(false);
            _playerController.UpdateJumpInput(false);
            _blockController.StopBlock();
            return;
        }

        if (damageable.GetHealth() <= 0)
            return;

        // Movement
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        _playerController.UpdateMoveInput(moveInput);

        // Sprint
        _playerController.UpdateSprintInput(_sprintAction.IsPressed());

        // Jump
        _playerController.UpdateJumpInput(_jumpAction.WasPressedThisFrame());

        // Attack
        AttackInputs();

        // Block
        if (_blockAction.IsPressed())
            _blockController.StartBlock();
        else
            _blockController.StopBlock();

        // Weapon cycle
        if (Input.GetKeyDown(KeyCode.V))
            _weaponController.CycleWeapon();
    }

    private void AttackInputs()
    {
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

        if (_heavyAttack.WasPressedThisFrame())
            _attackHandler.RequestHeavyAttack();
    }

    private void CameraInput()
    {
        if (_inputActions["Next"].WasPressedThisFrame())
        {
            _currentCamera = (CameraMode)(((int)_currentCamera + 1) % System.Enum.GetValues(typeof(CameraMode)).Length);
            SetCameraMode(_currentCamera);
        }
        else if (_inputActions["Previous"].WasPressedThisFrame())
        {
            int count = System.Enum.GetValues(typeof(CameraMode)).Length;
            _currentCamera = (CameraMode)(((int)_currentCamera - 1 + count) % count);
            SetCameraMode(_currentCamera);
        }
    }

    private void SetCameraMode(CameraMode mode)
    {
        _currentCamera = mode;

        _virtualCamera.gameObject.SetActive(false);
        _freeLookCamera.gameObject.SetActive(false);
        _FPCamera.gameObject.SetActive(false);

        switch (mode)
        {
            case CameraMode.ThirdPerson: _virtualCamera.gameObject.SetActive(true); break;
            case CameraMode.FreeLook: _freeLookCamera.gameObject.SetActive(true); break;
            case CameraMode.FirstPerson: _FPCamera.gameObject.SetActive(true); break;
        }
    }
}