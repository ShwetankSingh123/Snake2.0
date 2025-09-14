using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[Serializable]
public class SnakeSaveData
{
    public int score;
    public Vector2Int headPosition;
    public Vector2Int direction;
    public List<Vector2Int> bodyPositions;
    public Vector2Int foodPosition;
}

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

    [SerializeField] private Sprite headSprite;
    [SerializeField] private Sprite bodySprite;
    [SerializeField] private Sprite tailSprite;
    [SerializeField] private Sprite headNormal;
    [SerializeField] private Sprite headTongue;
    [SerializeField] private Sprite headBlink;

    private float animationTimer;
    private SpriteRenderer headSR;

    public Vector2Int GridPosition => gridPosition;
    public Vector2Int MoveDirection => gridMoveDir;
    public List<Vector2Int> BodyPositions
    {
        get
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            foreach (var t in snakeBody)
                positions.Add(WorldToGrid(t.position));
            return positions;
        }
    }




    void Awake()
    {
        headSR = GetComponent<SpriteRenderer>();

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

        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0f)
        {
            int r = UnityEngine.Random.Range(0, 10); // 10% chance
            if (r < 2) headSR.sprite = headTongue;
            else if (r < 4) headSR.sprite = headBlink;
            else headSR.sprite = headNormal;

            animationTimer = UnityEngine.Random.Range(0.1f, 0.3f); // every 0.5–2 sec
        }
    }

    public void RestoreState(Vector2Int headPos, Vector2Int direction, List<Vector2Int> bodyPos)
    {
        gridPosition = headPos;
        gridMoveDir = direction;

        transform.position = GridToWorld(gridPosition);

        // Clear old body
        foreach (var part in snakeBody)
            Destroy(part.gameObject);
        snakeBody.Clear();

        // Rebuild body from saved positions
        foreach (var pos in bodyPos)
        {
            Transform newPart = Instantiate(bodyPrefab, GridToWorld(pos), Quaternion.identity);
            newPart.gameObject.tag = "SnakeBody";
            snakeBody.Add(newPart);
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

        UpdateSnakeVisuals();
    }

    private void UpdateSnakeVisuals()
    {
        //  Head is always this GameObject
        headSR.sprite = headSprite;

        if (gridMoveDir == Vector2Int.up) headSR.transform.rotation = Quaternion.Euler(0, 0, 180);
        else if (gridMoveDir == Vector2Int.right) headSR.transform.rotation = Quaternion.Euler(0, 0, 90);
        else if (gridMoveDir == Vector2Int.down) headSR.transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (gridMoveDir == Vector2Int.left) headSR.transform.rotation = Quaternion.Euler(0, 0, -90);

        for (int i = 0; i < snakeBody.Count; i++)
        {
            SpriteRenderer sr = snakeBody[i].GetComponent<SpriteRenderer>();

            if (i == snakeBody.Count - 1)
            {
                // --- TAIL ---
                sr.sprite = tailSprite;

                // Determine direction of tail
                Vector3 tailPos = snakeBody[i].position;
                Vector3 prevPos = (i > 0) ? snakeBody[i - 1].position : transform.position;

                Vector3 dir = (prevPos - tailPos).normalized;

                float z = 0f;
                if (dir == Vector3.up) z = 180f;
                else if (dir == Vector3.right) z = 90f;
                else if (dir == Vector3.down) z = 0f;
                else if (dir == Vector3.left) z = -90f;

                snakeBody[i].rotation = Quaternion.Euler(0f, 0f, z);
            }
            else
            {
                // --- BODY ---
                sr.sprite = bodySprite;
                snakeBody[i].rotation = Quaternion.identity;
            }
        }
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
                StartCoroutine(PopEffect(transform)); // head pops
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

    private IEnumerator PopEffect(Transform target)
    {
        Vector3 original = target.localScale;
        Vector3 enlarged = original * 1.2f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            target.localScale = Vector3.Lerp(original, enlarged, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        target.localScale = original;
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
