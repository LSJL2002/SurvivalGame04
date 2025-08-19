using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    private string[] dialogues;
    private int currentIndex;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(string npcName, string[] lines)
    {
        Time.timeScale = 0f;
        dialoguePanel.SetActive(true);
        nameText.text = npcName;
        dialogues = lines;           //��� �迭 ����
        currentIndex = 0;            //ù��° ������   
        ShowNextDialogue();          //ù ��� ���   
    }

    public void ShowNextDialogue()
    {
        if (currentIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentIndex];   // ���� ��� ���
            currentIndex++;
        }
        else
        {
            EndDialogue();      // ��� ��縦 ������ٸ� ��ȭ ����
        }
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextDialogue();
        }
    }
}
