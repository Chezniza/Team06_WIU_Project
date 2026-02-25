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

    private int startIndex;
    private int endIndex;
    private int currentIndex; // track what line we�re on
    private bool isTyping;

    void OnEnable()
    {
        // Do nothing prevent Unity from auto-typing a line
        nameText.text = string.Empty;
        dialogueText.text = string.Empty;
    }
    void Start()
    {
        nameText.text = string.Empty;
        dialogueText.text = string.Empty;
    }

    public void setStartIndex(int num)
    {
        startIndex = num;
    }

    public void setEndIndex(int num)
    {
        endIndex = num;
    }

    public void PlayDialogue()
    {
        StopAllCoroutines();
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

        // Wait one frame to ensure UI updates
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
            dialogueBox.SetActive(false);

            Debug.Log("dialogue end");
        }
    }

    public void Click()
    {
        // Skip line if user clicks while still text still typing
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