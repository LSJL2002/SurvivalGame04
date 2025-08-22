using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Npc : MonoBehaviour, IInteractable
{
    public string npcName;
    public string[] dialogues; // ���� ���� ����

    public string GetInteractPrompt()
    {
        string str = $"{npcName}";
        return str;
    }

    public void OnInteract()
    {
        DialogueManager.Instance.StartDialogue(npcName, dialogues);
    }
}
