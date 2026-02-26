using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private InputHandler _inputHandler;

    // Optional — only assign if this NPC gives a quest after dialogue
    [SerializeField] private QuestGiver _questGiver;

    private bool _playerInRange = false;

    public UnityEvent dialogueTrigger;
    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            onTriggerEnter.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            onTriggerExit.Invoke();
            OnDialogueEnd();
        }
    }

    private void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.T))
        {
            _inputHandler?.LockControls();
            DialogueManager.SetDialogueOpen(true);
            dialogueTrigger.Invoke();
        }
    }

    public void OnDialogueEnd()
    {
        _inputHandler?.UnlockControls();
        DialogueManager.SetDialogueOpen(false);

        // If this NPC has a quest to give, offer it after dialogue ends
        _questGiver?.OfferQuest();
    }
}