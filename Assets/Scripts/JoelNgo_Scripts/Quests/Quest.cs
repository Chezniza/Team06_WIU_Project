using UnityEngine;

public abstract class Quest
{
    public QuestData Data { get; private set; }
    public bool IsCompleted { get; protected set; }

    public Quest(QuestData data) { Data = data; }

    public abstract void Initialize();
    public abstract void OnEnemyKilled(string enemyID);
    public abstract void OnItemCollected(string itemID);

    // Teammate's original method — kept as is
    public abstract int GetProgress();

    // ADDED — used by QuestLogUI for display text
    public virtual string GetProgressText()
    {
        return $"{GetProgress()} / {Data.requiredAmount}";
    }
}