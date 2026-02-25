using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        bool inventoryOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsInventoryOpen();
        bool dialogueOpen = DialogueManager.IsDialogueOpen();

        if (inventoryOpen || dialogueOpen)
            UnlockCursor();
        else
            LockCursor();
    }

    private void LockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}