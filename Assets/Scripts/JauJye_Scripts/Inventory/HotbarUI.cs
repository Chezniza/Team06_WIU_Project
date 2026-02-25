using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance { get; private set; }

    [Header("Weapon Slots (2)")]
    [SerializeField] private QuickSlotUI _weaponSlot1; // index 0
    [SerializeField] private QuickSlotUI _weaponSlot2; // index 1

    [Header("Potion Slot (1)")]
    [SerializeField] private QuickSlotUI _potionSlot;

    [Header("Input")]
    [SerializeField] private PlayerInput _playerInput;

    private InputAction _slot1Action;
    private InputAction _slot2Action;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _slot1Action = _playerInput.actions["WeaponSlot1"];
        _slot2Action = _playerInput.actions["WeaponSlot2"];

        Refresh();
    }

    private void Update()
    {
        if (_slot1Action.WasPressedThisFrame()) SetActiveWeapon(0);
        if (_slot2Action.WasPressedThisFrame()) SetActiveWeapon(1);
    }

    private void SetActiveWeapon(int index)
    {
        InventoryManager.Instance.Quick.SetActiveWeaponSlot(index);
        Refresh();
    }

    public void Refresh()
    {
        var quick = InventoryManager.Instance.Quick;

        // Weapon slots
        _weaponSlot1?.SetItem(quick.GetWeaponSlot(0));
        _weaponSlot1?.SetActive(quick.ActiveWeaponSlot == 0);

        _weaponSlot2?.SetItem(quick.GetWeaponSlot(1));
        _weaponSlot2?.SetActive(quick.ActiveWeaponSlot == 1);

        // Potion slot — no active state
        _potionSlot?.SetItem(quick.GetPotion());
        _potionSlot?.SetActive(false);
    }
}