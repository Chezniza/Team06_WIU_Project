using NUnit.Framework.Interfaces;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 6;

    public InventoryGrid Grid { get; private set; }
    public EquipmentSlots Equipment { get; private set; }

    [Header("Test Items - drag ItemData here to auto-add on start")]
    [SerializeField] private ItemData[] startingItems;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Grid = new InventoryGrid(gridWidth, gridHeight);
        Equipment = new EquipmentSlots();
    }

    private void Start()
    {
        foreach (var itemData in startingItems)
            AddItem(itemData);
    }

    // Add item to first available grid slot
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

    // Equip from grid to armour slot
    public void EquipFromGrid(InventoryItem item)
    {
        Grid.Remove(item);
        InventoryItem displaced = Equipment.Equip(item);
        if (displaced != null)
        {
            // Put displaced item back in grid
            if (!Grid.FindFreeSlot(displaced, out int x, out int y) || !Grid.TryPlace(displaced, x, y))
                Debug.LogWarning($"No room to return {displaced.data.itemName} to grid!");
        }
        InventoryUI.Instance?.RefreshAll();
    }

    // Unequip from armour slot back to grid
    public void UnequipToGrid(ArmourSlot slot)
    {
        InventoryItem item = Equipment.Unequip(slot);
        if (item == null) return;

        if (!Grid.FindFreeSlot(item, out int x, out int y) || !Grid.TryPlace(item, x, y))
            Debug.LogWarning($"No room to unequip {item.data.itemName}!");

        InventoryUI.Instance?.RefreshAll();
    }
}