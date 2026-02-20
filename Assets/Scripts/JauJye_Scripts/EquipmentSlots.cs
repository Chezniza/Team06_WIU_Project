using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlots
{
    private Dictionary<ArmourSlot, InventoryItem> equipped = new Dictionary<ArmourSlot, InventoryItem>();

    public EquipmentSlots()
    {
        foreach (ArmourSlot slot in System.Enum.GetValues(typeof(ArmourSlot)))
            if (slot != ArmourSlot.None)
                equipped[slot] = null;
    }

    // Equip item into its slot. Returns any previously equipped item (to put back in inventory).
    public InventoryItem Equip(InventoryItem item)
    {
        if (item.data.itemType != ItemType.Armour) return item; // can't equip non-armour here
        ArmourSlot slot = item.data.armourSlot;
        if (slot == ArmourSlot.None) return item;

        InventoryItem previous = equipped[slot];
        equipped[slot] = item;
        return previous; // caller must put this back in the grid
    }

    public InventoryItem Unequip(ArmourSlot slot)
    {
        if (!equipped.ContainsKey(slot)) return null;
        InventoryItem item = equipped[slot];
        equipped[slot] = null;
        return item;
    }

    public InventoryItem GetEquipped(ArmourSlot slot)
    {
        if (!equipped.ContainsKey(slot)) return null;
        return equipped[slot];
    }

    public bool IsSlotEmpty(ArmourSlot slot) => equipped.ContainsKey(slot) && equipped[slot] == null;
}