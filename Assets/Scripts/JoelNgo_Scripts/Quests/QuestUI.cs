using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questPrefab;
    [SerializeField] private GameObject chatContent;

    void AddQuestUI()
    {
        Instantiate(questPrefab, chatContent.transform);
    }
}
