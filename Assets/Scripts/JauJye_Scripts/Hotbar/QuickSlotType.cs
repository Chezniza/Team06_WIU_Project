using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum QuickSlotType { Weapon, Potion }

public class QuickSlotUI : MonoBehaviour
{
    [Header("Slot Config")]
    public QuickSlotType slotType;
    public int weaponSlotIndex; // 0 or 1, only used when slotType == Weapon

    [Header("UI References - drag children here")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _activeBorder;
    [SerializeField] private TextMeshProUGUI _keyLabel;
    [SerializeField] private TextMeshProUGUI _itemName;

    [Header("Colors")]
    [SerializeField] private Color _emptyColor = new Color(0.10f, 0.08f, 0.05f, 0.90f);
    [SerializeField] private Color _filledColor = new Color(0.20f, 0.15f, 0.08f, 1.00f);
    [SerializeField] private Color _activeColor = new Color(0.80f, 0.55f, 0.10f, 1.00f);
    [SerializeField] private Color _potionColor = new Color(0.55f, 0.10f, 0.10f, 1.00f);

    private void Start()
    {
        if (_keyLabel)
            _keyLabel.text = slotType == QuickSlotType.Weapon
                ? (weaponSlotIndex + 1).ToString()
                : "Q";

        SetItem(null);
        SetActive(false);
    }

    public void SetItem(InventoryItem item)
    {
        bool hasItem = item != null;

        if (_background)
            _background.color = hasItem
                ? (slotType == QuickSlotType.Potion ? _potionColor : _filledColor)
                : _emptyColor;

        if (_itemIcon)
        {
            // Always keep enabled, use alpha to show/hide
            _itemIcon.enabled = true;
            if (hasItem && item.data.icon != null)
            {
                _itemIcon.sprite = item.data.icon;
                _itemIcon.color = Color.white; // fully visible
            }
            else
            {
                _itemIcon.sprite = null;
                _itemIcon.color = Color.clear; // invisible when empty
            }
        }

        if (_itemName)
            _itemName.text = hasItem ? item.data.itemName : string.Empty;
    }

    public void SetActive(bool active)
    {
        if (_activeBorder)
            _activeBorder.color = active ? _activeColor : Color.clear;
    }
}