using UnityEngine;
using UnityEngine.InputSystem;

public class ShortcutWheel : MonoBehaviour
{
    public static ShortcutWheel Instance { get; private set; }

    [Header("Items")]
    public ShortcutWheelItem[] items;

    [Header("References")]
    [SerializeField] private ShortcutWheelUI _wheelUI;
    [SerializeField] private InputHandler _inputHandler;

    [Header("Key to hold")]
    [SerializeField] private KeyCode _wheelKey = KeyCode.Tab;

    private bool _isOpen = false;
    private int _selectedIndex = -1;
    private Vector2 _wheelCenter;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(_wheelKey))
        {
            // If a panel is already open, close it instead of opening wheel
            if (TryCloseOpenPanel()) return;

            OpenWheel();
        }

        if (Input.GetKeyUp(_wheelKey))
            CloseAndSelect();

        if (!_isOpen) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 delta = mousePos - _wheelCenter;

        if (delta.magnitude > 30f)
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            angle = (90f - angle + 360f) % 360f;
            float segSize = 360f / items.Length;
            _selectedIndex = Mathf.FloorToInt(angle / segSize) % items.Length;
        }
        else
        {
            _selectedIndex = -1;
        }

        _wheelUI.UpdateSelection(_selectedIndex);
    }

    // Returns true if a panel was open and got closed
    private bool TryCloseOpenPanel()
    {
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsInventoryOpen())
        {
            InventoryUI.Instance.ToggleInventory();
            return true;
        }

        if (QuestLogUI.Instance != null && QuestLogUI.Instance.IsOpen())
        {
            QuestLogUI.Instance.Hide();
            return true;
        }

        return false;
    }

    private void OpenWheel()
    {
        if (_isOpen) return;
        _isOpen = true;
        _selectedIndex = -1;

        _inputHandler?.LockControls();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _wheelCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        _wheelUI.Show(items);
    }

    private void CloseAndSelect()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_selectedIndex >= 0 && _selectedIndex < items.Length)
            items[_selectedIndex].onSelect?.Invoke();

        if (InventoryUI.Instance == null || !InventoryUI.Instance.IsInventoryOpen())
        {
            if (QuestLogUI.Instance == null || !QuestLogUI.Instance.IsOpen())
            {
                _inputHandler?.UnlockControls();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        _wheelUI.Hide();
    }

    public bool IsOpen() => _isOpen;
}