public static class DialogueManager
{
    private static bool _dialogueOpen = false;

    public static bool IsDialogueOpen() => _dialogueOpen;

    public static void SetDialogueOpen(bool open)
    {
        _dialogueOpen = open;
    }
}