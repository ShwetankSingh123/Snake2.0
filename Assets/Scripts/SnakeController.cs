using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // new system

public class SnakeController : MonoBehaviour
{
    [Header("Snake Settings")]
    public float moveRate = 0.2f;
    public int cellSize = 1;             // match prefab scale
    public Transform bodyPrefab;

    private float moveTimer;
    public Vector2Int gridMoveDir = Vector2Int.right;
    public Vector2Int gridPosition;

    private List<Transform> snakeBody = new List<Transform>();
    private SnakeControls controls;

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
        gridPosition = WorldToGrid(transform.position);
        moveTimer = moveRate;
        snakeBody.Clear();
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); // reload scene
        }

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            moveTimer += moveRate;
            Move();
        }
    }

    void Move()
    {
        Vector3 oldHeadWorld = transform.position;

        // move in grid
        gridPosition += gridMoveDir;

        // wrap grid position inside board bounds
        WrapWithinBounds();

        // place head at wrapped grid position
        transform.position = GridToWorld(gridPosition);

        // move body
        for (int i = 0; i < snakeBody.Count; i++)
        {
            Vector3 temp = snakeBody[i].position;
            snakeBody[i].position = oldHeadWorld;
            oldHeadWorld = temp;
        }
    }

    void WrapWithinBounds()
    {
        var b = Board.Instance;

        if (gridPosition.x > b.maxX) gridPosition.x = b.minX;
        else if (gridPosition.x < b.minX) gridPosition.x = b.maxX;

        if (gridPosition.y > b.maxY) gridPosition.y = b.minY;
        else if (gridPosition.y < b.minY) gridPosition.y = b.maxY;
    }

    public void Grow()
    {
        Transform newPart = Instantiate(bodyPrefab);
        newPart.position = snakeBody.Count > 0 ? snakeBody[snakeBody.Count - 1].position : transform.position;
        snakeBody.Add(newPart);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Food"))
        {
            Grow();
            Destroy(other.gameObject);
            FindObjectOfType<FoodSpawner>().SpawnFood();
            ScoreManager.Instance.AddScore(1);
        }

        if (other.CompareTag("Wall") || other.CompareTag("SnakeBody"))
        {
            Debug.Log("Game Over!");
            ScoreManager.Instance.GameOver();
            // TODO: restart logic
        }
    }

    // ===== Helpers for FoodSpawner =====
    public Vector3 GridToWorld(Vector2Int gp) => new Vector3(gp.x * cellSize, gp.y * cellSize, 0);

    public Vector2Int WorldToGrid(Vector3 wp) =>
        new Vector2Int(Mathf.RoundToInt(wp.x / cellSize), Mathf.RoundToInt(wp.y / cellSize));

    public IEnumerable<Vector2Int> OccupiedCells()
    {
        yield return gridPosition; // head
        foreach (var t in snakeBody)
            yield return WorldToGrid(t.position);
    }
}
