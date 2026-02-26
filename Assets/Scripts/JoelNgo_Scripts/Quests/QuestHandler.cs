using UnityEngine;
using System.Collections.Generic;

public class QuestHandler : MonoBehaviour
{
    private List<Quest> activeQuests = new();

    [Header("References")]
    [SerializeField] private InventoryManager _inventoryManager;

    private Quest CreateQuest(QuestData data)
    {
        Quest quest = null;
        switch (data.questType)
        {
            case QuestType.Kill:
                quest = new KillQuest(data, this);
                break;
            case QuestType.Fetch:
                quest = new FetchQuest(data, this);
                break;
        }
        quest?.Initialize();
        return quest;
    }

    public void AddQuest(QuestData data)
    {
        Quest quest = CreateQuest(data);
        if (quest != null)
        {
            activeQuests.Add(quest);
            QuestLogUI.Instance?.TrackQuest(quest);
            Debug.Log("Quest Added: " + data.title);
        }
    }

    // Called by KillQuest/FetchQuest on completion
    public void OnQuestCompleted(Quest quest)
    {
        if (!activeQuests.Contains(quest)) return;
        activeQuests.Remove(quest);

        // Give item rewards
        GiveItemRewards(quest.Data);

        QuestLogUI.Instance?.MarkCompleted(quest);

        Debug.Log($"Quest Completed: {quest.Data.title}");
    }

    private void GiveItemRewards(QuestData data)
    {
        if (data.itemRewards == null || data.itemRewards.Length == 0) return;
        if (_inventoryManager == null)
        {
            Debug.LogWarning("[QuestHandler] InventoryManager not assigned — item rewards not given.");
            return;
        }

        foreach (var reward in data.itemRewards)
        {
            if (reward.item == null) continue;

            for (int i = 0; i < reward.quantity; i++)
            {
                var newItem = new InventoryItem(reward.item);

                if (_inventoryManager.Grid.FindFreeSlot(newItem, out int x, out int y))
                {
                    _inventoryManager.Grid.TryPlace(newItem, x, y);
                    Debug.Log($"[QuestHandler] Given item: {reward.item.itemName}");
                }
                else
                {
                    Debug.LogWarning($"[QuestHandler] Inventory full — could not give: {reward.item.itemName}");
                }
            }
        }

        // Refresh inventory UI if open
        InventoryUI.Instance?.RefreshGrid();
    }

    public void NotifyEnemyKilled(string enemyID)
    {
        foreach (var quest in activeQuests)
            quest.OnEnemyKilled(enemyID);
    }

    public void NotifyItemCollected(string itemID)
    {
        foreach (var quest in activeQuests)
            quest.OnItemCollected(itemID);
    }
}