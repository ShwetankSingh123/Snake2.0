using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public float moveRate = 0.2f;
    public int cellSize = 3;
    public Transform bodyPrefab;

    [Header("Speed Scaling")]
    [SerializeField] private float minMoveRate = 0.05f;
    [SerializeField] private float speedScoreStep = 10; // score points per speed increase
    [SerializeField] private float speedDecrement = 0.005f;

    [Header("Sprites")]
    [SerializeField] private Sprite headSprite;
    [SerializeField] private Sprite bodySprite;
    [SerializeField] private Sprite tailSprite;
    [SerializeField] private Sprite headNormal;
    [SerializeField] private Sprite headTongue;
    [SerializeField] private Sprite headBlink;

    [Header("State (read-only)")]
    public Vector2Int gridPosition;
    public Vector2Int gridMoveDir = Vector2Int.right;

    // Ghost mode (walk through self)
    [HideInInspector] public bool isGhost = false;
    // Shield (survive one wall/self hit)
    [HideInInspector] public bool hasShield = false;

    private float moveTimer;
    private float baseMoveRate;
    private List<Transform> snakeBody = new List<Transform>();
    private SnakeControls controls;
    private Vector3 lastTailPrevWorldPos;
    private float animationTimer;
    private SpriteRenderer headSR;
    private Vector2 touchStart;
    private bool isSwiping;
    [SerializeField] private float minSwipeDistance = 50f;

    // Queued input for smoother controls
    private Vector2Int queuedDir;
    private bool hasDirQueued = false;

    public Vector2Int GridPosition => gridPosition;
    public Vector2Int MoveDirection => gridMoveDir;
    public List<Vector2Int> BodyPositions
    {
        get
        {
            var positions = new List<Vector2Int>();
            foreach (var t in snakeBody)
                positions.Add(WorldToGrid(t.position));
            return positions;
        }
    }

    void Awake()
    {
        headSR = GetComponent<SpriteRenderer>();
        baseMoveRate = moveRate;

        controls = new SnakeControls();
        controls.Snake.Up.performed    += _ => TryQueueDir(Vector2Int.up);
        controls.Snake.Down.performed  += _ => TryQueueDir(Vector2Int.down);
        controls.Snake.Left.performed  += _ => TryQueueDir(Vector2Int.left);
        controls.Snake.Right.performed += _ => TryQueueDir(Vector2Int.right);

        controls.Touch.PrimaryContact.started  += _ => { touchStart = controls.Touch.PrimaryPosition.ReadValue<Vector2>(); isSwiping = true; };
        controls.Touch.PrimaryContact.canceled += _ => { if (!isSwiping) return; DetectSwipe(touchStart, controls.Touch.PrimaryPosition.ReadValue<Vector2>()); isSwiping = false; };
    }

    void OnEnable()  => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        gridPosition = WorldToGrid(transform.position);
        transform.position = GridToWorld(gridPosition);
        moveTimer = moveRate;
        //snakeBody.Clear();
        lastTailPrevWorldPos = transform.position;
    }

    void Update()
    {
        // Only move and animate when the game is actively Playing
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            moveTimer += moveRate;

            // Apply queued direction
            if (hasDirQueued)
            {
                gridMoveDir = queuedDir;
                hasDirQueued = false;
            }

            Step();
        }

        // Head animation
        animationTimer -= Time.deltaTime;
        if (animationTimer <= 0f)
        {
            int r = UnityEngine.Random.Range(0, 10);
            headSR.sprite = r < 2 ? headTongue : (r < 4 ? headBlink : headNormal);
            animationTimer = UnityEngine.Random.Range(0.5f, 2f);
        }
    }

    private void TryQueueDir(Vector2Int dir)
    {
        // Reject 180 reversal
        if (dir == -gridMoveDir) return;
        queuedDir = dir;
        hasDirQueued = true;
    }

    public void ScaleSpeedWithScore(int score)
    {
        float newRate = baseMoveRate - (score / speedScoreStep) * speedDecrement;
        moveRate = Mathf.Max(newRate, minMoveRate);
    }

    private void DetectSwipe(Vector2 start, Vector2 end)
    {
        Vector2 swipe = end - start;
        if (swipe.magnitude < minSwipeDistance) return;

        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            TryQueueDir(swipe.x > 0 ? Vector2Int.right : Vector2Int.left);
        else
            TryQueueDir(swipe.y > 0 ? Vector2Int.up : Vector2Int.down);
    }

    private void Step()
    {
        Vector3 oldHeadWorld = transform.position;
        gridPosition += gridMoveDir;
        WrapWithinBounds();
        transform.position = GridToWorld(gridPosition);

        Vector3 prev = oldHeadWorld;
        Debug.Log("[SnakeController]" + snakeBody.Count);
        for (int i = 0; i < snakeBody.Count; i++)
        {
            Vector3 temp = snakeBody[i].position;
            snakeBody[i].position = prev;
            prev = temp;
        }
        lastTailPrevWorldPos = prev;

        UpdateSnakeVisuals();
    }

    private void UpdateSnakeVisuals()
    {
        headSR.sprite = headSprite;

        float zRot = gridMoveDir == Vector2Int.up ? 180f :
                     gridMoveDir == Vector2Int.right ? 90f :
                     gridMoveDir == Vector2Int.down ? 0f : -90f;
        headSR.transform.rotation = Quaternion.Euler(0, 0, zRot);

        // Tint head if shield active
        headSR.color = hasShield ? new Color(0.4f, 0.8f, 1f) : Color.white;
        Debug.Log("[SnakeController]" + snakeBody.Count);
        for (int i = 0; i < snakeBody.Count; i++)
        {
            Debug.Log("SnakeController " + i);
            SpriteRenderer sr = snakeBody[i].GetComponent<SpriteRenderer>();
            bool isTail = i == snakeBody.Count - 1;
            sr.sprite = isTail ? tailSprite : bodySprite;
            sr.color = isGhost ? new Color(1f, 1f, 1f, 0.5f) : Color.white;

            if (isTail)
            {
                Vector3 tailPos = snakeBody[i].position;
                Vector3 prevPos = i > 0 ? snakeBody[i - 1].position : transform.position;
                Vector3 dir = (prevPos - tailPos).normalized;
                float z = dir == Vector3.up ? 180f : dir == Vector3.right ? 90f : dir == Vector3.left ? -90f : 0f;
                snakeBody[i].rotation = Quaternion.Euler(0f, 0f, z);
            }
            else
            {
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
        Debug.Log("[SnakeController] Grow the body");
        Transform newPart = Instantiate(bodyPrefab, lastTailPrevWorldPos, Quaternion.identity);
        newPart.gameObject.tag = "SnakeBody";
        var col = newPart.GetComponent<Collider2D>();
        if (col) { col.enabled = false; StartCoroutine(EnableColliderNextFrame(col)); }
        snakeBody.Add(newPart);
    }

    private IEnumerator EnableColliderNextFrame(Collider2D col)
    {
        yield return null;
        if (col) col.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Snake has colleded with something");
        if (other.CompareTag("Food"))
        {
            Debug.Log($"[Snake] Trigger with Food tag! obj={other.gameObject.name}");
            Food food = other.GetComponent<Food>();
            Debug.Log($"[Snake] Food component: {food}, ScoreManager={ScoreManager.Instance}, GameManager={GameManager.Instance}");
            if (food != null)
            {
                Debug.Log($"[Snake] Ate food: {food.type}, score={food.GetScore()}");
                int score = food.GetScore();
                ScoreManager.Instance.AddScore(score);
                ScoreManager.Instance.AddCombo();

                // Speed scaling
                ScaleSpeedWithScore(ScoreManager.Instance.CurrentScore);

                // Apply food effect
                ApplyFoodEffect(food);

                Vector3 foodPos = other.transform.position;
                FoodType eatenType = food.type;
                if (FoodVFX.Instance != null) FoodVFX.Instance.PlayEat(eatenType, foodPos);

                if (AudioManager.Instance != null) AudioManager.Instance.PlayEat();
                if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.Shake(0.1f, 0.05f);
                if (HapticManager.Instance != null) HapticManager.Instance.Light();

                StartCoroutine(PopEffect(transform));
            }
            Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("SnakeBody"))
        {
            if (isGhost) return; // ghost ignores self
            HandleDeathHit();
        }

        if (other.CompareTag("Wall"))
        {
            HandleDeathHit();
        }
    }

    private void ApplyFoodEffect(Food food)
    {
        var spawner = GameManager.Instance?.spawner;

        switch (food.type)
        {
            case FoodType.Normal:
                Grow();
                spawner?.OnNormalFoodEaten();
                spawner?.SpawnNormalFood();
                break;
            case FoodType.Golden:
                for (int i = 0; i < food.GetGrowth(); i++) Grow();
                break;
            case FoodType.Bomb:
                GameManager.Instance.GameOver();
                break;
            case FoodType.Shrink:
                for (int i = 0; i < -food.GetGrowth(); i++) Shrink();
                break;
            case FoodType.Speed:
                StartCoroutine(SpeedBoost(5f));
                Grow();
                break;
            case FoodType.Slow:
                StartCoroutine(SlowEffect(5f));
                Grow();
                break;
            case FoodType.Ghost:
                StartCoroutine(GhostMode(7f));
                Grow();
                break;
            case FoodType.Shield:
                hasShield = true;
                UIManager.Instance?.ShowShieldIndicator(true);
                Grow();
                break;
        }
    }

    private void HandleDeathHit()
    {
        if (hasShield)
        {
            hasShield = false;
            UIManager.Instance?.ShowShieldIndicator(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShieldBreak();
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.Shake(0.3f, 0.1f);
            return;
        }
        GameManager.Instance?.GameOver();
    }

    private void Shrink()
    {
        if (snakeBody.Count > 0)
        {
            Destroy(snakeBody[snakeBody.Count - 1].gameObject);
            snakeBody.RemoveAt(snakeBody.Count - 1);
        }
    }

    private IEnumerator SpeedBoost(float duration)
    {
        float original = moveRate;
        moveRate = Mathf.Max(moveRate * 0.5f, minMoveRate);
        yield return new WaitForSeconds(duration);
        moveRate = original;
    }

    private IEnumerator SlowEffect(float duration)
    {
        float original = moveRate;
        moveRate = moveRate * 1.8f;
        yield return new WaitForSeconds(duration);
        moveRate = original;
    }

    private IEnumerator GhostMode(float duration)
    {
        isGhost = true;
        UIManager.Instance?.ShowGhostIndicator(true);
        yield return new WaitForSeconds(duration);
        isGhost = false;
        UIManager.Instance?.ShowGhostIndicator(false);
    }

    private IEnumerator PopEffect(Transform target)
    {
        Vector3 original = target.localScale;
        Vector3 enlarged = original * 1.25f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 12f;
            target.localScale = Vector3.Lerp(original, enlarged, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        target.localScale = original;
    }

    public void RestoreState(Vector2Int headPos, Vector2Int direction, List<Vector2Int> bodyPos)
    {
        gridPosition = headPos;
        gridMoveDir = direction;
        transform.position = GridToWorld(gridPosition);
        //foreach (var part in snakeBody) Destroy(part.gameObject);
        //snakeBody.Clear();
        Debug.Log("[SnakeController] cleared snakeBody first");
        foreach (var pos in bodyPos)
        {
            Transform newPart = Instantiate(bodyPrefab, GridToWorld(pos), Quaternion.identity);
            newPart.gameObject.tag = "SnakeBody";
            snakeBody.Add(newPart);
            Debug.Log("[SnakeController] added a body to snake " + snakeBody.Count);
        }
        hasDirQueued = false;
        isGhost = false;
        hasShield = false;
        moveRate = baseMoveRate;
    }

    public Vector3 GridToWorld(Vector2Int gp) => new Vector3(gp.x * cellSize, gp.y * cellSize, 0f);
    public Vector2Int WorldToGrid(Vector3 wp) => new Vector2Int(Mathf.RoundToInt(wp.x / cellSize), Mathf.RoundToInt(wp.y / cellSize));
    public IEnumerable<Vector2Int> OccupiedCells()
    {
        yield return gridPosition;
        foreach (var t in snakeBody)
            yield return WorldToGrid(t.position);
    }
}
