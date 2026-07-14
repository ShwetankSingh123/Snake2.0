using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("References")]
    public SnakeController snake;

    [Header("Normal Food")]
    public GameObject foodPrefab;

    [Header("Special Food Prefabs")]
    [SerializeField] private GameObject goldenFoodPrefab;
    [SerializeField] private GameObject bombFoodPrefab;
    [SerializeField] private GameObject shrinkFoodPrefab;
    [SerializeField] private GameObject speedFoodPrefab;
    [SerializeField] private GameObject slowFoodPrefab;
    [SerializeField] private GameObject ghostFoodPrefab;
    [SerializeField] private GameObject shieldFoodPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int maxAttempts = 4096;
    [SerializeField] private int normalFoodsBeforeSpecial = 4;

    private GameObject normalFood;
    private List<GameObject> activeSpecials = new List<GameObject>();
    private int normalFoodsEaten = 0;

    // Weighted special pool — rotate through but skip Bomb until score is high enough
    private FoodType[] specialPool = { FoodType.Golden, FoodType.Speed, FoodType.Ghost, FoodType.Shield, FoodType.Slow, FoodType.Shrink, FoodType.Bomb };
    private int poolIndex = 0;

    public Vector2Int CurrentFoodGridPosition { get; private set; }

    void Awake()
    {
        if (snake == null) snake = FindAnyObjectByType<SnakeController>();
    }

    void Start()
    {
        // GameManager controls food spawning via StartNewGame/ContinueGame
        // Don't auto-spawn here to avoid duplicates
        if (Board.Instance == null) { Debug.LogError("[FoodSpawner] Board.Instance is null."); return; }
    }

    public void SpawnNormalFood()
    {
        Debug.Log($"[FoodSpawner] SpawnNormalFood called. snake={snake}, foodPrefab={foodPrefab}, Board={Board.Instance}");
        if (snake == null) { Debug.LogError("[FoodSpawner] snake is NULL!"); return; }
        if (foodPrefab == null) { Debug.LogError("[FoodSpawner] foodPrefab is NULL!"); return; }
        if (Board.Instance == null) { Debug.LogError("[FoodSpawner] Board.Instance is NULL!"); return; }
        if (normalFood != null) Destroy(normalFood);
        Vector2Int cell = FindFreeCell();
        normalFood = Instantiate(foodPrefab, snake.GridToWorld(cell), Quaternion.identity);
        normalFood.tag = "Food";
        var food = normalFood.GetComponent<Food>();
        if (food) { food.type = FoodType.Normal; food.lifeTime = 0f; }
        CurrentFoodGridPosition = cell;
        Debug.Log($"[FoodSpawner] Spawned food at {cell}, normalFood={normalFood}");
        if (FoodVFX.Instance != null) FoodVFX.Instance.PlaySpawn(FoodType.Normal, snake.GridToWorld(cell));
    }

    public void RestoreFood(Vector2Int pos)
    {
        if (normalFood != null) Destroy(normalFood);
        CurrentFoodGridPosition = pos;
        normalFood = Instantiate(foodPrefab, snake.GridToWorld(pos), Quaternion.identity);
        normalFood.tag = "Food";
        var food = normalFood.GetComponent<Food>();
        if (food) { food.type = FoodType.Normal; food.lifeTime = 0f; }
    }

    public void OnNormalFoodEaten()
    {
        normalFoodsEaten++;
        if (normalFoodsEaten >= normalFoodsBeforeSpecial)
        {
            normalFoodsEaten = 0;
            SpawnNextSpecial();
        }
    }

    private void SpawnNextSpecial()
    {
        Debug.Log($"[FoodSpawner] SpawnNextSpecial called. poolIndex={poolIndex}, specialPool.Length={specialPool.Length}");
        FoodType next = specialPool[poolIndex % specialPool.Length];
        poolIndex++;

        // Don't spawn Bomb until score > 15
        if (next == FoodType.Bomb && ScoreManager.Instance.CurrentScore < 15)
        {
            next = FoodType.Golden;
        }

        SpawnSpecial(next);
    }

    private void SpawnSpecial(FoodType type)
    {
        Debug.Log($"[FoodSpawner] SpawnSpecial called. type= {type}");

        GameObject prefab = type switch
        {
            FoodType.Golden => goldenFoodPrefab,
            FoodType.Bomb   => bombFoodPrefab,
            FoodType.Shrink => shrinkFoodPrefab,
            FoodType.Speed  => speedFoodPrefab,
            FoodType.Slow   => slowFoodPrefab,
            FoodType.Ghost  => ghostFoodPrefab,
            FoodType.Shield => shieldFoodPrefab,
            _               => null,
        };


        if (prefab == null)
        {
            Debug.LogError($"[FoodSpawner] No prefab assigned for FoodType {type}. Cannot spawn special food.");
            return;
        } 
            
        Vector2Int cell = FindFreeCell();
        GameObject special = Instantiate(prefab, snake.GridToWorld(cell), Quaternion.identity);
        special.tag = "Food";

        var food = special.GetComponent<Food>();
        if (food) { food.type = type; food.lifeTime = 6f; }

        activeSpecials.Add(special);
        if (FoodVFX.Instance != null) FoodVFX.Instance.PlaySpawn(type, snake.GridToWorld(cell));
    }

    public void NotifySpecialDestroyed(GameObject special) => activeSpecials.Remove(special);

    public void ClearAllFood()
    {
        if (normalFood != null) Destroy(normalFood);
        foreach (var s in activeSpecials) if (s != null) Destroy(s);
        activeSpecials.Clear();
        normalFoodsEaten = 0;
        poolIndex = 0;
    }

    private Vector2Int FindFreeCell()
    {
        var b = Board.Instance;
        var occupied = new HashSet<Vector2Int>(snake.OccupiedCells());
        Vector2Int cell;
        int guard = 0;
        do
        {
            cell = new Vector2Int(Random.Range(b.minX, b.maxX + 1), Random.Range(b.minY, b.maxY + 1));
            guard++;
            if (guard > maxAttempts) break;
        } while (occupied.Contains(cell));
        return cell;
    }
}
