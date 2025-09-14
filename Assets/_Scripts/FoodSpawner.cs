using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Refs")]
    public SnakeController snake;     // drag your SnakeHead here (or it'll auto-find)

    [Header("Prefabs")]
    public GameObject foodPrefab;       // normal
    public GameObject goldenFoodPrefab; // golden
    public GameObject bombFoodPrefab;   // bomb

    [Header("Spawn Settings")]
    [SerializeField] int maxAttempts = 4096;

    private GameObject normalFood;   // always 1
    private List<GameObject> specials = new List<GameObject>(); // golden/bomb

    // This is the variable you were missing
    private GameObject currentFood;

    public Vector2Int CurrentFoodGridPosition { get; private set; }

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

        SpawnNormalFood(); // initial spawn
        InvokeRepeating(nameof(SpawnSpecialFood), 5f, 7f);
        // every 7s, try to spawn a special
    }

    // === Normal Food ===
    public void SpawnNormalFood()
    {
        if (normalFood != null) Destroy(normalFood);

        Vector2Int cell = FindFreeCell();
        normalFood = Instantiate(foodPrefab, snake.GridToWorld(cell), Quaternion.identity);
        normalFood.tag = "Food";

        var food = normalFood.GetComponent<Food>();
        if (food) { food.type = FoodType.Normal; food.lifeTime = 0f; } // permanent

        // Save the grid position so saves/restores can use it
        CurrentFoodGridPosition = cell;

        // Keep `currentFood` reference in sync (if other code expects it)
        currentFood = normalFood;
    }

    // === Specials (Golden / Bomb) ===
    private void SpawnSpecialFood()
    {
        // If a special already exists, skip
        specials.RemoveAll(item => item == null); // clean up destroyed refs
        if (specials.Count > 0) return;           //  don't spawn another

        // 30% chance nothing spawns this cycle
        if (Random.value < 0.3f) return;

        Vector2Int cell = FindFreeCell();

        GameObject prefab = (Random.value < 0.5f) ? goldenFoodPrefab : bombFoodPrefab;
        GameObject special = Instantiate(prefab, snake.GridToWorld(cell), Quaternion.identity);
        special.tag = "Food";

        var food = special.GetComponent<Food>();
        if (food)
        {
            food.type = (prefab == goldenFoodPrefab) ? FoodType.Golden : FoodType.Bomb;
            food.lifeTime = 5f; // disappear after 5s
        }

        specials.Add(special);
    }

    // Restore the normal food to a specific grid cell (used by save/load)
    public void RestoreFood(Vector2Int pos)
    {
        // destroy any existing normal food (we're restoring it)
        if (normalFood != null) Destroy(normalFood);

        CurrentFoodGridPosition = pos;
        Vector3 worldPos = snake.GridToWorld(pos);

        normalFood = Instantiate(foodPrefab, worldPos, Quaternion.identity);
        normalFood.tag = "Food";

        var food = normalFood.GetComponent<Food>();
        if (food) { food.type = FoodType.Normal; food.lifeTime = 0f; }

        // keep currentFood reference in sync if some other code reads it
        currentFood = normalFood;
    }

    // === Utility ===
    private Vector2Int FindFreeCell()
    {
        var b = Board.Instance;
        var occupied = new HashSet<Vector2Int>(snake.OccupiedCells());

        Vector2Int cell;
        int guard = 0;
        do
        {
            cell = new Vector2Int(
                Random.Range(b.minX, b.maxX + 1),
                Random.Range(b.minY, b.maxY + 1)
            );
            guard++;
            if (guard > maxAttempts) break;
        } while (occupied.Contains(cell));

        return cell;
    }

    public void NotifySpecialDestroyed(GameObject special)
    {
        specials.Remove(special);
    }
}
