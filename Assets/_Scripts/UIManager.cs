using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    [Header("Special Food Timer UI")]
    [SerializeField] private GameObject _gameplayPanel;
    [SerializeField] private TMP_Text _currentScoreText;
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private GameObject _specialTimerPanel; // container panel
    [SerializeField] private Image _specialTimerFill;       // fill bar

    [Header("Main Menu UI")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _exitButton;

    [Header("Game Over UI")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _gameOverText;

    private float timerDuration;
    private float timerRemaining;
    private bool isActive = false;

    

    void Start()
    {
        if (_specialTimerPanel != null)
            _specialTimerPanel.SetActive(false);

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);

        if (_mainMenuPanel != null)
        {
            _mainMenuPanel.SetActive(true);

            // Enable/disable Continue based on save
            bool hasSave = PlayerPrefs.HasKey("HasSave") && PlayerPrefs.GetInt("HasSave") == 1;
            if (_continueButton != null)
                _continueButton.gameObject.SetActive(hasSave);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(() => GameManager.Instance.ContinueGame());
            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(() => GameManager.Instance.StartNewGame());
            if (_exitButton != null)
                _exitButton.onClick.AddListener(() => GameManager.Instance.ExitGame());
        }
    }

    void Update()
    {
        if (!isActive) return;

        timerRemaining -= Time.deltaTime;

        if (_specialTimerFill != null)
            _specialTimerFill.fillAmount = Mathf.Clamp01(timerRemaining / timerDuration);

        if (timerRemaining <= 0f)
            StopSpecialTimer();
    }

    public void StartSpecialTimer(float duration, Color barColor)
    {
        timerDuration = duration;
        timerRemaining = duration;
        isActive = true;

        if (_specialTimerPanel != null)
            _specialTimerPanel.SetActive(true);

        if (_specialTimerFill != null)
        {
            _specialTimerFill.fillAmount = 1f;
            _specialTimerFill.color = barColor;
        }
    }

    public void ShowGameplayUI()
    {
        if (_gameplayPanel != null)
            _gameplayPanel.SetActive(true);
    }

    public void StopSpecialTimer()
    {
        isActive = false;
        if (_specialTimerPanel != null)
            _specialTimerPanel.SetActive(false);
    }

    public void ShowMainMenuUI()
    {
        // Enable Main Menu panel
        _mainMenuPanel.SetActive(true);

        // Hide gameplay UI
        _gameplayPanel.SetActive(false);

        // Hide game over UI
        _gameOverPanel.SetActive(false);
    }

    public void UpdateScoreUI(int CurrentScore)
    {
        if (_currentScoreText != null)
            _currentScoreText.text = $"Score: {CurrentScore}";
    }

    public void UpdateBestScore()
    {
        if (_bestScoreText != null)
            _bestScoreText.text = $"Best Score: {ScoreManager.Instance.bestScore}";
    }

    public void HideMainMenuPanel()
    {
        if (_mainMenuPanel != null)
            _mainMenuPanel.SetActive(false);
    }

    // === Game Over UI ===
    public void ShowGameOverUI()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);

        _gameOverText.text = $"Game Over!\n\n<size=50%>Your Score: {ScoreManager.Instance.CurrentScore}\nBest Score: {ScoreManager.Instance.bestScore}</size>";     
    }

    public void HideGameOverUI()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    
}
