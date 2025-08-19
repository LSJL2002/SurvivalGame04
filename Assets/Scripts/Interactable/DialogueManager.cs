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
        dialogues = lines;           //대사 배열 저장
        currentIndex = 0;            //첫번째 대사부터   
        ShowNextDialogue();          //첫 대사 출력   
    }

    public void ShowNextDialogue()
    {
        if (currentIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentIndex];   // 현재 대사 출력
            currentIndex++;
        }
        else
        {
            EndDialogue();      // 모든 대사를 보여줬다면 대화 종료
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
