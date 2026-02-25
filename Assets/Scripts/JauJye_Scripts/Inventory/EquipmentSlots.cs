using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlots
{
    // Keyed by ItemType — only armour types are valid keys here
    private Dictionary<ItemType, InventoryItem> _equipped = new Dictionary<ItemType, InventoryItem>()
    {
        { ItemType.Helmet,     null },
        { ItemType.Chestplate, null },
        { ItemType.Pants,      null },
        { ItemType.Boots,      null },
        { ItemType.Gauntlets,  null },
    };

    // Equip item, returns displaced item or null
    public InventoryItem Equip(InventoryItem item)
    {
        if (!item.data.IsArmour()) return item;
        if (!_equipped.ContainsKey(item.data.itemType)) return item;

        InventoryItem displaced = _equipped[item.data.itemType];
        _equipped[item.data.itemType] = item;
        return displaced;
    }

    public InventoryItem Unequip(ItemType type)
    {
        if (!_equipped.ContainsKey(type)) return null;
        InventoryItem item = _equipped[type];
        _equipped[type] = null;
        return item;
    }

    public InventoryItem GetEquipped(ItemType type)
    {
        if (!_equipped.ContainsKey(type)) return null;
        return _equipped[type];
    }

    public bool IsSlotEmpty(ItemType type) => _equipped.ContainsKey(type) && _equipped[type] == null;
}