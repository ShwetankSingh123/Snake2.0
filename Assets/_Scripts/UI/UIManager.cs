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

    [Header("Background")]
    [SerializeField] private GameObject background;

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

    // Hover logic removed: buttons keep their normal sprite

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Hover handling removed - buttons keep their normal sprite
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
        if (continueButton) continueButton.onButtonClick.AddListener(() => { GameManager.Instance.ContinueGame(); RefreshContinueButton(); });
        if (newGameButton)  newGameButton.onButtonClick.AddListener(ShowDifficultyPanel);
        if (howToPlayButton) howToPlayButton.onButtonClick.AddListener(ShowHowToPlayPanel);
        if (exitButton)     exitButton.onButtonClick.AddListener(() => GameManager.Instance.ExitGame());

        // Difficulty panel bindings
        if (diffLeftButton) diffLeftButton.onButtonClick.AddListener(() => CycleDifficulty(-1));
        if (diffRightButton) diffRightButton.onButtonClick.AddListener(() => CycleDifficulty(1));
        if (difficultyStartButton) difficultyStartButton.onButtonClick.AddListener(() => OnDifficultyStartPressed());
        if (difficultyBackButton) difficultyBackButton.onButtonClick.AddListener(HideDifficultyPanel);

        // HowToPlay
        if (howToPlayBackButton) howToPlayBackButton.onButtonClick.AddListener(HideHowToPlayPanel);

        // Pause bindings
        if (resumeButton) resumeButton.onButtonClick.AddListener(() => GameManager.Instance.ResumeGame());
        if (mainMenuFromPauseButton) mainMenuFromPauseButton.onButtonClick.AddListener(() => GameManager.Instance.GoToMainMenu());

        // Music & Haptics toggles
        if (musicToggleButton) musicToggleButton.onButtonClick.AddListener(ToggleMusic);
        if (hapticsToggleButton) hapticsToggleButton.onButtonClick.AddListener(ToggleHaptics);

        // Initialize difficulty from GameManager
        if (GameManager.Instance != null) selectedDifficulty = (UIDifficulty)GameManager.Instance.CurrentDifficulty;
        UpdateDifficultyUI();

        // Restore Music and Haptics settings
        bool musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        if (AudioManager.Instance != null)
        {
            if (musicEnabled)
            {
                float vol = PlayerPrefs.GetFloat("MusicVolume", AudioManager.Instance.musicVolume);
                AudioManager.Instance.SetMusicVolume(vol);
            }
            else
            {
                // save current as backup then mute
                float cur = PlayerPrefs.GetFloat("MusicVolume", AudioManager.Instance.musicVolume);
                PlayerPrefs.SetFloat("MusicVolumeBackup", cur);
                AudioManager.Instance.SetMusicVolume(0f);
            }
        }
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);

        bool hapticsEnabled = PlayerPrefs.GetInt("HapticsEnabled", 1) == 1;
        if (HapticManager.Instance != null)
            HapticManager.Instance.IsEnabled = hapticsEnabled;
        PlayerPrefs.SetInt("HapticsEnabled", hapticsEnabled ? 1 : 0);

        // Initialize icons

        // Ensure Continue button reflects saved state
        RefreshContinueButton();

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
        RefreshHapticsIcon(); // Update haptics icon

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
        // Reset shared menu state then show main menu
        ResetToMenu();
        mainMenuPanel?.SetActive(true);
        ShowBackground();
        Time.timeScale = 1f;
    }

    public void HideMainMenuPanel()
    {
        mainMenuPanel?.SetActive(false);
        // background handled by other show methods
    }

    public void RefreshContinueButton()
    {
        if (continueButton != null) continueButton.gameObject.SetActive(PlayerPrefs.GetInt("HasSave", 0) == 1);
    }

    #endregion

    #region Difficulty

    public void ShowDifficultyPanel()
    {
        // Keep UI selection in sync with GameManager
        if (GameManager.Instance != null) selectedDifficulty = (UIDifficulty)GameManager.Instance.CurrentDifficulty;
        HideAllPanels();
        difficultyPanel?.SetActive(true);
        ShowBackground();
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
        if (GameManager.Instance != null) GameManager.Instance.SetDifficulty((Difficulty)selectedDifficulty);
        UpdateDifficultyUI();
    }

    private void OnDifficultyStartPressed()
    {
        // Apply selected difficulty to GameManager then start
        if (GameManager.Instance != null) GameManager.Instance.SetDifficulty((Difficulty)selectedDifficulty);
        HideDifficultyPanel();
        if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
        RefreshContinueButton();
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
        HideAllPanels();
        howToPlayPanel?.SetActive(true);
        ShowBackground();
        if (howToPlayScrollRect != null) howToPlayScrollRect.verticalNormalizedPosition = 1f;
    }

    public void HideHowToPlayPanel()
    {
        howToPlayPanel?.SetActive(false);
        ShowMainMenuUI();
    }

    #endregion

    #region Gameplay

    public void ShowGameplayUI()
    {
        // Hide menu background during gameplay
        HideBackground();
        gameplayPanel?.SetActive(true);
    }

    public void HideGameplayUI()
    {
        gameplayPanel?.SetActive(false);
    }

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
        // Pause should hide gameplay background but not show menu background
        gameplayPanel?.SetActive(false);
        HideBackground();
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
        ShowBackground();
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

    private const string MusicEnabledKey = "MusicEnabled";
    private const string MusicVolumeBackupKey = "MusicVolumeBackup";
    private const string HapticsEnabledKey = "HapticsEnabled";

    private void ToggleMusic()
    {
        if (AudioManager.Instance == null) return;
        bool enabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        // toggle
        if (enabled)
        {
            // turn off: backup current volume then mute
            PlayerPrefs.SetFloat(MusicVolumeBackupKey, PlayerPrefs.GetFloat("MusicVolume", AudioManager.Instance.musicVolume));
            AudioManager.Instance.SetMusicVolume(0f);
            PlayerPrefs.SetInt(MusicEnabledKey, 0);
        }
        else
        {
            // turn on: restore default music volume
            AudioManager.Instance.SetMusicVolume(AudioManager.Instance.musicVolume);
            PlayerPrefs.SetInt(MusicEnabledKey, 1);
        }
        PlayerPrefs.Save();
        RefreshMusicIcon();
    }

    private void RefreshMusicIcon()
    {
        bool enabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        if (musicToggleIcon != null)
            musicToggleIcon.sprite = enabled ? musicOnSprite : musicOffSprite;
    }

    private void ToggleHaptics()
    {
        bool enabled = PlayerPrefs.GetInt(HapticsEnabledKey, 1) == 1;
        bool newState = !enabled;
        PlayerPrefs.SetInt(HapticsEnabledKey, newState ? 1 : 0);
        if (HapticManager.Instance != null) HapticManager.Instance.IsEnabled = newState;
        PlayerPrefs.Save();
        RefreshHapticsIcon();
    }

    private void RefreshHapticsIcon()
    {
        bool enabled = HapticManager.Instance != null ? HapticManager.Instance.IsEnabled : PlayerPrefs.GetInt(HapticsEnabledKey, 1) == 1;
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

    // Hover handling removed; buttons keep their normal sprite

    #region Utilities

    private void SetOnlyActive(GameObject toShow)
    {
        HideAllPanels();
        specialTimerPanel?.SetActive(false);
        toShow?.SetActive(true);
    }

    private void ShowBackground()
    {
        if (background != null) background.SetActive(true);
    }

    private void HideBackground()
    {
        if (background != null) background.SetActive(false);
    }

    private void HideAllPanels()
    {
        mainMenuPanel?.SetActive(false);
        difficultyPanel?.SetActive(false);
        howToPlayPanel?.SetActive(false);
        gameplayPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
    }

    public void ResetToMenu()
    {
        // Hide panels
        HideAllPanels();
        // Stop any active timer
        StopSpecialTimer();
        // Disable timer UI
        specialTimerPanel?.SetActive(false);
        // Reset scroll
        if (howToPlayScrollRect != null) howToPlayScrollRect.verticalNormalizedPosition = 1f;
        // Refresh continue button
        RefreshContinueButton();
        // Ensure other panels are closed
        pausePanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        gameplayPanel?.SetActive(false);
        // Unpause game
        Time.timeScale = 1f;
    }

    #endregion
}
