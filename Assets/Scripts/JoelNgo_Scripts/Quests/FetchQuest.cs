using UnityEngine;

public class FetchQuest : Quest
{
    private int _collected;
    private QuestHandler _handler;

    public FetchQuest(QuestData data, QuestHandler handler) : base(data)
    {
        _handler = handler;
    }

    public override void Initialize()
    {
        _collected = 0;
        IsCompleted = false;
    }

    public override void OnEnemyKilled(string enemyID) { }

    public override void OnItemCollected(string itemID)
    {
        if (IsCompleted) return;
        if (itemID != Data.itemName) return;

        _collected++;
        Debug.Log($"[FetchQuest] {_collected}/{Data.requiredAmount} {Data.itemName}");

        QuestLogUI.Instance?.Refresh();

        if (_collected >= Data.requiredAmount)
            CompleteQuest();
    }

    public override int GetProgress() => _collected;

    public override string GetProgressText() =>
        $"{_collected} / {Data.requiredAmount} {Data.itemName}s collected";

    private void CompleteQuest()
    {
        IsCompleted = true;
        _handler?.OnQuestCompleted(this);
    }
}