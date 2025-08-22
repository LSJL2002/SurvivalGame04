using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AchievementUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Image checkmark;

    public void SetAchievement(string title, bool isUnlocked)
    {
        titleText.text = title;
        checkmark.enabled = isUnlocked;
    }
}