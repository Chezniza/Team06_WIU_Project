using System.Collections;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialogueBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public string NPCName;
    public string[] lines;
    public float textSpeed = 0.05f;

    [SerializeField] private DialogueTrigger _dialogueTrigger;

    private int _currentIndex;
    private bool _isTyping = false;
    private bool _isOpen = false;

    void Start()
    {
        dialogueBox.SetActive(false);
    }

    void Update()
    {
        if (!_isOpen) return;

        if (Input.GetKeyDown(KeyCode.E))
            Advance();
    }

    public void PlayDialogue()
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("[Dialogue] No lines assigned!");
            return;
        }

        StopAllCoroutines();
        _currentIndex = 0;
        _isOpen = true;
        nameText.text = NPCName;
        dialogueText.text = string.Empty;
        dialogueBox.SetActive(true);
        StartCoroutine(TypeLine());
    }

    private void Advance()
    {
        if (_isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = lines[_currentIndex];
            _isTyping = false;
        }
        else
        {
            NextLine();
        }
    }

    private IEnumerator TypeLine()
    {
        _isTyping = true;
        dialogueText.text = string.Empty;
        yield return null;

        foreach (char c in lines[_currentIndex])
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        _isTyping = false;
    }

    private void NextLine()
    {
        // Last line index is lines.Length - 1
        if (_currentIndex < lines.Length - 1)
        {
            _currentIndex++;
            dialogueText.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        _isOpen = false;
        _isTyping = false;
        dialogueBox.SetActive(false);
        _dialogueTrigger?.OnDialogueEnd();
    }
}