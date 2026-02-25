using System.Collections;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    public GameObject dialogueBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public string NPCName;
    public string[] lines;
    public float textSpeed = 0.05f;

    // Drag the DialogueTrigger on the same NPC into this field
    [SerializeField] private DialogueTrigger _dialogueTrigger;

    private int startIndex;
    private int endIndex;
    private int currentIndex;
    private bool isTyping;

    void OnEnable()
    {
        nameText.text = string.Empty;
        dialogueText.text = string.Empty;
    }

    void Start()
    {
        nameText.text = string.Empty;
        dialogueText.text = string.Empty;
    }

    public void PlayDialogue()
    {
        StopAllCoroutines();
        startIndex = 0;
        endIndex = lines.Length - 1;
        nameText.text = NPCName;
        dialogueBox.SetActive(true);
        currentIndex = startIndex;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        string line = lines[currentIndex];
        dialogueText.text = string.Empty;
        yield return null;
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSeconds(textSpeed);
        }
        isTyping = false;
    }

    void NextLine()
    {
        if (currentIndex < endIndex)
        {
            currentIndex++;
            dialogueText.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            // Dialogue finished — close box and unlock player
            dialogueBox.SetActive(false);
            _dialogueTrigger?.OnDialogueEnd();
        }
    }

    public void Click()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = lines[currentIndex];
            isTyping = false;
        }
        else
        {
            NextLine();
        }
    }
}