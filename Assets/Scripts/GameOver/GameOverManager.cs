using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverUI;
    public TextMeshProUGUI finalScore;
    public static bool IsGameOver { get; private set; }

    private void Start()
    {
        Time.timeScale = 1f;
        gameOverUI.SetActive(false);
        IsGameOver = false;
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;
        IsGameOver = true;

        // Show UI
        gameOverUI.SetActive(true);

        if (GameManager.Instance != null)
        {
            finalScore.text = "Days Survived: " + GameManager.Instance.daysSurvived.ToString();
        }
        else
        {
            finalScore.text = "Days Survived: 0";
        }   
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;  
        IsGameOver = false;
        SceneManager.LoadScene("Level");
    }

    public void GoToHome()
    {
        Time.timeScale = 1f;  
        IsGameOver = false;
        SceneManager.LoadScene("StartScreen");
    }
}
