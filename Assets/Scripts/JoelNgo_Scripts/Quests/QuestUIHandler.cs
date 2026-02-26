using UnityEngine;

public class QuestUIHandler : MonoBehaviour
{
    [SerializeField] private GameObject questPrefab;
    [SerializeField] private GameObject questContent;

    public void AddQuestUI(Quest quest)
    {
        QuestData data = quest.Data;
        GameObject questInstance = Instantiate(questPrefab, questContent.transform);
        questInstance.GetComponent<QuestUI>().UpdateText(quest);
    }
}
