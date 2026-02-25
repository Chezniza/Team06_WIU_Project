using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest to offer after dialogue")]
    [SerializeField] private QuestData _questData;
    [SerializeField] private string _npcName;

    [Header("References")]
    [SerializeField] private QuestHandler _questHandler; // drag QuestManager GameObject here

    // Called by DialogueTrigger.OnDialogueEnd()
    public void OfferQuest()
    {
        if (_questData == null || _questHandler == null) return;

        // Don't offer if already in active list
        //if (_questHandler.GetActiveQuests().Exists(q => q.Data.questID == _questData.questID))
        //{
        //    Debug.Log("[QuestGiver] Quest already active.");
        //    return;
        //}

        //QuestOfferUI.Instance?.Show(_questData, _npcName, _questHandler);
    }
}