using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AchievementBoard : MonoBehaviour
{
    public Transform contentParent; // Parent of the slots in the UI
    public GameObject achievementSlotPrefab; // Prefab with AchievementUI script

    void Start()
    {
        Time.timeScale = 1f;
        UpdateBoard();
    }

    public void UpdateBoard()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Spawn slots for all achievements
        foreach (var achievement in GameManager.Instance.achievements)
        {
            GameObject slot = Instantiate(achievementSlotPrefab, contentParent);
            var ui = slot.GetComponent<AchievementUI>();
            ui.SetAchievement(achievement.title, achievement.isUnlocked);
        }
    }

    public void StartButton()
    {
        SceneManager.LoadScene("Level");
    }
}
