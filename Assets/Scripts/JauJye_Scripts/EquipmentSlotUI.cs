using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("Slot Config")]
    public ArmourSlot armourSlot;

    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private GameObject _emptyOverlay; // shown when empty

    [Header("Colors")]
    [SerializeField] private Color _emptyColor = new Color(0.15f, 0.12f, 0.08f, 0.85f);
    [SerializeField] private Color _filledColor = new Color(0.25f, 0.20f, 0.12f, 1f);

    private InventoryItem _currentItem;

    private void Start()
    {
        SetItem(null);
    }

    public void SetItem(InventoryItem item)
    {
        _currentItem = item;

        bool hasItem = item != null;

        if (_backgroundImage)
            _backgroundImage.color = hasItem ? _filledColor : _emptyColor;

        if (_iconImage)
        {
            _iconImage.enabled = hasItem;
            if (hasItem && item.data.icon)
                _iconImage.sprite = item.data.icon;
        }

        if (_label)
            _label.text = hasItem ? item.data.itemName : armourSlot.ToString();

        if (_emptyOverlay)
            _emptyOverlay.SetActive(!hasItem);
    }

    // Called when clicking an equipped item to unequip it
    public void OnClick()
    {
        if (_currentItem == null) return;
        InventoryManager.Instance.UnequipToGrid(armourSlot);
    }

    public InventoryItem GetItem() => _currentItem;
}