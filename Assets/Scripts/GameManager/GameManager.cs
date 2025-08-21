using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Progress")]
    public int daysSurvived = 1;

    [Header("Achievements")]
    public List<Achievement> achievements = new List<Achievement>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Debug.Log(daysSurvived);

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Example achievements
        achievements.Add(new Achievement("Survive 3 Days", 3));
        achievements.Add(new Achievement("Survive 5 Days", 5));
        achievements.Add(new Achievement("Survive 10 Days", 10));
    }

    public void OnNewDay(int currentDay)
    {
        daysSurvived = currentDay;

        // Check if any achievements are unlocked
        foreach (var achievement in achievements)
        {
            if (!achievement.isUnlocked && currentDay >= achievement.daysRequired)
            {
                achievement.Unlock();
            }
        }
    }
}
