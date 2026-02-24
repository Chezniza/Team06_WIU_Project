using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Config")]
    public ItemType armourType; // Set to Helmet, Chestplate, Pants, Boots, or Gauntlets

    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private GameObject _emptyOverlay; // shown when slot is empty

    [Header("Colors")]
    [SerializeField] private Color _emptyColor = new Color(0.15f, 0.12f, 0.08f, 0.85f);
    [SerializeField] private Color _filledColor = new Color(0.25f, 0.20f, 0.12f, 1.00f);

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
            // Always keep the Image component enabled
            // just swap the sprite and alpha so it shows/hides cleanly
            _iconImage.enabled = true;
            if (hasItem && item.data.icon != null)
            {
                _iconImage.sprite = item.data.icon;
                _iconImage.color = Color.white; // fully visible
            }
            else
            {
                _iconImage.sprite = null;
                _iconImage.color = Color.clear; // invisible when empty
            }
        }

        if (_label)
            _label.text = hasItem ? item.data.itemName : armourType.ToString();

        if (_emptyOverlay)
            _emptyOverlay.SetActive(!hasItem);
    }

    // ?? Hover ?????????????????????????????????????????????????????????
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentItem == null) return;
        InventoryUI.Instance?.ShowEquipmentSlotTooltip(_currentItem, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryUI.Instance?.HideEquipmentSlotTooltip();
    }

    // ?? Click to unequip ??????????????????????????????????????????????
    public void OnClick()
    {
        if (_currentItem == null) return;
        InventoryManager.Instance.UnequipArmourToGrid(armourType);
    }

    public InventoryItem GetItem() => _currentItem;
}