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
    [TextArea] public string description;
    public QuestType questType;

    [Header("Shared Data")]
    public int requiredAmount;

    [Header("Kill Quest")]
    public string targetName;

    [Header("Fetch Quest")]
    public string itemName;

    [Header("Reward")]
    public int rewardGold;
    public int rewardXP;   // ADDED

    [Header("UI")]
    public Sprite icon;    // ADDED
}