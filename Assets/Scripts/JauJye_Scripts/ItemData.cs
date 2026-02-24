using UnityEngine;

public enum ItemType { Weapon, Helmet, Chestplate, Pants, Boots, Gauntlets, Potion, Quest }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType;
    public ItemRarity rarity;

    [Header("Grid Shape")]
    [Tooltip("Flat array read left to right, top to bottom. Size must equal shapeWidth x shapeHeight.")]
    public bool[] shape = { true };
    public int shapeWidth = 1;
    public int shapeHeight = 1;

    [Header("Stats")]
    public int statValue = 0;  // damage for weapons, defense for armour
    public int weight = 1;

    [Header("Potion")]
    public int healAmount = 0; // only used if ItemType == Potion

    public bool IsArmour()
    {
        return itemType == ItemType.Helmet ||
               itemType == ItemType.Chestplate ||
               itemType == ItemType.Pants ||
               itemType == ItemType.Boots ||
               itemType == ItemType.Gauntlets;
    }

    public bool[,] GetShape()
    {
        bool[,] result = new bool[shapeHeight, shapeWidth];
        for (int r = 0; r < shapeHeight; r++)
            for (int c = 0; c < shapeWidth; c++)
            {
                int index = r * shapeWidth + c;
                result[r, c] = index < shape.Length && shape[index];
            }
        return result;
    }

    public bool[,] GetRotatedShape(int rotations)
    {
        bool[,] current = GetShape();
        for (int i = 0; i < rotations % 4; i++)
            current = Rotate90(current);
        return current;
    }

    public static bool[,] Rotate90(bool[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        bool[,] rotated = new bool[cols, rows];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                rotated[c, rows - 1 - r] = original[r, c];
        return rotated;
    }

    public static (int width, int height) GetShapeDimensions(bool[,] shape)
        => (shape.GetLength(1), shape.GetLength(0));
}