using UnityEngine;

public class QuickSlots
{
    public const int WeaponSlotCount = 2;

    private InventoryItem[] _weaponSlots = new InventoryItem[WeaponSlotCount];
    private InventoryItem _potionSlot;
    private int _activeWeaponSlot = 0;

    public int ActiveWeaponSlot => _activeWeaponSlot;

    public void SetActiveWeaponSlot(int index)
    {
        if (index >= 0 && index < WeaponSlotCount)
            _activeWeaponSlot = index;
    }

    // ?? Weapons ???????????????????????????????????????????????????????
    public InventoryItem EquipWeaponToActive(InventoryItem item)
    {
        InventoryItem displaced = _weaponSlots[_activeWeaponSlot];
        _weaponSlots[_activeWeaponSlot] = item;
        return displaced;
    }

    public InventoryItem GetWeaponSlot(int index)
    {
        if (index < 0 || index >= WeaponSlotCount) return null;
        return _weaponSlots[index];
    }

    public InventoryItem GetActiveWeapon() => _weaponSlots[_activeWeaponSlot];

    // ?? Potion ????????????????????????????????????????????????????????
    public InventoryItem EquipPotion(InventoryItem item)
    {
        InventoryItem displaced = _potionSlot;
        _potionSlot = item;
        return displaced;
    }

    public InventoryItem GetPotion() => _potionSlot;

    public void ClearPotion() => _potionSlot = null;
}