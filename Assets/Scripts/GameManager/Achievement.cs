using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Achievement
{
    public string title;
    public int daysRequired;
    public bool isUnlocked;

    public Achievement(string title, int daysRequired)
    {
        this.title = title;
        this.daysRequired = daysRequired;
        isUnlocked = false;
    }

    public void Unlock()
    {
        isUnlocked = true;
    }
}
