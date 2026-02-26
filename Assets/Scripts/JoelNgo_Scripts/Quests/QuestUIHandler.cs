using System.Collections.Generic;
using UnityEngine;

public class QuestUIHandler : MonoBehaviour
{
    [SerializeField] private GameObject questPrefab;
    [SerializeField] private GameObject questContent;
    private List<QuestUI> activeQuests = new();

    public void AddQuestUI(Quest quest)
    {
        QuestData data = quest.Data;
        GameObject questInstance = Instantiate(questPrefab, questContent.transform);

        QuestUI questUI = questInstance.GetComponent<QuestUI>();
        questUI.UpdateText(quest);

        activeQuests.Add(questUI);
    }

    public void UpdateQuests()
    {
        foreach (var quest in activeQuests)
        {
            quest.UpdateText(quest.GetQuest());
        }
    }
}
