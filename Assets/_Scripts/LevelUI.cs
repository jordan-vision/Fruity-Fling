using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    int score = 0, moves = 15;
    float timer = 90;
    bool timerStarted = true, gameWon = false;
    int[] objectiveValues = new int [] { 3, 3, 3, 3, 3 };

    [SerializeField] Image[] objectiveSprites;
    [SerializeField] GameObject loseGame, winGame, pauseMenu;
    [SerializeField] TextMeshProUGUI movesLeft, timeLeft;
    [SerializeField] TextMeshProUGUI[] scoreTexts;
    [SerializeField] Animator star1, star2, star3;
    
    private void Update()
    {
        if (timerStarted)
        {
            timer -= Time.deltaTime;
            timeLeft.text = ((int)timer).ToString("00");
        }

        if (timer <= 0)
        {
            timerStarted = false;
            StartCoroutine(EndGame());
        }
    }

    private IEnumerator EndGame()
    {
        while (GameManager.Instance.LevelGrid.ResolvingMove)
        {
            yield return null;
        }

        if (gameWon)
        {
            winGame.SetActive(true);

            if (score >= 0)
            {
                star1.SetTrigger("Sparkle");
            }

            if (score >= 500)
            {
                star3.SetTrigger("Sparkle");
            }

            if (score >= 1000)
            {
                star2.SetTrigger("Sparkle");
            }
        } else
        {
            loseGame.SetActive(true);
        }
    }

    public void GetPoints(int points)
    {
        score += points;

        foreach (var text in scoreTexts)
        {
            text.text = score.ToString("000");
        }
    }

    // Load main scene
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void PauseGame()
    {
        if (GameManager.Instance.LevelGrid.ResolvingMove)
        {
            return;
        }

        timerStarted = false;
        pauseMenu.SetActive(true);
    }

    public void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ResumeGame()
    {
        timerStarted = true;
        pauseMenu.SetActive(false);
    }

    public void UpdateMoveCount()
    {
        moves -= 1;
        movesLeft.text = moves.ToString("00");

        if (moves == 0)
        {
            StartCoroutine(EndGame());
        }
    }

    public void UpdateObjectives(FruitType type)
    {
        var i = (int)type - 1;
        if (objectiveValues[i] <= 0)
        {
            return;
        }

        // Obscure the corresponding sprite
        var j = --objectiveValues[i];
        objectiveSprites[i * 3 + j].color = Color.black;

        var win = true;
        foreach (var value in objectiveValues)
        {
            if (value != 0)
            {
                win = false;
                break;
            }
        }

        if (win)
        {
            gameWon = true;
            GameManager.Instance.LevelGrid.GameWon = true;
        }
    }
}
