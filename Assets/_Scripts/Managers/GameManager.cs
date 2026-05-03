using UnityEngine;
using System.Collections.Generic;

public enum GameState { MainMenu, Playing, Paused, GameOver }
public enum Difficulty  { Easy, Normal, Hard, Extreme }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    public SnakeController snake;
    public FoodSpawner spawner;
    public ScoreManager scoreManager;
    public UIManager uiManager;

    [Header("Difficulty Speed Profiles (moveRate)")]
    [SerializeField] private float easySpeed    = 0.28f;
    [SerializeField] private float normalSpeed  = 0.20f;
    [SerializeField] private float hardSpeed    = 0.14f;
    [SerializeField] private float extremeSpeed = 0.08f;

    public GameState CurrentState { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GoToMainMenu();
    }

    // ─── Difficulty ───────────────────────────────
    public void SetDifficulty(Difficulty d)
    {
        CurrentDifficulty = d;
        if (snake != null)
        {
            snake.moveRate = d switch
            {
                Difficulty.Easy    => easySpeed,
                Difficulty.Hard    => hardSpeed,
                Difficulty.Extreme => extremeSpeed,
                _                  => normalSpeed,
            };
        }
    }

    // ─── Game Flow ────────────────────────────────
    public void StartNewGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (snake != null) snake.gameObject.SetActive(true);
        SetDifficulty(CurrentDifficulty);
        scoreManager?.SetScore(0);
        spawner?.ClearAllFood();
        snake?.RestoreState(Vector2Int.zero, Vector2Int.right, new List<Vector2Int>());
        spawner?.SpawnNormalFood();

        uiManager?.HideMainMenuPanel();
        uiManager?.HideGameOverUI();
        uiManager?.HidePausePanel();
        uiManager?.ShowGameplayUI();

        PlayerPrefs.SetInt("HasSave", 1);
        PlayerPrefs.Save();

        AudioManager.Instance?.PlayGameMusic();
    }

    public void ContinueGame()
    {
        if (PlayerPrefs.GetInt("HasSave", 0) != 1) return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        uiManager?.HideMainMenuPanel();
        uiManager?.HideGameOverUI();
        uiManager?.HidePausePanel();
        uiManager?.ShowGameplayUI();
        if (snake != null) snake.gameObject.SetActive(true);
        LoadGame();
        AudioManager.Instance?.PlayGameMusic();
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        SaveGame();
        uiManager?.ShowPausePanel();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        uiManager?.HidePausePanel();
    }

    public void GameOver()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;

        scoreManager?.FinalizeScore();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeath();
        if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.Shake(0.5f, 0.2f);
        if (HapticManager.Instance != null) HapticManager.Instance.Heavy();

        uiManager?.ShowGameOverUI();

        SaveSystem.DeleteSave("snake_save");
        PlayerPrefs.SetInt("HasSave", 0);
        PlayerPrefs.Save();

        StartCoroutine(ReturnToMenuAfterDelay(3.5f));
    }

    private System.Collections.IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        CurrentState = GameState.MainMenu;
        Time.timeScale = 1f;
        AudioManager.Instance?.PlayMenuMusic();
        uiManager?.ShowMainMenuUI();

        // Hide only the snake (FoodSpawner is on same GO as GameManager, can't hide it)
        if (snake != null) snake.gameObject.SetActive(false);
        spawner?.ClearAllFood();
    }

    public void ExitGame() => Application.Quit();

    // ─── Save / Load ──────────────────────────────
    public void SaveGame()
    {
        var data = new SnakeSaveData
        {
            score         = scoreManager.CurrentScore,
            headPosition  = snake.GridPosition,
            direction     = snake.MoveDirection,
            bodyPositions = new List<Vector2Int>(snake.BodyPositions),
            foodPosition  = spawner.CurrentFoodGridPosition,
        };
        SaveSystem.Save(data, "snake_save");
    }

    public void LoadGame()
    {
        if (!SaveSystem.SaveExists("snake_save")) return;
        var data = SaveSystem.Load<SnakeSaveData>("snake_save");
        scoreManager?.SetScore(data.score);
        snake?.RestoreState(data.headPosition, data.direction, data.bodyPositions);
        spawner?.RestoreFood(data.foodPosition);
    }

    void OnApplicationQuit()  { if (CurrentState == GameState.Playing) SaveGame(); }
    void OnApplicationPause(bool paused) { if (paused && CurrentState == GameState.Playing) SaveGame(); }
}
