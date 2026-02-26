using UnityEngine;

public class KillQuest : Quest
{
    private int _currentKills;
    private QuestHandler _handler;

    public KillQuest(QuestData data, QuestHandler handler) : base(data)
    {
        _handler = handler;
    }

    public override void Initialize()
    {
        _currentKills = 0;
        IsCompleted = false;
    }

    public override void OnEnemyKilled(string enemyID)
    {
        if (IsCompleted) return;
        if (enemyID != Data.targetName) return;

        _currentKills++;
        Debug.Log($"[KillQuest] {_currentKills}/{Data.requiredAmount} {Data.targetName}");

        QuestLogUI.Instance?.Refresh();

        if (_currentKills >= Data.requiredAmount)
            CompleteQuest();
    }

    public override void OnItemCollected(string itemID) { }

    public override int GetProgress() => _currentKills;

    public override string GetProgressText() =>
        $"{_currentKills} / {Data.requiredAmount} {Data.targetName}s killed";

    private void CompleteQuest()
    {
        IsCompleted = true;
        _handler?.OnQuestCompleted(this);
    }
}