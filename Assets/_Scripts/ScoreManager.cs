using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private UIManager _uiManager;

    public int CurrentScore { get; private set; }
    public int bestScore { get; private set; }

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
        _uiManager.UpdateBestScore();
    }

    public void FinalizeScore()
    {
        // Save best score if beaten
        if (CurrentScore > bestScore)
        {
            bestScore = CurrentScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }
    }

    private void UpdateScoreUI()
    {
        _uiManager.UpdateScoreUI(CurrentScore);
    }
}
