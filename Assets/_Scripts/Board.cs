using UnityEngine;

/// <summary>
/// Board: computes dynamic grid bounds by projecting the camera frustum onto a ground plane.
/// Works with perspective or orthographic cameras. Supports XZ (ground) or XY (2D) layouts.
/// </summary>
public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    public enum PlaneMode { XZ, XY }

    [Header("Mode")]
    public PlaneMode planeMode = PlaneMode.XZ; // choose XZ for 3D ground, XY for 2D

    [Header("Grid & Plane")]
    [Tooltip("Size of each grid cell in world units (must match SnakeController.cellSize)")]
    public int cellSize = 1;

    [Tooltip("If planeMode==XZ, this is ground Y. If planeMode==XY, this is ground Z.")]
    public float planeCoord = 0f;

    [Header("Computed bounds (grid coords)")]
    [HideInInspector] public int minX, maxX, minY, maxY;

    // caching to detect changes
    Camera cam;
    int lastScreenW, lastScreenH;
    Vector3 lastCamPos;
    Quaternion lastCamRot;
    float lastCamFOV;
    float lastOrthoSize;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cam = Camera.main;
        if (cam == null) Debug.LogError("[Board] Main Camera not found.");

        Recalculate(); // initial calc
        CacheCameraState();
    }

    void Update()
    {
        if (cam == null) return;

        // detect changes that require recalculation
        if (Screen.width != lastScreenW || Screen.height != lastScreenH
            || cam.transform.position != lastCamPos
            || cam.transform.rotation != lastCamRot
            || (cam.orthographic ? cam.orthographicSize != lastOrthoSize : cam.fieldOfView != lastCamFOV))
        {
            Recalculate();
            CacheCameraState();
        }
    }

    void CacheCameraState()
    {
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
        lastCamPos = cam.transform.position;
        lastCamRot = cam.transform.rotation;
        lastCamFOV = cam.fieldOfView;
        lastOrthoSize = cam.orthographicSize;
    }

    /// <summary>
    /// Call to force recalculation (public).
    /// </summary>
    public void Recalculate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3[] worldCorners = new Vector3[4];

        // define viewport corners in order: BL, BR, TR, TL
        Vector3[] viewportCorners = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f)
        };

        // define the plane depending on mode
        Plane groundPlane;
        if (planeMode == PlaneMode.XZ)
            groundPlane = new Plane(Vector3.up, new Vector3(0f, planeCoord, 0f)); // Y = planeCoord
        else
            groundPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeCoord)); // Z = planeCoord

        bool anyMiss = false;
        for (int i = 0; i < 4; i++)
        {
            Ray r = cam.ViewportPointToRay(viewportCorners[i]);
            if (groundPlane.Raycast(r, out float enter))
            {
                worldCorners[i] = r.GetPoint(enter);
            }
            else
            {
                // Ray didn't hit plane (camera probably parallel). Mark miss and fallback to some projection.
                anyMiss = true;
                worldCorners[i] = cam.transform.position; // fallback placeholder
                Debug.LogWarning($"[Board] Frustum corner {i} did not hit ground plane. Camera angle maybe parallel to plane.");
            }
        }

        if (anyMiss)
        {
            // If any miss, fall back to a conservative orthographic-style bounds around camera
            // This is safer than leaving zeros and ensures some playable area.
            float fallbackHalfWidth = (cam.orthographic ? cam.orthographicSize * cam.aspect : 10f); // 10 units fallback
            float fallbackHalfHeight = (cam.orthographic ? cam.orthographicSize : 5f);
            Vector3 center = cam.transform.position;
            if (planeMode == PlaneMode.XZ)
            {
                float minWX = center.x - fallbackHalfWidth;
                float maxWX = center.x + fallbackHalfWidth;
                float minWZ = center.z - fallbackHalfHeight;
                float maxWZ = center.z + fallbackHalfHeight;

                minX = Mathf.CeilToInt(minWX / cellSize);
                maxX = Mathf.FloorToInt(maxWX / cellSize);
                minY = Mathf.CeilToInt(minWZ / cellSize);
                maxY = Mathf.FloorToInt(maxWZ / cellSize);
            }
            else
            {
                float minWX = center.x - fallbackHalfWidth;
                float maxWX = center.x + fallbackHalfWidth;
                float minWY = center.y - fallbackHalfHeight;
                float maxWY = center.y + fallbackHalfHeight;

                minX = Mathf.CeilToInt(minWX / cellSize);
                maxX = Mathf.FloorToInt(maxWX / cellSize);
                minY = Mathf.CeilToInt(minWY / cellSize);
                maxY = Mathf.FloorToInt(maxWY / cellSize);
            }

            Debug.LogWarning($"[Board] Using fallback bounds because raycasts failed. X[{minX},{maxX}] Y[{minY},{maxY}]");
            return;
        }

        // compute world extents
        float minWorldX = Mathf.Min(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x);
        float maxWorldX = Mathf.Max(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x);

        float minWorldYorZ, maxWorldYorZ;
        if (planeMode == PlaneMode.XZ)
        {
            minWorldYorZ = Mathf.Min(worldCorners[0].z, worldCorners[1].z, worldCorners[2].z, worldCorners[3].z);
            maxWorldYorZ = Mathf.Max(worldCorners[0].z, worldCorners[1].z, worldCorners[2].z, worldCorners[3].z);
        }
        else
        {
            minWorldYorZ = Mathf.Min(worldCorners[0].y, worldCorners[1].y, worldCorners[2].y, worldCorners[3].y);
            maxWorldYorZ = Mathf.Max(worldCorners[0].y, worldCorners[1].y, worldCorners[2].y, worldCorners[3].y);
        }

        // apply small padding (so sprites don't get cut off)
        float pad = 0.5f * cellSize; // half cell padding
        minWorldX += pad;
        maxWorldX -= pad;
        minWorldYorZ += pad;
        maxWorldYorZ -= pad;

        // convert to integer grid coords
        minX = Mathf.CeilToInt(minWorldX / cellSize);
        maxX = Mathf.FloorToInt(maxWorldX / cellSize);
        minY = Mathf.CeilToInt(minWorldYorZ / cellSize);
        maxY = Mathf.FloorToInt(maxWorldYorZ / cellSize);

        Debug.Log($"[Board] Dynamic Bounds computed: X[{minX},{maxX}] Y[{minY},{maxY}] (planeMode={planeMode})");
    }

#if UNITY_EDITOR
    // Draw the computed bounds in the Scene view for debugging
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        if (planeMode == PlaneMode.XZ)
        {
            Vector3 a = new Vector3(minX * cellSize, planeCoord, minY * cellSize);
            Vector3 b = new Vector3(maxX * cellSize + cellSize, planeCoord, minY * cellSize);
            Vector3 c = new Vector3(maxX * cellSize + cellSize, planeCoord, maxY * cellSize + cellSize);
            Vector3 d = new Vector3(minX * cellSize, planeCoord, maxY * cellSize + cellSize);
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        }
        else
        {
            Vector3 a = new Vector3(minX * cellSize, minY * cellSize, planeCoord);
            Vector3 b = new Vector3(maxX * cellSize + cellSize, minY * cellSize, planeCoord);
            Vector3 c = new Vector3(maxX * cellSize + cellSize, maxY * cellSize + cellSize, planeCoord);
            Vector3 d = new Vector3(minX * cellSize, maxY * cellSize + cellSize, planeCoord);
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        }
    }
#endif
}
