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

    // Input actions
    private InputActionAsset _inputActions;
    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _lightAttack;
    private InputAction _heavyAttack;
    private InputAction _blockAction;

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
        bool isJumping = _jumpAction.IsPressed() ? true : false;
        _playerController.UpdateJumpInput(isJumping);

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
}
