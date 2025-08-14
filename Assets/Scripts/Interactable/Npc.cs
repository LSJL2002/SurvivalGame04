using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Npc : MonoBehaviour
{

    public string npcName = "NPC";
    [TextArea]
    public string[] dialogues; // ���� ���� ����

    public virtual void Interact()
    {
        DialogueManager.Instance.StartDialogue(npcName, dialogues);
    }

void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
