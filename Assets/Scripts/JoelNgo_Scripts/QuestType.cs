using UnityEngine;

public enum QuestType
{
    Kill,
    Fetch
}

[CreateAssetMenu(fileName = "QuestType", menuName = "Scriptable Objects/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Quest Details")]
    public int questID;
    public string title;
    public string description;
    public QuestType questType;

    [Header("Kill Quest")]
    public string targetName;
    public int requiredAmount;

    [Header("Fetch Quest")]
    public string itemName;

    [Header("Reward")]
    public int rewardGold;
    public int rewardXP;
}
