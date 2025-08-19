using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Npc : MonoBehaviour
{

    public string npcName = "������";
    [TextArea]
    public string[] dialogues; // ���� ���� ����
    void Awake()
    {
        dialogues = new string[]
        {
            "���� ������, ������ �ٳ�.",
            "���� ������ �� �ִ� �� ������?"
        };
    }
    public virtual void Interact()
    {
        DialogueManager.Instance.StartDialogue(npcName, dialogues);
    }
}
