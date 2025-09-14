using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text gameOverText;

    public int CurrentScore { get; private set; }
    private int bestScore = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        CurrentScore = 0;
        gameOverText.gameObject.SetActive(false);

        // Load best score
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        UpdateScoreUI();
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;

        if (CurrentScore > bestScore)
            bestScore = CurrentScore;

        UpdateScoreUI();
    }

    public void SetScore(int value)
    {
        CurrentScore = value;

        if (CurrentScore > bestScore)
            bestScore = CurrentScore;

        UpdateScoreUI();
    }

    public void GameOver()
    {
        // Save best score if beaten
        if (CurrentScore > bestScore)
        {
            bestScore = CurrentScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        gameOverText.gameObject.SetActive(true);
        gameOverText.text = $"GAME OVER\nScore: {CurrentScore}\nBest: {bestScore}\nPress R to Restart";
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {CurrentScore}   Best: {bestScore}";
    }
}
