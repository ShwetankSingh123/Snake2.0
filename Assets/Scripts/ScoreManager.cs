using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text gameOverText;

    private int score = 0;
    private int bestScore = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        score = 0;
        gameOverText.gameObject.SetActive(false);
        // Load best score from PlayerPrefs
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        UpdateScoreUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score > bestScore) bestScore = score;
        UpdateScoreUI();
    }

    public void GameOver()
    {
        // Save best score if beaten
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = $"GAME OVER\nScore: {score}\nBest: {bestScore}\nPress R to Restart";
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private void UpdateScoreUI()
    {
        scoreText.text = $"Score: {score}   Best: {bestScore}";
    }
}
