using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using CustomUI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    #region Main Menu

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private CustomButton continueButton;
    [SerializeField] private CustomButton newGameButton;
    [SerializeField] private CustomButton howToPlayButton;
    [SerializeField] private CustomButton exitButton;

    // Bottom-right icons
    [SerializeField] private CustomButton musicToggleButton;
    [SerializeField] private CustomButton hapticsToggleButton;
    [SerializeField] private Image musicToggleIcon;
    [SerializeField] private Image hapticsToggleIcon;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite hapticsOnSprite;
    [SerializeField] private Sprite hapticsOffSprite;

    #endregion

    #region Difficulty

    [Header("Difficulty Panel")]
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private CustomButton diffLeftButton;
    [SerializeField] private TMP_Text difficultyNameText;
    [SerializeField] private CustomButton diffRightButton;
    [SerializeField] private TMP_Text difficultyDescriptionText;
    [SerializeField] private CustomButton difficultyStartButton;
    [SerializeField] private CustomButton difficultyBackButton;

    private enum UIDifficulty { Easy = 0, Normal = 1, Hard = 2, Extreme = 3 }
    private UIDifficulty selectedDifficulty = UIDifficulty.Normal;

    #endregion

    #region How To Play

    [Header("How To Play Panel")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private ScrollRect howToPlayScrollRect;
    [SerializeField] private CustomButton howToPlayBackButton;
    [SerializeField] private TMP_Text howToPlayContentText;

    // Individual food entries (allows richer layout with icons)
    [Header("How To Play - Food Entries")]
    [SerializeField] private TMP_Text normalFoodText;
    [SerializeField] private TMP_Text goldenFoodText;
    [SerializeField] private TMP_Text bombFoodText;
    [SerializeField] private TMP_Text shrinkFoodText;
    [SerializeField] private TMP_Text speedFoodText;
    [SerializeField] private TMP_Text slowFoodText;
    [SerializeField] private TMP_Text ghostFoodText;
    [SerializeField] private TMP_Text shieldFoodText;

    #endregion

    #region Gameplay

    [Header("Gameplay Panel")]
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject comboPanel;

    #endregion

    #region Pause

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CustomButton resumeButton;
    [SerializeField] private CustomButton mainMenuFromPauseButton;

    #endregion

    #region Game Over

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private TMP_Text gameOverBestText;
    [SerializeField] private TMP_Text newBestLabel;

    #endregion

    #region Status Effects

    [Header("Status Effect Icons")]
    [SerializeField] private GameObject shieldIcon;
    [SerializeField] private GameObject ghostIcon;

    #endregion

    #region Timer

    [Header("Special Food Timer")]
    [SerializeField] private GameObject specialTimerPanel;
    [SerializeField] private Image specialTimerFill;
    [SerializeField] private TMP_Text specialTimerLabel;

    private float timerDuration;
    private float timerRemaining;
    private bool timerActive;

    #endregion

    #region Hover

    [Space(10)]
    [SerializeField] private Sprite _selectedButtonSprite;
    [SerializeField] private Sprite _normalButtonSprite;

    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Register hover visuals for buttons
        RegisterHover(continueButton);
        RegisterHover(newGameButton);
        RegisterHover(howToPlayButton);
        RegisterHover(exitButton);
        RegisterHover(musicToggleButton);
        RegisterHover(hapticsToggleButton);
        RegisterHover(diffLeftButton);
        RegisterHover(diffRightButton);
        RegisterHover(difficultyStartButton);
        RegisterHover(difficultyBackButton);
        RegisterHover(howToPlayBackButton);
        RegisterHover(resumeButton);
        RegisterHover(mainMenuFromPauseButton);

        // Initial panel states
        specialTimerPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        comboPanel?.SetActive(false);
        shieldIcon?.SetActive(false);
        ghostIcon?.SetActive(false);
        howToPlayPanel?.SetActive(false);
        difficultyPanel?.SetActive(false);

        // Main menu bindings
        if (continueButton) continueButton.onClick.AddListener(() => GameManager.Instance.ContinueGame());
        if (newGameButton)  newGameButton.onClick.AddListener(ShowDifficultyPanel);
        if (howToPlayButton) howToPlayButton.onClick.AddListener(ShowHowToPlayPanel);
        if (exitButton)     exitButton.onClick.AddListener(() => GameManager.Instance.ExitGame());

        // Difficulty panel bindings
        if (diffLeftButton) diffLeftButton.onClick.AddListener(() => CycleDifficulty(-1));
        if (diffRightButton) diffRightButton.onClick.AddListener(() => CycleDifficulty(1));
        if (difficultyStartButton) difficultyStartButton.onClick.AddListener(() => { HideDifficultyPanel(); GameManager.Instance.StartNewGame(); });
        if (difficultyBackButton) difficultyBackButton.onClick.AddListener(HideDifficultyPanel);

        // HowToPlay
        if (howToPlayBackButton) howToPlayBackButton.onClick.AddListener(HideHowToPlayPanel);

        // Pause bindings
        if (resumeButton) resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());
        if (mainMenuFromPauseButton) mainMenuFromPauseButton.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());

        // Music & Haptics toggles
        if (musicToggleButton) musicToggleButton.onClick.AddListener(ToggleMusic);
        if (hapticsToggleButton) hapticsToggleButton.onClick.AddListener(ToggleHaptics);

        // Initialize difficulty from GameManager
        if (GameManager.Instance != null) selectedDifficulty = (UIDifficulty)GameManager.Instance.CurrentDifficulty;
        UpdateDifficultyUI();

        // Populate HowToPlay content
        if (howToPlayContentText != null)
        {
            howToPlayContentText.text =
                "OBJECTIVE\n\nEat food.\nAvoid walls.\nAvoid your own body.\n\nCONTROLS\n\nArrow Keys\nWASD\nESC = Pause\n\nSPECIAL FOODS\n\nNormal - Regular food that grows the snake and gives points.\n\nGolden - Extra points when collected.\n\nBomb - Ends the game if collected.\n\nShrink - Reduces snake length.\n\nSpeed - Temporarily increases snake speed.\n\nSlow - Temporarily decreases snake speed.\n\nGhost - Temporarily allows passing through walls/obstacles.\n\nShield - Protects from one collision.\n";
        }
        // Populate individual entries if present (prefer these when designer has separate fields)
        if (normalFoodText != null) normalFoodText.text = "Normal - Regular food that grows the snake and gives points.";
        if (goldenFoodText != null) goldenFoodText.text = "Golden - Extra points when collected.";
        if (bombFoodText != null) bombFoodText.text = "Bomb - Ends the game if collected.";
        if (shrinkFoodText != null) shrinkFoodText.text = "Shrink - Reduces snake length.";
        if (speedFoodText != null) speedFoodText.text = "Speed - Temporarily increases snake speed.";
        if (slowFoodText != null) slowFoodText.text = "Slow - Temporarily decreases snake speed.";
        if (ghostFoodText != null) ghostFoodText.text = "Ghost - Temporarily allows passing through walls/obstacles.";
        if (shieldFoodText != null) shieldFoodText.text = "Shield - Protects from one collision.";

        // Initialize icons
        RefreshMusicIcon();
        RefreshHapticsIcon();

        // Show main menu
        ShowMainMenuUI();
    }

    private void Update()
    {
        if (!timerActive) return;
        timerRemaining -= Time.deltaTime;
        if (specialTimerFill) specialTimerFill.fillAmount = Mathf.Clamp01(timerRemaining / timerDuration);
        if (timerRemaining <= 0f) StopSpecialTimer();
    }

    #region Main Menu

    public void ShowMainMenuUI()
    {
        SetOnlyActive(mainMenuPanel);
        RefreshContinueVisibility();
        Time.timeScale = 1f;
    }

    public void HideMainMenuPanel()
    {
        mainMenuPanel?.SetActive(false);
    }

    private void RefreshContinueVisibility()
    {
        if (continueButton != null) continueButton.gameObject.SetActive(PlayerPrefs.GetInt("HasSave", 0) == 1);
    }

    #endregion

    #region Difficulty

    public void ShowDifficultyPanel()
    {
        SetOnlyActive(difficultyPanel);
        UpdateDifficultyUI();
    }

    public void HideDifficultyPanel()
    {
        difficultyPanel?.SetActive(false);
        ShowMainMenuUI();
    }

    private void CycleDifficulty(int delta)
    {
        int d = (int)selectedDifficulty;
        d = (d + delta) % 4;
        if (d < 0) d += 4;
        selectedDifficulty = (UIDifficulty)d;
        // Map UI enum to GameManager's global Difficulty enum
        GameManager.Instance.SetDifficulty((Difficulty)selectedDifficulty);
        UpdateDifficultyUI();
    }

    private void UpdateDifficultyUI()
    {
        if (difficultyNameText != null) difficultyNameText.text = selectedDifficulty.ToString();
        if (difficultyDescriptionText != null)
        {
            string desc = "";
            switch (selectedDifficulty)
            {
                case UIDifficulty.Easy:
                    desc = "Perfect for beginners.\n• Slow snake\n• More Golden Foods";
                    break;
                case UIDifficulty.Normal:
                    desc = "Balanced gameplay.\nRecommended for most players.";
                    break;
                case UIDifficulty.Hard:
                    desc = "Faster snake.\nBombs appear earlier.";
                    break;
                case UIDifficulty.Extreme:
                    desc = "Maximum speed.\nFor experienced players.";
                    break;
            }
            difficultyDescriptionText.text = desc;
        }
    }

    #endregion

    #region How To Play

    public void ShowHowToPlayPanel()
    {
        SetOnlyActive(howToPlayPanel);
        if (howToPlayScrollRect != null) howToPlayScrollRect.verticalNormalizedPosition = 1f;
    }

    public void HideHowToPlayPanel()
    {
        howToPlayPanel?.SetActive(false);
        ShowMainMenuUI();
    }

    #endregion

    #region Gameplay

    public void ShowGameplayUI()  => gameplayPanel?.SetActive(true);
    public void HideGameplayUI()  => gameplayPanel?.SetActive(false);

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

    #endregion

    #region Pause

    public void ShowPausePanel()
    {
        gameplayPanel?.SetActive(false);
        pausePanel?.SetActive(true);
    }

    public void HidePausePanel()
    {
        pausePanel?.SetActive(false);
    }

    #endregion

    #region Game Over

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

    public void HideGameOverUI()
    {
        gameOverPanel?.SetActive(false);
    }

    #endregion

    #region Settings

    private const string MusicMutedKey = "MusicMuted";
    private const string MusicVolumeBackupKey = "MusicVolumeBackup";

    private void ToggleMusic()
    {
        if (AudioManager.Instance == null) return;
        bool currentlyMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        if (currentlyMuted)
        {
            float backup = PlayerPrefs.GetFloat(MusicVolumeBackupKey, 0.5f);
            AudioManager.Instance.SetMusicVolume(backup);
            PlayerPrefs.SetInt(MusicMutedKey, 0);
        }
        else
        {
            float saved = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            PlayerPrefs.SetFloat(MusicVolumeBackupKey, saved);
            AudioManager.Instance.SetMusicVolume(0f);
            PlayerPrefs.SetInt(MusicMutedKey, 1);
        }
        PlayerPrefs.Save();
        RefreshMusicIcon();
    }

    private void RefreshMusicIcon()
    {
        bool muted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        if (musicToggleIcon != null)
            musicToggleIcon.sprite = muted ? musicOffSprite : musicOnSprite;
    }

    private void ToggleHaptics()
    {
        if (HapticManager.Instance != null)
        {
            bool newState = !HapticManager.Instance.IsEnabled;
            HapticManager.Instance.IsEnabled = newState;
        }
        else
        {
            int val = PlayerPrefs.GetInt("HapticsEnabled", 1);
            PlayerPrefs.SetInt("HapticsEnabled", val == 1 ? 0 : 1);
            PlayerPrefs.Save();
        }
        RefreshHapticsIcon();
    }

    private void RefreshHapticsIcon()
    {
        bool enabled = HapticManager.Instance != null ? HapticManager.Instance.IsEnabled : PlayerPrefs.GetInt("HapticsEnabled", 1) == 1;
        if (hapticsToggleIcon != null)
            hapticsToggleIcon.sprite = enabled ? hapticsOnSprite : hapticsOffSprite;
    }

    #endregion

    #region Timer

    public void StartSpecialTimer(float duration, Color color)
    {
        timerDuration = timerRemaining = duration;
        timerActive = true;
        specialTimerFill.transform.parent.gameObject.SetActive(true);
        if (specialTimerFill) { specialTimerFill.fillAmount = 1f; specialTimerFill.color = color; }
    }

    public void StopSpecialTimer()
    {
        timerActive = false;
        specialTimerFill.transform.parent.gameObject.SetActive(false);
    }

    #endregion

    #region Status Indicators

    public void ShowShieldIndicator(bool on) => shieldIcon?.SetActive(on);
    public void ShowGhostIndicator(bool on)  => ghostIcon?.SetActive(on);

    #endregion

    #region Hover

    private void RegisterHover(Button btn)
    {
        if (btn == null) return;
        AddHoverEvents(btn);
    }

    private void SetButtonSprite(Button button, Sprite sprite)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null) image.sprite = sprite;
    }

    private void AddHoverEvents(Button button)
    {
        if (button == null) return;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
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

    #endregion

    #region Utilities

    private void SetOnlyActive(GameObject toShow)
    {
        mainMenuPanel?.SetActive(false);
        difficultyPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
        gameplayPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        specialTimerPanel?.SetActive(false);

        toShow?.SetActive(true);
    }

    #endregion
}
