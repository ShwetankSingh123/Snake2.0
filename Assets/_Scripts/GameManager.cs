using UnityEngine;
using System.Collections.Generic;
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

    public void SaveGame()
    {
        SnakeSaveData data = new SnakeSaveData();
        data.score = scoreManager.CurrentScore;
        data.headPosition = snake.GridPosition;
        data.direction = snake.MoveDirection;
        data.bodyPositions = new List<Vector2Int>(snake.BodyPositions);
        data.foodPosition = spawner.CurrentFoodGridPosition;

        SaveSystem.Save(data, "snake_save");
    }

    public void LoadGame()
    {
        if (!SaveSystem.SaveExists("snake_save")) return;

        SnakeSaveData data = SaveSystem.Load<SnakeSaveData>("snake_save");

        scoreManager.SetScore(data.score);
        snake.RestoreState(data.headPosition, data.direction, data.bodyPositions);
        spawner.RestoreFood(data.foodPosition);
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
