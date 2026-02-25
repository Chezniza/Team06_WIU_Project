using UnityEngine;
using UnityEngine.UI;

public class ShortcutWheelUI : MonoBehaviour
{
    [Header("Wheel Root")]
    [SerializeField] private GameObject _wheelPanel;

    [Header("Prefab")]
    [SerializeField] private GameObject _slicePrefab;

    [Header("Wheel Settings")]
    [SerializeField] private float _iconRadius = 130f;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = new Color(0.10f, 0.14f, 0.20f, 0.85f);
    [SerializeField] private Color _selectedColor = new Color(0.80f, 0.55f, 0.10f, 0.95f);

    private SliceUI[] _slices;

    private class SliceUI
    {
        public GameObject root;
        public Image background;
        public Image icon;
    }

    private void Start()
    {
        _wheelPanel.SetActive(false);
    }

    public void Show(ShortcutWheelItem[] items)
    {
        _wheelPanel.SetActive(true);

        // Clear old slices
        if (_slices != null)
            foreach (var s in _slices)
                if (s.root) Destroy(s.root);

        _slices = new SliceUI[items.Length];

        float segmentAngle = 360f / items.Length;

        for (int i = 0; i < items.Length; i++)
        {
            float angleDeg = 90f - i * segmentAngle;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            var sliceGO = Instantiate(_slicePrefab, _wheelPanel.transform);
            var rt = sliceGO.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(
                Mathf.Cos(angleRad) * _iconRadius,
                Mathf.Sin(angleRad) * _iconRadius
            );

            var slice = new SliceUI();
            slice.root = sliceGO;
            slice.background = sliceGO.GetComponent<Image>();
            slice.icon = sliceGO.transform.Find("Icon")?.GetComponent<Image>();

            if (slice.icon && items[i].icon)
                slice.icon.sprite = items[i].icon;

            if (slice.background)
                slice.background.color = _normalColor;

            _slices[i] = slice;
        }

        UpdateSelection(-1);
    }

    public void Hide()
    {
        _wheelPanel.SetActive(false);
    }

    public void UpdateSelection(int selectedIndex)
    {
        if (_slices == null) return;

        for (int i = 0; i < _slices.Length; i++)
        {
            if (_slices[i]?.background == null) continue;

            bool isSelected = i == selectedIndex;
            _slices[i].background.color = isSelected ? _selectedColor : _normalColor;
            _slices[i].root.transform.localScale = Vector3.one * (isSelected ? 1.15f : 1f);
        }
    }
}