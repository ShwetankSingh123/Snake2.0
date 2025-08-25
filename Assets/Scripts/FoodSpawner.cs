using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SnakeController snake;     // drag your SnakeHead here (or it'll auto-find)
    public GameObject foodPrefab;     // assign in Inspector

    [Header("Spawn Settings")]
    [SerializeField] int maxAttempts = 4096;

    private GameObject currentFood;

    void Awake()
    {
        if (snake == null) snake = FindObjectOfType<SnakeController>();
        if (foodPrefab == null) Debug.LogError("[FoodSpawner] foodPrefab not assigned.");
    }

    void Start()
    {
        if (Board.Instance == null)
        {
            Debug.LogError("[FoodSpawner] Board.Instance is null. Add Board to the scene.");
            return;
        }

        SpawnFood(); // initial spawn
    }

    public void SpawnFood()
    {
        if (foodPrefab == null || snake == null || Board.Instance == null) return;

        if (currentFood != null) Destroy(currentFood);

        var b = Board.Instance;
        var occupied = new HashSet<Vector2Int>(snake.OccupiedCells());

        int width = b.maxX - b.minX + 1;
        int height = b.maxY - b.minY + 1;
        int freeCells = width * height - occupied.Count;
        if (freeCells <= 0)
        {
            Debug.LogWarning("[FoodSpawner] No free cells to spawn food.");
            return;
        }

        Vector2Int cell;
        int attempts = 0;
        do
        {
            cell = new Vector2Int(
                Random.Range(b.minX, b.maxX + 1),
                Random.Range(b.minY, b.maxY + 1)
            );
            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning($"[FoodSpawner] Max attempts reached ({attempts}).");
                return;
            }
        } while (occupied.Contains(cell));

        Vector3 pos = snake.GridToWorld(cell);
        currentFood = Instantiate(foodPrefab, pos, Quaternion.identity);
        currentFood.tag = "Food";
        // Optional: Debug.Log($"[FoodSpawner] Spawned at cell {cell} (world {pos}).");
    }
}
