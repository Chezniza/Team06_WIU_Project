using UnityEngine;

public enum QuestType { Kill, Fetch }

[System.Serializable]
public class QuestItemReward
{
    public ItemData item;
    public int quantity = 1;
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/Quest Data")]
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
    public int rewardXP;

    [Header("Item Rewards")]
    public QuestItemReward[] itemRewards;

    [Header("UI")]
    public Sprite icon;
}