using UnityEngine;

public abstract class Quest
{
    public QuestData Data { get; private set; }
    public bool IsCompleted { get; protected set; }

    public Quest(QuestData data)
    {
        Data = data;
    }

    public abstract void Initialize();
    public abstract void OnEnemyKilled(string enemyID);
    public abstract void OnItemCollected(string itemID);

    public abstract int GetProgress();
}