using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI questText;

    public void UpdateText(Quest quest)
    {
        QuestData data = quest.Data;

        // Add title
        questText.text = data.title;

        // Add quest type
        switch (data.questType)
        {
            case QuestType.Kill:
                questText.text += " Kill " + data.targetName;
                break;

            case QuestType.Fetch:
                questText.text += " Fetch " + data.targetName;
                break;
        }

        // Add quest progress
        questText.text += " " + quest.GetProgress() + " / " + data.requiredAmount;
    }
}
