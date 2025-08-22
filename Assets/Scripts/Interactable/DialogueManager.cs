using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Typing Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Typing Effect")]
    public float TypingSpeed;

    private string[] dialogues;
    private int currentIndex;
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextDialogue();
        }
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
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        if (currentIndex < dialogues.Length)
        {
            currentFullText = dialogues[currentIndex];   // ���� ��� ���
            typingCoroutine = StartCoroutine(TypeText(currentFullText));
            currentIndex++;
        }
        else
        {
            EndDialogue();      // ��� ��縦 ������ٸ� ��ȭ ����
        }
    }

    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        dialoguePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private IEnumerator TypeText(string TextToType)                 // 한 글자씩 출력하게 하는 코루틴
    {
        isTyping = true;
        dialogueText.text = "";

        for (int i = 0; i < TextToType.Length; i++)
        {
            dialogueText.text += TextToType[i];
            yield return new WaitForSecondsRealtime(TypingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = currentFullText;
        isTyping = false;
    }
}
