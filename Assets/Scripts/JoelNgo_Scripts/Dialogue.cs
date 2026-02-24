using System.Collections;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string NPCName;
    public string[] lines;
    public float textSpeed = 0.05f;

    private int startIndex;
    private int endIndex;
    private int currentIndex; // track what line we�re on

    void OnEnable()
    {
        // Do nothing prevent Unity from auto-typing a line
        textComponent.text = string.Empty;
    }
    void Start()
    {
        textComponent.text = string.Empty;
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
        gameObject.SetActive(true);

        currentIndex = startIndex;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        string line = lines[currentIndex];
        textComponent.text = string.Empty;

        // Wait one frame to ensure UI updates (a missing character bug occurs without this)
        yield return null;

        for (int i = 0; i < line.Length; i++)
        {
            textComponent.text += line[i];
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        if (currentIndex < endIndex)
        {
            currentIndex++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            gameObject.SetActive(false);

            Debug.Log("dialogue end");
        }
    }

    public void Click()
    {
        // If line finished, go to next
        if (textComponent.text == lines[currentIndex])
        {
            NextLine();
        }
        // If still typing, instantly complete line
        else
        {
            StopAllCoroutines();
            textComponent.text = lines[currentIndex];
        }
    }
}