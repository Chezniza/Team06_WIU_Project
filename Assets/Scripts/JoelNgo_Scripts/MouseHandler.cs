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
        bool wheelOpen = ShortcutWheel.Instance != null && ShortcutWheel.Instance.IsOpen();
        bool questLogOpen = QuestLogUI.Instance != null && QuestLogUI.Instance.IsOpen();

        if (inventoryOpen || dialogueOpen || wheelOpen || questLogOpen)
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