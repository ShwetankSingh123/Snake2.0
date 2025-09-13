using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    public SnakeController snake;
    public FoodSpawner spawner;
    public ScoreManager scoreManager;
    public UIManager uiManager;

    private bool isGameOver = false;
    private GameState currentState;
    private bool hasSave = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSave();
        GoToMainMenu();
    }

    private void LoadSave()
    {
        hasSave = PlayerPrefs.HasKey("HasSave") && PlayerPrefs.GetInt("HasSave") == 1;
    }

    public void StartNewGame()
    {
        PlayerPrefs.SetInt("HasSave", 1);
        PlayerPrefs.Save();

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        if (!hasSave) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void GameOver()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.GameOver;
        Time.timeScale = 0f;

        if (uiManager != null)
            uiManager.ShowGameOverUI();

        // After 3 sec -> return to menu
        Invoke(nameof(GoToMainMenuAfterGameOver), 3f);
    }

    private void GoToMainMenuAfterGameOver()
    {
        PlayerPrefs.SetInt("HasSave", 0);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        currentState = GameState.MainMenu;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
