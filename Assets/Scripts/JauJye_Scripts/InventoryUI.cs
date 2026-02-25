using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject _inventoryPanel;

    [Header("Grid UI")]
    [SerializeField] private RectTransform _gridContainer;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private GameObject _itemTilePrefab;
    public float cellSize = 64f;

    [Header("Canvas")]
    [SerializeField] private Canvas _canvas;

    [Header("Input Handler - for control locking")]
    [SerializeField] private InputHandler _inputHandler;

    [Header("Equipment Slots (body map)")]
    [SerializeField] private EquipmentSlotUI slotHelmet;
    [SerializeField] private EquipmentSlotUI slotChestplate;
    [SerializeField] private EquipmentSlotUI slotPants;
    [SerializeField] private EquipmentSlotUI slotBoots;
    [SerializeField] private EquipmentSlotUI slotGauntlets;

    [Header("Hover Tooltip")]
    [SerializeField] private GameObject _hoverTooltip;
    [SerializeField] private Image _hoverIcon;
    [SerializeField] private TextMeshProUGUI _hoverName;
    [SerializeField] private TextMeshProUGUI _hoverType;
    [SerializeField] private TextMeshProUGUI _hoverDesc;
    [SerializeField] private RectTransform _hoverTooltipRT;
    [SerializeField] private Vector2 _hoverOffset = new Vector2(12f, -12f);

    [Header("Input")]
    [SerializeField] private PlayerInput _playerInput;

    // Drag state
    private InventoryItem _dragItem;
    private GameObject _dragVisual;
    private bool _isDragging;
    private int _dragOffsetX;
    private int _dragOffsetY;

    // Hover state
    private InventoryItem _hoveredItem;

    // Tiles
    private Dictionary<InventoryItem, GameObject> _itemTiles = new Dictionary<InventoryItem, GameObject>();
    private List<GameObject> _cellObjects = new List<GameObject>();

    // Input actions
    private InputAction _toggleAction;
    private InputAction _rotateAction;
    private InputAction _clickAction;
    private InputAction _rightClickAction;

    private Camera _canvasCamera => _canvas.renderMode == RenderMode.ScreenSpaceOverlay
        ? null : _canvas.worldCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _toggleAction = _playerInput.actions["Inventory"];
        _rotateAction = _playerInput.actions["RotateItem"];
        _clickAction = _playerInput.actions["Click"];
        _rightClickAction = _playerInput.actions["RightClick"];

        _inventoryPanel.SetActive(false);
        if (_hoverTooltip) _hoverTooltip.SetActive(false);

        BuildGridBackground();
        RefreshAll();
    }

    private void Update()
    {
        if (_toggleAction.WasPressedThisFrame())
            ToggleInventory();

        if (!_inventoryPanel.activeSelf) return;

        // Consume R so it doesn't leak to scene reset
        if (_rotateAction.WasPressedThisFrame())
        {
            if (_isDragging) RotateDragItem();
            return;
        }

        if (_isDragging) UpdateDragVisual();

        if (_clickAction.WasPressedThisFrame()) OnLeftClick();
        if (_clickAction.WasReleasedThisFrame()) OnRelease();
        if (_rightClickAction.WasPressedThisFrame()) OnRightClick();

        UpdateHoverTooltip();
    }

    public bool IsInventoryOpen() => _inventoryPanel.activeSelf;

    private void ToggleInventory()
    {
        bool open = !_inventoryPanel.activeSelf;
        _inventoryPanel.SetActive(open);

        if (open)
        {
            _inputHandler?.LockControls();
            RefreshAll();
        }
        else
        {
            CancelDrag();
            HideHoverTooltip();
            _inputHandler?.UnlockControls();
        }
    }

    private void BuildGridBackground()
    {
        foreach (var obj in _cellObjects) Destroy(obj);
        _cellObjects.Clear();

        var grid = InventoryManager.Instance.Grid;
        _gridContainer.sizeDelta = new Vector2(grid.width * cellSize, grid.height * cellSize);

        for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
            {
                var cell = Instantiate(_cellPrefab, _gridContainer);
                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.up;
                rt.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                rt.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);
                _cellObjects.Add(cell);
            }
    }

    public void RefreshAll()
    {
        RefreshGrid();
        RefreshEquipmentSlots();
    }

    public void RefreshGrid()
    {
        foreach (var kv in _itemTiles) Destroy(kv.Value);
        _itemTiles.Clear();
        foreach (var item in InventoryManager.Instance.Grid.GetAllItems())
            SpawnItemTile(item);
    }

    public void RefreshEquipmentSlots()
    {
        var eq = InventoryManager.Instance.Equipment;
        slotHelmet?.SetItem(eq.GetEquipped(ItemType.Helmet));
        slotChestplate?.SetItem(eq.GetEquipped(ItemType.Chestplate));
        slotPants?.SetItem(eq.GetEquipped(ItemType.Pants));
        slotBoots?.SetItem(eq.GetEquipped(ItemType.Boots));
        slotGauntlets?.SetItem(eq.GetEquipped(ItemType.Gauntlets));
    }

    private void SpawnItemTile(InventoryItem item)
    {
        bool[,] shape = item.GetCurrentShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        var tileRoot = new GameObject(item.data.itemName);
        tileRoot.transform.SetParent(_gridContainer, false);
        var rootRT = tileRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = Vector2.up;
        rootRT.anchoredPosition = new Vector2(item.gridX * cellSize, -item.gridY * cellSize);
        rootRT.sizeDelta = new Vector2(cols * cellSize, rows * cellSize);

        bool foundFirst = false;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (!shape[r, c]) continue;

                var cell = Instantiate(_itemTilePrefab, tileRoot.transform);
                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.up;
                rt.anchoredPosition = new Vector2(c * cellSize, -r * cellSize);
                rt.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);

                var img = cell.GetComponent<Image>();
                if (img) img.color = GetRarityColor(item.data.rarity);

                bool isFirst = !foundFirst;
                foundFirst = true;

                var iconImg = cell.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImg) { iconImg.enabled = isFirst; if (isFirst && item.data.icon) iconImg.sprite = item.data.icon; }

                var label = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (label) { label.enabled = isFirst; if (isFirst) label.text = item.data.itemName; }
            }

        _itemTiles[item] = tileRoot;
    }

    private GameObject BuildDragVisual(InventoryItem item, Transform parent)
    {
        bool[,] shape = item.GetCurrentShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        var dragRoot = new GameObject("DragVisual");
        dragRoot.transform.SetParent(parent, false);
        var rootRT = dragRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = Vector2.up;
        rootRT.sizeDelta = new Vector2(cols * cellSize, rows * cellSize);

        bool foundFirst = false;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (!shape[r, c]) continue;

                var cell = Instantiate(_itemTilePrefab, dragRoot.transform);
                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.up;
                rt.anchoredPosition = new Vector2(c * cellSize, -r * cellSize);
                rt.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);

                bool isFirst = !foundFirst;
                foundFirst = true;

                var img = cell.GetComponent<Image>();
                if (img) { img.color = GetRarityColor(item.data.rarity); img.raycastTarget = false; }

                var iconImg = cell.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImg) { iconImg.enabled = isFirst; iconImg.raycastTarget = false; if (isFirst && item.data.icon) iconImg.sprite = item.data.icon; }

                var label = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (label) { label.enabled = isFirst; if (isFirst) label.text = item.data.itemName; }
            }

        return dragRoot;
    }

    private void OnLeftClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        foreach (var kv in _itemTiles)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                kv.Value.GetComponent<RectTransform>(), mousePos, _canvasCamera))
            {
                StartDrag(kv.Key, mousePos);
                return;
            }
        }
    }

    private void StartDrag(InventoryItem item, Vector2 mousePos)
    {
        _dragItem = item;
        _isDragging = true;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridContainer, mousePos, _canvasCamera, out Vector2 local))
        {
            _dragOffsetX = Mathf.FloorToInt(local.x / cellSize) - item.gridX;
            _dragOffsetY = Mathf.FloorToInt(-local.y / cellSize) - item.gridY;
        }
        else { _dragOffsetX = 0; _dragOffsetY = 0; }

        InventoryManager.Instance.Grid.Remove(item);
        if (_itemTiles.TryGetValue(item, out var oldTile)) { Destroy(oldTile); _itemTiles.Remove(item); }

        _dragVisual = BuildDragVisual(item, _inventoryPanel.transform);
        _dragVisual.GetComponent<RectTransform>().position = mousePos;

        HideHoverTooltip();
        RefreshGrid();
    }

    private void UpdateDragVisual()
    {
        if (_dragVisual == null) return;
        _dragVisual.GetComponent<RectTransform>().position = Mouse.current.position.ReadValue();
        HighlightPlacement(Mouse.current.position.ReadValue());
    }

    private void RotateDragItem()
    {
        if (_dragItem == null) return;
        _dragItem.Rotate();
        Vector2 pos = Mouse.current.position.ReadValue();
        Destroy(_dragVisual);
        _dragVisual = BuildDragVisual(_dragItem, _inventoryPanel.transform);
        _dragVisual.GetComponent<RectTransform>().position = pos;
    }

    private void OnRelease()
    {
        if (!_isDragging || _dragItem == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (TryDropOnEquipmentSlot(mousePos)) { FinaliseDrop(); return; }

        if (TryGetGridCell(mousePos, out int gx, out int gy))
        {
            int tx = gx - _dragOffsetX;
            int ty = gy - _dragOffsetY;
            if (InventoryManager.Instance.Grid.TryPlace(_dragItem, tx, ty))
            {
                FinaliseDrop();
                return;
            }
        }

        ReturnToOriginal();
    }

    private void OnRightClick()
    {
        if (_isDragging) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        foreach (var kv in _itemTiles)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                kv.Value.GetComponent<RectTransform>(), mousePos, _canvasCamera))
            {
                InventoryManager.Instance.QuickEquip(kv.Key);
                HideHoverTooltip();
                return;
            }
        }
    }

    private void UpdateHoverTooltip()
    {
        if (_isDragging) { HideHoverTooltip(); return; }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        InventoryItem found = null;

        foreach (var kv in _itemTiles)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                kv.Value.GetComponent<RectTransform>(), mousePos, _canvasCamera))
            {
                found = kv.Key;
                break;
            }
        }

        if (found == null) { HideHoverTooltip(); _hoveredItem = null; return; }

        if (found != _hoveredItem)
        {
            _hoveredItem = found;
            ShowHoverTooltip(found, mousePos);
        }
        else if (_hoverTooltipRT)
            _hoverTooltipRT.position = mousePos + _hoverOffset;
    }

    public void ShowEquipmentSlotTooltip(InventoryItem item, Vector2 mousePos)
        => ShowHoverTooltip(item, mousePos);

    public void HideEquipmentSlotTooltip()
        => HideHoverTooltip();

    private void ShowHoverTooltip(InventoryItem item, Vector2 mousePos)
    {
        if (_hoverTooltip == null) return;
        _hoverTooltip.SetActive(true);

        if (_hoverIcon)
        {
            _hoverIcon.enabled = item.data.icon != null;
            if (item.data.icon) _hoverIcon.sprite = item.data.icon;
        }

        if (_hoverName)
            _hoverName.text = $"<color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor(item.data.rarity))}>" +
                              $"{item.data.itemName}</color>";

        if (_hoverType)
            _hoverType.text = item.data.itemType.ToString();

        if (_hoverDesc)
            _hoverDesc.text = item.data.description;

        if (_hoverTooltipRT)
            _hoverTooltipRT.position = mousePos + _hoverOffset;
    }

    private void HideHoverTooltip()
    {
        _hoveredItem = null;
        if (_hoverTooltip) _hoverTooltip.SetActive(false);
    }

    private bool TryDropOnEquipmentSlot(Vector2 mousePos)
    {
        if (!_dragItem.data.IsArmour()) return false;

        EquipmentSlotUI[] slots = { slotHelmet, slotChestplate, slotPants, slotBoots, slotGauntlets };
        foreach (var slot in slots)
        {
            if (slot == null || slot.armourType != _dragItem.data.itemType) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(
                slot.GetComponent<RectTransform>(), mousePos, _canvasCamera))
            {
                InventoryManager.Instance.EquipArmourFromGrid(_dragItem);
                return true;
            }
        }
        return false;
    }

    private bool TryGetGridCell(Vector2 mousePos, out int gx, out int gy)
    {
        gx = gy = -1;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridContainer, mousePos, _canvasCamera, out Vector2 local)) return false;

        gx = Mathf.FloorToInt(local.x / cellSize);
        gy = Mathf.FloorToInt(-local.y / cellSize);

        var grid = InventoryManager.Instance.Grid;
        return gx >= 0 && gx < grid.width && gy >= 0 && gy < grid.height;
    }

    private void HighlightPlacement(Vector2 mousePos)
    {
        if (_dragItem == null || _dragVisual == null) return;
        bool valid = TryGetGridCell(mousePos, out int gx, out int gy) &&
                     InventoryManager.Instance.Grid.CanPlace(_dragItem, gx - _dragOffsetX, gy - _dragOffsetY);

        foreach (var img in _dragVisual.GetComponentsInChildren<Image>())
            img.color = valid ? new Color(0.4f, 1f, 0.4f, 0.85f) : new Color(1f, 0.3f, 0.3f, 0.85f);
    }

    private void FinaliseDrop()
    {
        Destroy(_dragVisual);
        _dragVisual = null;
        _dragItem = null;
        _isDragging = false;
        RefreshAll();
    }

    private void ReturnToOriginal()
    {
        if (!InventoryManager.Instance.Grid.TryPlace(_dragItem, _dragItem.gridX, _dragItem.gridY))
            if (InventoryManager.Instance.Grid.FindFreeSlot(_dragItem, out int x, out int y))
                InventoryManager.Instance.Grid.TryPlace(_dragItem, x, y);

        Destroy(_dragVisual);
        _dragVisual = null;
        _dragItem = null;
        _isDragging = false;
        RefreshAll();
    }

    private void CancelDrag()
    {
        if (_dragItem != null)
        {
            if (!InventoryManager.Instance.Grid.TryPlace(_dragItem, _dragItem.gridX, _dragItem.gridY))
                if (InventoryManager.Instance.Grid.FindFreeSlot(_dragItem, out int x, out int y))
                    InventoryManager.Instance.Grid.TryPlace(_dragItem, x, y);
        }
        Destroy(_dragVisual);
        _dragVisual = null;
        _dragItem = null;
        _isDragging = false;
        RefreshAll();
    }

    private Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => new Color(0.75f, 0.72f, 0.65f),
        ItemRarity.Uncommon => new Color(0.27f, 0.80f, 0.37f),
        ItemRarity.Rare => new Color(0.25f, 0.55f, 1.00f),
        ItemRarity.Epic => new Color(0.70f, 0.30f, 1.00f),
        ItemRarity.Legendary => new Color(1.00f, 0.65f, 0.10f),
        _ => Color.white
    };
}