using Unity.Cinemachine;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static UnityEditor.SceneView;

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

        // Disable all cameras first
        _virtualCamera.gameObject.SetActive(false);
        _freeLookCamera.gameObject.SetActive(false);
        _FPCamera.gameObject.SetActive(false);

        switch (mode)
        {
            case CameraMode.ThirdPerson:
                _virtualCamera.gameObject.SetActive(true);
                break;

            case CameraMode.FreeLook:
                _freeLookCamera.gameObject.SetActive(true);
                break;

            case CameraMode.FirstPerson:
                _FPCamera.gameObject.SetActive(true);
                break;
        }
    }
}
