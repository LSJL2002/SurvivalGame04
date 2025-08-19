using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Npc : MonoBehaviour
{

    public string npcName = "생존자";
    [TextArea]
    public string[] dialogues; // 여러 문장 지원
    void Awake()
    {
        dialogues = new string[]
        {
            "여긴 위험해, 조심히 다녀.",
            "내가 도와줄 수 있는 게 있을까?"
        };
    }
    public virtual void Interact()
    {
        DialogueManager.Instance.StartDialogue(npcName, dialogues);
    }
}
