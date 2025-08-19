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
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private float typingSpeed = 0.04f; // 타이핑 속도(초)

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
        dialogues = lines;
        currentIndex = 0;
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (isTyping)
        {
            // 타이핑 중일 때 E키를 누르면 즉시 전체 문장 출력
            CompleteTyping();
            return;
        }

        if (currentIndex < dialogues.Length)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeSentence(dialogues[currentIndex]));
            currentIndex++;
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence)
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        isTyping = false;
    }

    private void CompleteTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = dialogues[currentIndex - 1];
        isTyping = false;
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
