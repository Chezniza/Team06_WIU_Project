using UnityEngine;
using System.Collections.Generic;

public class QuestHandler : MonoBehaviour
{
    private List<Quest> activeQuests = new();

    private Quest CreateQuest(QuestData data)
    {
        Quest quest = null;
        switch (data.questType)
        {
            case QuestType.Kill:
                quest = new KillQuest(data);
                break;
            case QuestType.Fetch:
                quest = new FetchQuest(data);
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