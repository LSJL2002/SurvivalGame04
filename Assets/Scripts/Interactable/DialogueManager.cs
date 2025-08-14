using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public GameObject dialoguePanel;
    public Text nameText;
    public Text dialogueText;

    private string[] dialogues;
    private int currentIndex;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(string npcName, string[] lines)
    {
        dialoguePanel.SetActive(true);
        nameText.text = npcName;
        dialogues = lines;
        currentIndex = 0;
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (currentIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentIndex];
            currentIndex++;
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextDialogue();
        }
    }
}
