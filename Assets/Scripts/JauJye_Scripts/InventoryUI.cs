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

    [Header("Equipment Slots UI")]
    [SerializeField] private EquipmentSlotUI slotHead;
    [SerializeField] private EquipmentSlotUI slotTorso;
    [SerializeField] private EquipmentSlotUI slotPants;
    [SerializeField] private EquipmentSlotUI slotShoes;
    [SerializeField] private EquipmentSlotUI slotGauntlets;

    [Header("Tooltip")]
    [SerializeField] private GameObject _tooltip;
    [SerializeField] private TextMeshProUGUI _tooltipName;
    [SerializeField] private TextMeshProUGUI _tooltipDesc;
    [SerializeField] private TextMeshProUGUI _tooltipStats;

    [Header("Input")]
    [SerializeField] private PlayerInput _playerInput;

    // Drag state
    private InventoryItem _dragItem;
    private GameObject _dragVisual;
    private bool _isDragging;

    // Track spawned item tiles
    private Dictionary<InventoryItem, GameObject> _itemTiles = new Dictionary<InventoryItem, GameObject>();

    // Grid cell backgrounds
    private List<GameObject> _cellObjects = new List<GameObject>();

    private InputAction _toggleAction;
    private InputAction _rotateAction;
    private InputAction _clickAction;

    public bool IsInventoryOpen() => _inventoryPanel.activeSelf;

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

        _inventoryPanel.SetActive(false);
        if (_tooltip) _tooltip.SetActive(false);

        BuildGridBackground();
        RefreshAll();
    }

    private void Update()
    {
        if (_toggleAction.WasPressedThisFrame())
            ToggleInventory();

        if (!_inventoryPanel.activeSelf) return;

        if (_rotateAction.WasPressedThisFrame() && _isDragging)
            RotateDragItem();

        if (_isDragging)
            UpdateDragVisual();

        if (_clickAction.WasPressedThisFrame())
            OnClick();

        if (_clickAction.WasReleasedThisFrame())
            OnRelease();
    }

    private void ToggleInventory()
    {
        bool open = !_inventoryPanel.activeSelf;
        _inventoryPanel.SetActive(open);
        if (open) RefreshAll();
        else CancelDrag();
    }

    // ?? Grid Background ??????????????????????????????????????????????
    private void BuildGridBackground()
    {
        foreach (var obj in _cellObjects) Destroy(obj);
        _cellObjects.Clear();

        var grid = InventoryManager.Instance.Grid;
        _gridContainer.sizeDelta = new Vector2(grid.width * cellSize, grid.height * cellSize);

        for (int y = 0; y < grid.height; y++)
        {
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
    }

    // ?? Refresh ???????????????????????????????????????????????????????
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

    private void SpawnItemTile(InventoryItem item)
    {
        bool[,] shape = item.GetCurrentShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        // Root holder - no image, just an anchor point on the grid
        var tileRoot = new GameObject(item.data.itemName);
        tileRoot.transform.SetParent(_gridContainer, false);
        var rootRT = tileRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = Vector2.up;
        rootRT.anchoredPosition = new Vector2(item.gridX * cellSize, -item.gridY * cellSize);
        rootRT.sizeDelta = new Vector2(cols * cellSize, rows * cellSize);

        // Spawn one image per TRUE cell only — false cells are skipped leaving shape gaps
        bool foundFirst = false;
        for (int r = 0; r < rows; r++)
        {
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

                // Icon and label only on the first filled cell
                bool isFirst = !foundFirst;
                foundFirst = true;

                var iconImg = cell.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImg)
                {
                    iconImg.enabled = isFirst;
                    if (isFirst && item.data.icon) iconImg.sprite = item.data.icon;
                }

                var label = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (label)
                {
                    label.enabled = isFirst;
                    if (isFirst) label.text = item.data.itemName;
                }
            }
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
        {
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
                if (iconImg)
                {
                    iconImg.enabled = isFirst;
                    iconImg.raycastTarget = false;
                    if (isFirst && item.data.icon) iconImg.sprite = item.data.icon;
                }

                var label = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (label)
                {
                    label.enabled = isFirst;
                    if (isFirst) label.text = item.data.itemName;
                }
            }
        }

        return dragRoot;
    }

    public void RefreshEquipmentSlots()
    {
        var eq = InventoryManager.Instance.Equipment;
        slotHead?.SetItem(eq.GetEquipped(ArmourSlot.Head));
        slotTorso?.SetItem(eq.GetEquipped(ArmourSlot.Torso));
        slotPants?.SetItem(eq.GetEquipped(ArmourSlot.Pants));
        slotShoes?.SetItem(eq.GetEquipped(ArmourSlot.Shoes));
        slotGauntlets?.SetItem(eq.GetEquipped(ArmourSlot.Gauntlets));
    }

    // ?? Drag & Drop ???????????????????????????????????????????????????
    private void OnClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        foreach (var kv in _itemTiles)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                kv.Value.GetComponent<RectTransform>(), mousePos))
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

        InventoryManager.Instance.Grid.Remove(item);

        if (_itemTiles.TryGetValue(item, out var oldTile)) { Destroy(oldTile); _itemTiles.Remove(item); }

        _dragVisual = BuildDragVisual(item, _inventoryPanel.transform);

        RefreshGrid();
        ShowTooltip(item);
    }

    private void UpdateDragVisual()
    {
        if (_dragVisual == null) return;
        _dragVisual.GetComponent<RectTransform>().position = Mouse.current.position.ReadValue();
        HighlightPlacement(Mouse.current.position.ReadValue());
    }

    private void RotateDragItem()
    {
        if (_dragItem == null || _dragVisual == null) return;
        _dragItem.Rotate();
        Destroy(_dragVisual);
        _dragVisual = BuildDragVisual(_dragItem, _inventoryPanel.transform);
    }

    private void OnRelease()
    {
        if (!_isDragging || _dragItem == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (TryDropOnEquipmentSlot(mousePos)) { FinaliseDrop(); return; }

        if (TryGetGridCell(mousePos, out int gx, out int gy))
            if (InventoryManager.Instance.Grid.TryPlace(_dragItem, gx, gy)) { FinaliseDrop(); return; }

        // Return to original position
        if (InventoryManager.Instance.Grid.TryPlace(_dragItem, _dragItem.gridX, _dragItem.gridY))
            FinaliseDrop();
        else
            CancelDrag();
    }

    private bool TryDropOnEquipmentSlot(Vector2 mousePos)
    {
        if (_dragItem.data.itemType != ItemType.Armour) return false;

        EquipmentSlotUI[] slots = { slotHead, slotTorso, slotPants, slotShoes, slotGauntlets };
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            if (slot.armourSlot != _dragItem.data.armourSlot) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(slot.GetComponent<RectTransform>(), mousePos))
            {
                InventoryManager.Instance.EquipFromGrid(_dragItem);
                return true;
            }
        }
        return false;
    }

    private bool TryGetGridCell(Vector2 mousePos, out int gx, out int gy)
    {
        gx = gy = -1;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridContainer, mousePos, null, out Vector2 local)) return false;

        gx = Mathf.FloorToInt(local.x / cellSize);
        gy = Mathf.FloorToInt(-local.y / cellSize);

        var grid = InventoryManager.Instance.Grid;
        return gx >= 0 && gx < grid.width && gy >= 0 && gy < grid.height;
    }

    private void HighlightPlacement(Vector2 mousePos)
    {
        bool valid = TryGetGridCell(mousePos, out int gx, out int gy) &&
                     InventoryManager.Instance.Grid.CanPlace(_dragItem, gx, gy);

        if (_dragVisual)
        {
            foreach (var img in _dragVisual.GetComponentsInChildren<Image>())
                img.color = valid ? new Color(0.4f, 1f, 0.4f, 0.85f) : new Color(1f, 0.3f, 0.3f, 0.85f);
        }
    }

    private void FinaliseDrop()
    {
        Destroy(_dragVisual);
        _dragVisual = null;
        _dragItem = null;
        _isDragging = false;
        HideTooltip();
        RefreshAll();
    }

    private void CancelDrag()
    {
        if (_dragItem != null)
            InventoryManager.Instance.Grid.TryPlace(_dragItem, _dragItem.gridX, _dragItem.gridY);
        Destroy(_dragVisual);
        _dragVisual = null;
        _dragItem = null;
        _isDragging = false;
        RefreshAll();
    }

    // ?? Tooltip ???????????????????????????????????????????????????????
    private void ShowTooltip(InventoryItem item)
    {
        if (_tooltip == null) return;
        _tooltip.SetActive(true);
        if (_tooltipName) _tooltipName.text = $"<color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor(item.data.rarity))}>{item.data.itemName}</color>";
        if (_tooltipDesc) _tooltipDesc.text = item.data.description;
        if (_tooltipStats) _tooltipStats.text = item.data.itemType switch
        {
            ItemType.Weapon => $"DMG: {item.data.statValue}  WT: {item.data.weight}",
            ItemType.Armour => $"DEF: {item.data.statValue}  WT: {item.data.weight}  Slot: {item.data.armourSlot}",
            ItemType.Consumable => $"Heals: {item.data.healAmount}  WT: {item.data.weight}",
            _ => $"WT: {item.data.weight}"
        };
    }

    private void HideTooltip()
    {
        if (_tooltip) _tooltip.SetActive(false);
    }

    // ?? Helpers ???????????????????????????????????????????????????????
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