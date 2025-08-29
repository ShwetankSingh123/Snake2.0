using UnityEngine;

public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [Header("Dynamic Bounds")]
    [HideInInspector]
    public int minX, maxX, minY, maxY;

    [SerializeField] private int cellSize = 1; // match SnakeController cell size

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CalculateBounds();
    }

    void CalculateBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogError("Main Camera not found!"); return; }

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Convert world to grid with safe padding
        minX = Mathf.CeilToInt(-halfWidth / cellSize) + 1;   // +1 padding
        maxX = Mathf.FloorToInt(halfWidth / cellSize) - 1;   // -1 padding
        minY = Mathf.CeilToInt(-halfHeight / cellSize) + 1;  // +1 padding
        maxY = Mathf.FloorToInt(halfHeight / cellSize) - 1;  // -1 padding

        Debug.Log($"[Board] Safe Bounds: X[{minX},{maxX}] Y[{minY},{maxY}]");
    }

}
