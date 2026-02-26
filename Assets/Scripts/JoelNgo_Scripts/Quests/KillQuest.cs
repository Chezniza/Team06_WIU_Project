using UnityEngine;

public class KillQuest : Quest
{
    private int currentKills;

    public KillQuest(QuestData data) : base(data) { }

    public override void Initialize()
    {
        currentKills = 0;
        IsCompleted = false;
    }

    public override void OnEnemyKilled(string enemyID)
    {
        if (IsCompleted) return;

        if (enemyID == Data.targetName)
        {
            currentKills++;

            if (currentKills >= Data.requiredAmount)
            {
                CompleteQuest();
            }
        }
    }

    public override void OnItemCollected(string itemID) { }

    public override int GetProgress()
    {
        return currentKills;
    }

    private void CompleteQuest()
    {
        IsCompleted = true;
        Debug.Log("Kill Quest Completed!");
    }
}
