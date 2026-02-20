using System.Collections.Generic;
using UnityEngine;

public class InventoryGrid
{
    public int width;
    public int height;

    // null = empty, otherwise holds the item occupying that cell
    private InventoryItem[,] cells;
    private List<InventoryItem> items = new List<InventoryItem>();

    public InventoryGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        cells = new InventoryItem[width, height];
    }

    public List<InventoryItem> GetAllItems() => items;

    // Try to place item at grid position (x, y). Returns true on success.
    public bool TryPlace(InventoryItem item, int x, int y)
    {
        bool[,] shape = item.GetCurrentShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        // Bounds + overlap check
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!shape[r, c]) continue;
                int gx = x + c;
                int gy = y + r;
                if (gx < 0 || gx >= width || gy < 0 || gy >= height) return false;
                if (cells[gx, gy] != null) return false;
            }
        }

        // Place
        item.gridX = x;
        item.gridY = y;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (shape[r, c])
                    cells[x + c, y + r] = item;

        if (!items.Contains(item))
            items.Add(item);

        return true;
    }

    // Remove item from grid cells (does not delete item)
    public void Remove(InventoryItem item)
    {
        bool[,] shape = item.GetCurrentShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (shape[r, c])
                {
                    int gx = item.gridX + c;
                    int gy = item.gridY + r;
                    if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                        if (cells[gx, gy] == item)
                            cells[gx, gy] = null;
                }

        items.Remove(item);
    }

    public InventoryItem GetItemAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return cells[x, y];
    }

    // Check if placement is valid without committing
    public bool CanPlace(InventoryItem item, int x, int y, InventoryItem ignoreItem = null)
    {
        bool[,] shape = item.GetCurrentShape();
        int rows = shape.GetLength(0);
        int cols = shape.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!shape[r, c]) continue;
                int gx = x + c;
                int gy = y + r;
                if (gx < 0 || gx >= width || gy < 0 || gy >= height) return false;
                var occupant = cells[gx, gy];
                if (occupant != null && occupant != ignoreItem) return false;
            }
        }
        return true;
    }

    // Find the first available position for an item. Returns true if found.
    public bool FindFreeSlot(InventoryItem item, out int outX, out int outY)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (CanPlace(item, x, y))
                {
                    outX = x;
                    outY = y;
                    return true;
                }
        outX = -1;
        outY = -1;
        return false;
    }
}