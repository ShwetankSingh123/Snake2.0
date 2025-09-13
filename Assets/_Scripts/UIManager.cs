using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Special Food Timer UI")]
    public GameObject specialTimerPanel; // container panel
    public Image specialTimerFill;       // fill bar

    [Header("Main Menu UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    private float timerDuration;
    private float timerRemaining;
    private bool isActive = false;

    

    void Start()
    {
        if (specialTimerPanel != null)
            specialTimerPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);

            // Enable/disable Continue based on save
            bool hasSave = PlayerPrefs.HasKey("HasSave") && PlayerPrefs.GetInt("HasSave") == 1;
            if (continueButton != null)
                continueButton.gameObject.SetActive(hasSave);

            if (continueButton != null)
                continueButton.onClick.AddListener(() => GameManager.Instance.ContinueGame());
            if (newGameButton != null)
                newGameButton.onClick.AddListener(() => GameManager.Instance.StartNewGame());
            if (exitButton != null)
                exitButton.onClick.AddListener(() => GameManager.Instance.ExitGame());
        }
    }

    public void StartSpecialTimer(float duration, Color barColor)
    {
        timerDuration = duration;
        timerRemaining = duration;
        isActive = true;

        if (specialTimerPanel != null)
            specialTimerPanel.SetActive(true);

        if (specialTimerFill != null)
        {
            specialTimerFill.fillAmount = 1f;
            specialTimerFill.color = barColor;
        }
    }

    public void StopSpecialTimer()
    {
        isActive = false;
        if (specialTimerPanel != null)
            specialTimerPanel.SetActive(false);
    }

    // === Game Over UI ===
    public void ShowGameOverUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void HideGameOverUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        timerRemaining -= Time.deltaTime;

        if (specialTimerFill != null)
            specialTimerFill.fillAmount = Mathf.Clamp01(timerRemaining / timerDuration);

        if (timerRemaining <= 0f)
            StopSpecialTimer();
    }
}
