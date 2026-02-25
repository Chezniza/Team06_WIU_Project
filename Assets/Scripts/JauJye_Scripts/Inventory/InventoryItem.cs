using UnityEngine;

public class InventoryItem
{
    public ItemData data;
    public int gridX;       // top-left cell X in the grid
    public int gridY;       // top-left cell Y in the grid
    public int rotations;   // 0-3 clockwise 90-degree rotations

    public InventoryItem(ItemData data, int x = 0, int y = 0, int rotations = 0)
    {
        this.data = data;
        this.gridX = x;
        this.gridY = y;
        this.rotations = rotations;
    }

    public bool[,] GetCurrentShape() => data.GetRotatedShape(rotations);

    public (int width, int height) GetCurrentDimensions()
    {
        var shape = GetCurrentShape();
        return ItemData.GetShapeDimensions(shape);
    }

    public void Rotate()
    {
        rotations = (rotations + 1) % 4;
    }
}