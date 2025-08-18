using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UICondition : MonoBehaviour
{
    public Condition health;
    public Condition stamina;
    public Condition thirst;
    public Condition hunger;
    void Start()
    {
        CharacterManager.Instance.Player.condition.uiCondition = this;
    }
}
