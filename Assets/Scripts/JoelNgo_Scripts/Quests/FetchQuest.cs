using UnityEngine;

public class FetchQuest : Quest
{
    private int collected;

    public FetchQuest(QuestData data) : base(data) { }

    public override void Initialize()
    {
        collected = 0;
        IsCompleted = false;
    }

    public override void OnEnemyKilled(string enemyID) { }

    public override void OnItemCollected(string itemID)
    {
        if (IsCompleted) return;

        if (itemID == Data.itemName)
        {
            collected++;

            if (collected >= Data.requiredAmount)
            {
                CompleteQuest();
            }
        }
    }

    public override int GetProgress()
    {
        return collected;
    }

    private void CompleteQuest()
    {
        IsCompleted = true;
        Debug.Log("Fetch Quest Completed!");
    }
}
