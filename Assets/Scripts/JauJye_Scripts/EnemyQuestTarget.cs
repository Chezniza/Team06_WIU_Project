using UnityEngine;

// Attach to enemy GameObject
// Drag QuestHandler into the field, then wire NotifyDeath() to Damageable's deathEvent
public class EnemyQuestTarget : MonoBehaviour
{
    [Tooltip("Must match targetName exactly in QuestData")]
    [SerializeField] private string _enemyID;

    [SerializeField] private QuestHandler _questHandler;

    // Wire this to Damageable.deathEvent in the Inspector
    public void NotifyDeath()
    {
        if (string.IsNullOrEmpty(_enemyID) || _questHandler == null) return;
        _questHandler.NotifyEnemyKilled(_enemyID);
        Debug.Log($"[EnemyQuestTarget] Notified kill: {_enemyID}");
    }
}