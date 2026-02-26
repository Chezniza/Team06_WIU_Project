using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Left — Quest List")]
    [SerializeField] private Transform _activeContainer;
    [SerializeField] private Transform _completedContainer;
    [SerializeField] private GameObject _questEntryPrefab;
    [SerializeField] private GameObject _activeHeader;
    [SerializeField] private GameObject _completedHeader;

    [Header("Right — Detail View")]
    [SerializeField] private Image _detailIcon;
    [SerializeField] private TextMeshProUGUI _detailTitle;
    [SerializeField] private TextMeshProUGUI _detailType;
    [SerializeField] private TextMeshProUGUI _detailDescription;
    [SerializeField] private TextMeshProUGUI _detailProgress;
    [SerializeField] private TextMeshProUGUI _detailReward;
    [SerializeField] private GameObject _detailEmptyState;

    [Header("Input Handler")]
    [SerializeField] private InputHandler _inputHandler;

    [Header("UI to hide when journal is open")]
    [SerializeField] private GameObject _staminaBar;
    [SerializeField] private GameObject _hotbar;

    private List<Quest> _activeQuests = new();
    private List<Quest> _completedQuests = new();
    private List<GameObject> _spawnedEntries = new();
    private Quest _selectedQuest;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _panel.SetActive(false);
        ShowEmptyDetail();
    }

    private void Update()
    {
        if (_panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    // ?? Public API ????????????????????????????????????????????????????
    public void TrackQuest(Quest quest)
    {
        if (_activeQuests.Contains(quest)) return;
        _activeQuests.Add(quest);
        if (_panel.activeSelf) Refresh();
    }

    public void MarkCompleted(Quest quest)
    {
        _activeQuests.Remove(quest);
        if (!_completedQuests.Contains(quest))
            _completedQuests.Add(quest);

        if (_selectedQuest == quest && _detailProgress)
            _detailProgress.text = "?  COMPLETED";

        if (_panel.activeSelf) Refresh();
    }

    public void Refresh()
    {
        if (!_panel.activeSelf) return;

        foreach (var e in _spawnedEntries) Destroy(e);
        _spawnedEntries.Clear();

        if (_activeHeader) _activeHeader.SetActive(_activeQuests.Count > 0);
        if (_completedHeader) _completedHeader.SetActive(_completedQuests.Count > 0);

        foreach (var quest in _activeQuests)
            SpawnEntry(quest, _activeContainer, false);

        foreach (var quest in _completedQuests)
            SpawnEntry(quest, _completedContainer, true);

        if (_selectedQuest != null)
            SelectQuest(_selectedQuest);
    }

    // ?? Open / Close ??????????????????????????????????????????????????
    public void Toggle()
    {
        if (_panel.activeSelf) Hide();
        else Show();
    }

    public void Show()
    {
        _panel.SetActive(true);
        _inputHandler?.LockControls();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Hide other HUD elements
        if (_staminaBar) _staminaBar.SetActive(false);
        if (_hotbar) _hotbar.SetActive(false);

        Refresh();
    }

    public void Hide()
    {
        _panel.SetActive(false);
        _inputHandler?.UnlockControls();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Restore other HUD elements
        if (_staminaBar) _staminaBar.SetActive(true);
        if (_hotbar) _hotbar.SetActive(true);
    }

    public bool IsOpen() => _panel.activeSelf;

    // ?? Entry spawning ????????????????????????????????????????????????
    private void SpawnEntry(Quest quest, Transform container, bool completed)
    {
        var entry = Instantiate(_questEntryPrefab, container);
        _spawnedEntries.Add(entry);

        // FIX: use GetComponentInChildren to find TMP regardless of child name
        var allTMP = entry.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var tmp in allTMP)
        {
            // Match by GameObject name — set these names on your prefab children
            if (tmp.gameObject.name == "QuestName") tmp.text = quest.Data.title;
            if (tmp.gameObject.name == "TypeTag") tmp.text = quest.Data.questType.ToString().ToUpper();
        }



        var tick = entry.transform.Find("CompletedTick");
        if (tick) tick.gameObject.SetActive(completed);

        var btn = entry.GetComponent<Button>();
        if (btn) btn.onClick.AddListener(() => SelectQuest(quest));

        var bg = entry.GetComponent<Image>();
        if (bg && _selectedQuest == quest)
            bg.color = new Color(0.55f, 0.40f, 0.15f, 0.60f);
    }

    // ?? Detail panel ??????????????????????????????????????????????????
    private void SelectQuest(Quest quest)
    {
        _selectedQuest = quest;

        if (_detailEmptyState) _detailEmptyState.SetActive(false);

        // Show/hide icon using alpha so layout isn't affected
        if (_detailIcon)
        {
            if (quest.Data.icon != null)
            {
                _detailIcon.sprite = quest.Data.icon;
                _detailIcon.color = Color.white;
            }
            else
            {
                _detailIcon.color = Color.clear;
            }
        }

        if (_detailTitle) _detailTitle.text = quest.Data.title;
        if (_detailType) _detailType.text = quest.Data.questType.ToString().ToUpper();
        if (_detailDescription) _detailDescription.text = quest.Data.description;
        if (_detailProgress) _detailProgress.text = quest.IsCompleted
                                                            ? "?  COMPLETED"
                                                            : quest.GetProgressText();
        if (_detailReward)
            _detailReward.text = $"{quest.Data.rewardGold}  Gold      {quest.Data.rewardXP}  XP";

        Refresh();
    }

    private void ShowEmptyDetail()
    {
        if (_detailEmptyState) _detailEmptyState.SetActive(true);
        if (_detailIcon) _detailIcon.color = Color.clear;
        if (_detailTitle) _detailTitle.text = string.Empty;
        if (_detailType) _detailType.text = string.Empty;
        if (_detailDescription) _detailDescription.text = string.Empty;
        if (_detailProgress) _detailProgress.text = string.Empty;
        if (_detailReward) _detailReward.text = string.Empty;
    }
}