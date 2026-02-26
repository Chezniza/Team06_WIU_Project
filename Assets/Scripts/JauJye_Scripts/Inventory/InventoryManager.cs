using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 6;

    public InventoryGrid Grid { get; private set; }
    public EquipmentSlots Equipment { get; private set; }
    public QuickSlots Quick { get; private set; }

    [Header("Starting Items")]
    [SerializeField] private ItemData[] startingItems;

    [Header("References")]
    [SerializeField] private Damageable _playerDamageable; // drag player here

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Grid = new InventoryGrid(gridWidth, gridHeight);
        Equipment = new EquipmentSlots();
        Quick = new QuickSlots();
    }

    private void Start()
    {
        foreach (var data in startingItems)
            AddItem(data);
    }

    // ?? Add to grid ???????????????????????????????????????????????????
    public bool AddItem(ItemData data)
    {
        var item = new InventoryItem(data);
        if (Grid.FindFreeSlot(item, out int x, out int y))
        {
            Grid.TryPlace(item, x, y);
            InventoryUI.Instance?.RefreshGrid();
            return true;
        }
        Debug.LogWarning($"No room for {data.itemName}");
        return false;
    }

    // ?? Right-click quick equip ???????????????????????????????????????
    public void QuickEquip(InventoryItem item)
    {
        Grid.Remove(item);
        switch (item.data.itemType)
        {
            case ItemType.Weapon:
                QuickEquipWeapon(item);
                break;
            case ItemType.Potion:
                QuickEquipPotion(item);
                break;
            case ItemType.Helmet:
            case ItemType.Chestplate:
            case ItemType.Pants:
            case ItemType.Boots:
            case ItemType.Gauntlets:
                QuickEquipArmour(item);
                break;
            case ItemType.Quest:
                ReturnToGrid(item);
                break;
        }
        InventoryUI.Instance?.RefreshAll();
        HotbarUI.Instance?.Refresh();
    }

    private void QuickEquipWeapon(InventoryItem item)
    {
        InventoryItem displaced = Quick.EquipWeaponToActive(item);
        if (displaced != null) ReturnToGrid(displaced);
    }

    private void QuickEquipPotion(InventoryItem item)
    {
        InventoryItem displaced = Quick.EquipPotion(item);
        if (displaced != null) ReturnToGrid(displaced);
    }

    private void QuickEquipArmour(InventoryItem item)
    {
        InventoryItem displaced = Equipment.Equip(item);

        // Add new armour's health bonus
        if (_playerDamageable != null)
            _playerDamageable.AddBonusHealth(item.data.statValue);

        // Remove displaced armour's health bonus if something was replaced
        if (displaced != null)
        {
            if (_playerDamageable != null)
                _playerDamageable.RemoveBonusHealth(displaced.data.statValue);
            ReturnToGrid(displaced);
        }
    }

    // ?? Manual drag equip ?????????????????????????????????????????????
    public void EquipArmourFromGrid(InventoryItem item)
    {
        Grid.Remove(item);
        InventoryItem displaced = Equipment.Equip(item);

        // Add new armour's health bonus
        if (_playerDamageable != null)
            _playerDamageable.AddBonusHealth(item.data.statValue);

        // Remove displaced armour's health bonus if something was replaced
        if (displaced != null)
        {
            if (_playerDamageable != null)
                _playerDamageable.RemoveBonusHealth(displaced.data.statValue);
            ReturnToGrid(displaced);
        }

        InventoryUI.Instance?.RefreshAll();
    }

    public void UnequipArmourToGrid(ItemType type)
    {
        InventoryItem item = Equipment.Unequip(type);

        // Remove unequipped armour's health bonus
        if (item != null)
        {
            if (_playerDamageable != null)
                _playerDamageable.RemoveBonusHealth(item.data.statValue);
            ReturnToGrid(item);
        }

        InventoryUI.Instance?.RefreshAll();
    }

    // ?? Helpers ???????????????????????????????????????????????????????
    public void ReturnToGrid(InventoryItem item)
    {
        if (Grid.FindFreeSlot(item, out int x, out int y))
            Grid.TryPlace(item, x, y);
        else
            Debug.LogWarning($"No room to return {item.data.itemName} to grid!");
    }
}