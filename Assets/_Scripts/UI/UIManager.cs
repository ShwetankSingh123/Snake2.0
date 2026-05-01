using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Gameplay Panel")]
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject comboPanel;

    [Header("Status Effect Icons")]
    [SerializeField] private GameObject shieldIcon;
    [SerializeField] private GameObject ghostIcon;

    [Header("Special Food Timer")]
    [SerializeField] private GameObject specialTimerPanel;
    [SerializeField] private Image specialTimerFill;
    [SerializeField] private TMP_Text specialTimerLabel;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuFromPauseButton;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private TMP_Text gameOverBestText;
    [SerializeField] private TMP_Text newBestLabel;

    private float timerDuration;
    private float timerRemaining;
    private bool timerActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        specialTimerPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        comboPanel?.SetActive(false);
        shieldIcon?.SetActive(false);
        ghostIcon?.SetActive(false);

        if (continueButton)  continueButton.onClick.AddListener(()  => GameManager.Instance.ContinueGame());
        if (newGameButton)   newGameButton.onClick.AddListener(()   => GameManager.Instance.StartNewGame());
        if (exitButton)      exitButton.onClick.AddListener(()      => GameManager.Instance.ExitGame());
        if (resumeButton)    resumeButton.onClick.AddListener(()    => GameManager.Instance.ResumeGame());
        if (mainMenuFromPauseButton) mainMenuFromPauseButton.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());

        if (difficultyDropdown)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "Easy", "Normal", "Hard", "Extreme" });
            difficultyDropdown.value = 1; // Normal default
            difficultyDropdown.onValueChanged.AddListener(v => GameManager.Instance.SetDifficulty((Difficulty)v));
        }
    }

    void Update()
    {
        if (!timerActive) return;
        timerRemaining -= Time.deltaTime;
        if (specialTimerFill) specialTimerFill.fillAmount = Mathf.Clamp01(timerRemaining / timerDuration);
        if (timerRemaining <= 0f) StopSpecialTimer();
    }

    // ─── Score ────────────────────────────────────
    public void UpdateScoreUI(int score)
    {
        if (currentScoreText) currentScoreText.text = score.ToString();
    }

    public void UpdateBestScore()
    {
        if (bestScoreText) bestScoreText.text = $"Best: {ScoreManager.Instance.bestScore}";
    }

    public void UpdateComboUI(int combo)
    {
        if (comboPanel == null) return;
        if (combo <= 1) { comboPanel.SetActive(false); return; }
        comboPanel.SetActive(true);
        if (comboText) comboText.text = $"x{combo} COMBO!";
    }

    // ─── Timer ───────────────────────────────────
    public void StartSpecialTimer(float duration, Color color)
    {
        timerDuration = timerRemaining = duration;
        timerActive = true;
        specialTimerPanel?.SetActive(true);
        if (specialTimerFill) { specialTimerFill.fillAmount = 1f; specialTimerFill.color = color; }
    }

    public void StopSpecialTimer()
    {
        timerActive = false;
        specialTimerPanel?.SetActive(false);
    }

    // ─── Status Effects ──────────────────────────
    public void ShowShieldIndicator(bool on) => shieldIcon?.SetActive(on);
    public void ShowGhostIndicator(bool on)  => ghostIcon?.SetActive(on);

    // ─── Panels ──────────────────────────────────
    public void ShowGameplayUI()  => gameplayPanel?.SetActive(true);
    public void HideMainMenuPanel() => mainMenuPanel?.SetActive(false);
    public void HideGameOverUI()  => gameOverPanel?.SetActive(false);
    public void HidePausePanel()  => pausePanel?.SetActive(false);

    public void ShowPausePanel()
    {
        gameplayPanel?.SetActive(false);
        pausePanel?.SetActive(true);
    }

    public void ShowMainMenuUI()
    {
        bool hasSave = PlayerPrefs.GetInt("HasSave", 0) == 1;
        if (continueButton) continueButton.gameObject.SetActive(hasSave);
        mainMenuPanel?.SetActive(true);
        gameplayPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        pausePanel?.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        gameOverPanel?.SetActive(true);
        int cur  = ScoreManager.Instance.CurrentScore;
        int best = ScoreManager.Instance.bestScore;
        bool isNew = cur >= best && cur > 0;
        if (gameOverScoreText) gameOverScoreText.text = cur.ToString();
        if (gameOverBestText)  gameOverBestText.text  = best.ToString();
        if (newBestLabel)      newBestLabel.gameObject.SetActive(isNew);
    }
}
