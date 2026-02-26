using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private InputHandler _inputHandler;

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
    }
}