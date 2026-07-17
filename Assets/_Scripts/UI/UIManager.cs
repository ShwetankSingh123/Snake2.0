using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using CustomUI;

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
    [SerializeField] private CustomButton continueButton;
    [SerializeField] private CustomButton newGameButton;
    [SerializeField] private CustomButton exitButton;
    [SerializeField] private CustomButton optionsButton;
    [SerializeField] private CustomButton howToPlayButton;

    [Header("Main Menu Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private TMP_Text howToPlayText;

    [Header("Difficulty Selection Panel")]
    [SerializeField] private GameObject difficultySelectionPanel;
    [SerializeField] private CustomButton easyButton;
    [SerializeField] private CustomButton normalButton;
    [SerializeField] private CustomButton hardButton;
    [SerializeField] private CustomButton extremeButton;
    [SerializeField] private CustomButton startGameButton;
    [SerializeField] private CustomButton backFromDifficultyButton;

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CustomButton resumeButton;
    [SerializeField] private CustomButton mainMenuFromPauseButton;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private TMP_Text gameOverBestText;
    [SerializeField] private TMP_Text newBestLabel;

    [Space(10)]
    [SerializeField] private Sprite _selectedButtonSprite;
    [SerializeField] private Sprite _normalButtonSprite;

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
        // Hover visuals
        AddHoverEvents(continueButton);
        AddHoverEvents(newGameButton);
        AddHoverEvents(exitButton);
        AddHoverEvents(optionsButton);
        AddHoverEvents(howToPlayButton);
        AddHoverEvents(resumeButton);
        AddHoverEvents(mainMenuFromPauseButton);
        AddHoverEvents(easyButton);
        AddHoverEvents(normalButton);
        AddHoverEvents(hardButton);
        AddHoverEvents(extremeButton);
        AddHoverEvents(startGameButton);
        AddHoverEvents(backFromDifficultyButton);

        // Panels initial state
        specialTimerPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        comboPanel?.SetActive(false);
        shieldIcon?.SetActive(false);
        ghostIcon?.SetActive(false);
        optionsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
        difficultySelectionPanel?.SetActive(false);

        // Button listeners
        if (continueButton)  continueButton.onClick.AddListener(()  => GameManager.Instance.ContinueGame());
        if (newGameButton)   newGameButton.onClick.AddListener(()   => OpenDifficultySelection());
        if (exitButton)      exitButton.onClick.AddListener(()      => GameManager.Instance.ExitGame());
        if (optionsButton)   optionsButton.onClick.AddListener(()   => OpenOptionsPanel());
        if (howToPlayButton) howToPlayButton.onClick.AddListener(() => OpenHowToPlay());
        if (resumeButton)    resumeButton.onClick.AddListener(()    => GameManager.Instance.ResumeGame());
        if (mainMenuFromPauseButton) mainMenuFromPauseButton.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());

        // Difficulty selection listeners
        if (easyButton)  easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Easy));
        if (normalButton) normalButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Normal));
        if (hardButton)  hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));
        if (extremeButton) extremeButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Extreme));
        if (startGameButton) startGameButton.onClick.AddListener(() => StartGameFromDifficulty());
        if (backFromDifficultyButton) backFromDifficultyButton.onClick.AddListener(() => CloseDifficultySelection());

        // HowToPlay content
        if (howToPlayText)
        {
            howToPlayText.text = "Objective:\nEat food to grow and score points. Avoid hitting walls or yourself.\n\nControls:\nArrow Keys / WASD to move. Esc to pause.\n\nFood Types and Effects:\n- Normal: regular growth and points.\n- Golden: extra points.\n- Bomb: ends the game if collected.\n- Shrink: reduces snake length.\n- Speed: increases movement speed.\n- Slow: reduces movement speed.\n- Ghost: pass through walls/obstacles for a short time.\n- Shield: protects from one collision.\n";
        }

        // Default difficulty visual
        SelectDifficulty(Difficulty.Normal);
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
        Debug.Log($"Starting special timer for {duration} seconds with color {color}");
        timerDuration = timerRemaining = duration;
        timerActive = true;
        //specialTimerPanel?.SetActive(true);
        specialTimerFill.transform.parent.gameObject.SetActive(true);
        if (specialTimerFill) { specialTimerFill.fillAmount = 1f; specialTimerFill.color = color; }
    }

    public void StopSpecialTimer()
    {
        timerActive = false;
        //specialTimerPanel?.SetActive(false);
        specialTimerFill.transform.parent.gameObject.SetActive(false);
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
        optionsPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
        difficultySelectionPanel?.SetActive(false);
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

    // --- Main Menu / Difficulty / Options / HowToPlay ---
    private Difficulty selectedDifficulty = Difficulty.Normal;

    private void OpenDifficultySelection()
    {
        mainMenuPanel?.SetActive(false);
        difficultySelectionPanel?.SetActive(true);
        // ensure visuals reflect current selection
        SetDifficultyButtonSprites();
    }

    private void CloseDifficultySelection()
    {
        difficultySelectionPanel?.SetActive(false);
        mainMenuPanel?.SetActive(true);
    }

    private void SelectDifficulty(Difficulty diff)
    {
        selectedDifficulty = diff;
        // apply immediately so other systems can read current difficulty if needed
        GameManager.Instance.SetDifficulty(selectedDifficulty);
        SetDifficultyButtonSprites();
    }

    private void StartGameFromDifficulty()
    {
        // GameManager already has difficulty set by SelectDifficulty
        difficultySelectionPanel?.SetActive(false);
        GameManager.Instance.StartNewGame();
    }

    private void OpenOptionsPanel()
    {
        optionsPanel?.SetActive(true);
        mainMenuPanel?.SetActive(false);
    }

    public void CloseOptionsPanel()
    {
        optionsPanel?.SetActive(false);
        mainMenuPanel?.SetActive(true);
    }

    private void OpenHowToPlay()
    {
        howToPlayPanel?.SetActive(true);
        mainMenuPanel?.SetActive(false);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel?.SetActive(false);
        mainMenuPanel?.SetActive(true);
    }

    private void SetDifficultyButtonSprites()
    {
        if (easyButton) SetButtonSprite(easyButton, selectedDifficulty == Difficulty.Easy ? _selectedButtonSprite : _normalButtonSprite);
        if (normalButton) SetButtonSprite(normalButton, selectedDifficulty == Difficulty.Normal ? _selectedButtonSprite : _normalButtonSprite);
        if (hardButton) SetButtonSprite(hardButton, selectedDifficulty == Difficulty.Hard ? _selectedButtonSprite : _normalButtonSprite);
        if (extremeButton) SetButtonSprite(extremeButton, selectedDifficulty == Difficulty.Extreme ? _selectedButtonSprite : _normalButtonSprite);
    }

    private void SetButtonSprite(Button button, Sprite sprite)
    {
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.sprite = sprite;
    }

    private void AddHoverEvents(Button button)
    {
        if (button == null) return;

        EventTrigger trigger = button.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((_) => SetButtonSprite(button, _selectedButtonSprite));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((_) => SetButtonSprite(button, _normalButtonSprite));
        trigger.triggers.Add(exit);
    }
}
