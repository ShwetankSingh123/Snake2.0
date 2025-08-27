using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class SnakeController : MonoBehaviour
{
    [Header("Snake Settings")]
    public float moveRate = 0.2f;          // time between steps
    public int cellSize = 3;               // grid cell/world step size
    public Transform bodyPrefab;           // segment prefab (SpriteRenderer + Collider2D (isTrigger))

    [Header("State (read-only)")]
    public Vector2Int gridPosition;        // head grid position (for debug/inspector)
    public Vector2Int gridMoveDir = Vector2Int.right;

    private float moveTimer;
    private List<Transform> snakeBody = new List<Transform>();
    private SnakeControls controls;

    // stores where the tail USED to be before the latest move (so we can grow there safely)
    private Vector3 lastTailPrevWorldPos;

    void Awake()
    {
        controls = new SnakeControls();
        controls.Snake.Up.performed += ctx => { if (gridMoveDir != Vector2Int.down) gridMoveDir = Vector2Int.up; };
        controls.Snake.Down.performed += ctx => { if (gridMoveDir != Vector2Int.up) gridMoveDir = Vector2Int.down; };
        controls.Snake.Left.performed += ctx => { if (gridMoveDir != Vector2Int.right) gridMoveDir = Vector2Int.left; };
        controls.Snake.Right.performed += ctx => { if (gridMoveDir != Vector2Int.left) gridMoveDir = Vector2Int.right; };
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        // snap head to grid
        gridPosition = WorldToGrid(transform.position);
        transform.position = GridToWorld(gridPosition);

        moveTimer = moveRate;
        snakeBody.Clear();
        lastTailPrevWorldPos = transform.position;
    }

    void Update()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            moveTimer += moveRate;
            Step();
        }
    }

    private void Step()
    {
        // remember old head world pos for body follow
        Vector3 oldHeadWorld = transform.position;

        // advance head in grid
        gridPosition += gridMoveDir;

        // wrap inside board bounds
        WrapWithinBounds();

        // place head at new/wrapped grid cell
        transform.position = GridToWorld(gridPosition);

        // move body segments to follow
        Vector3 prev = oldHeadWorld;
        for (int i = 0; i < snakeBody.Count; i++)
        {
            Vector3 temp = snakeBody[i].position;
            snakeBody[i].position = prev;
            prev = temp;
        }

        // after the loop, 'prev' holds the previous position of the LAST segment (the tail)
        lastTailPrevWorldPos = prev;
    }

    private void WrapWithinBounds()
    {
        var b = Board.Instance;
        if (b == null) return;

        if (gridPosition.x > b.maxX) gridPosition.x = b.minX;
        else if (gridPosition.x < b.minX) gridPosition.x = b.maxX;

        if (gridPosition.y > b.maxY) gridPosition.y = b.minY;
        else if (gridPosition.y < b.minY) gridPosition.y = b.maxY;
    }

    public void Grow()
    {
        // spawn new segment where the tail WAS last step (now empty space)
        Transform newPart = Instantiate(bodyPrefab, lastTailPrevWorldPos, Quaternion.identity);

        // make sure it's tagged and briefly non-colliding to avoid instant self-hit
        newPart.gameObject.tag = "SnakeBody";
        var col = newPart.GetComponent<Collider2D>();
        if (col) col.enabled = false;           // disable for one frame
        StartCoroutine(EnableColliderNextFrame(col));

        snakeBody.Add(newPart);
    }

    private System.Collections.IEnumerator EnableColliderNextFrame(Collider2D col)
    {
        yield return null;                       // wait 1 frame
        if (col) col.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Handle food first
        if (other.CompareTag("Food"))
        {
            Food food = other.GetComponent<Food>();
            if (food != null)
            {
                // Score
                ScoreManager.Instance.AddScore(food.GetScore());

                // Growth
                for (int i = 0; i < food.GetGrowth(); i++)
                {
                    Grow();
                }

                // Bomb logic (optional: instant game over)
                if (food.type == FoodType.Bomb)
                {
                    ScoreManager.Instance.GameOver();
                }

                // Respawn only if it was NORMAL
                if (food.type == FoodType.Normal)
                {
                    FindObjectOfType<FoodSpawner>().SpawnNormalFood();
                }
            }

            Destroy(other.gameObject);
            //FindObjectOfType<FoodSpawner>().SpawnNormalFood();
            return;
        }

        // Self or wall collision
        if (other.CompareTag("SnakeBody") || other.CompareTag("Wall"))
        {
            // If you still ever get a same-frame false hit, you can early-out here
            // if (Time.time - lastEatTime < 0.02f) return;
            var score = ScoreManager.Instance;
            if (score) score.GameOver();
            else Debug.Log("Game Over!");
        }
    }

    // ===== Helpers used by FoodSpawner =====
    public Vector3 GridToWorld(Vector2Int gp) => new Vector3(gp.x * cellSize, gp.y * cellSize, 0f);

    public Vector2Int WorldToGrid(Vector3 wp) =>
        new Vector2Int(Mathf.RoundToInt(wp.x / cellSize), Mathf.RoundToInt(wp.y / cellSize));

    public IEnumerable<Vector2Int> OccupiedCells()
    {
        yield return gridPosition; // head
        foreach (var t in snakeBody)
            yield return WorldToGrid(t.position);
    }
}
