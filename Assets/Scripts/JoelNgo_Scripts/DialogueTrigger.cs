using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    private bool playerInRange = false;
    public UnityEvent dialogueTrigger;
    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            onTriggerEnter.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            onTriggerExit.Invoke();
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.T))
        {
            dialogueTrigger.Invoke();
        }
    }
}
