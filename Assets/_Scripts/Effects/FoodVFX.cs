using UnityEngine;

public class FoodVFX : MonoBehaviour
{
    public static FoodVFX Instance { get; private set; }

    [Header("Eat Burst Prefabs")]
    public GameObject normalEatVFX;
    public GameObject goldenEatVFX;
    public GameObject bombEatVFX;
    public GameObject shrinkEatVFX;

    [Header("Spawn Ring Prefabs")]
    public GameObject normalSpawnVFX;
    public GameObject goldenSpawnVFX;
    public GameObject bombSpawnVFX;
    public GameObject shrinkSpawnVFX;

    void Awake() { Instance = this; }

    public void PlayEat(FoodType type, Vector3 pos)
    {
        pos.z = -1f; // always in front of 2D sprites
        GameObject prefab = type switch
        {
            FoodType.Golden => goldenEatVFX,
            FoodType.Bomb   => bombEatVFX,
            FoodType.Shrink => shrinkEatVFX,
            _               => normalEatVFX
        };
        Debug.Log($"[FoodVFX] PlayEat {type} at {pos} using {(prefab != null ? prefab.name : "NULL")}");
        SpawnAndDestroy(prefab, pos);
    }

    public void PlaySpawn(FoodType type, Vector3 pos)
    {
        pos.z = -1f;
        GameObject prefab = type switch
        {
            FoodType.Golden => goldenSpawnVFX,
            FoodType.Bomb   => bombSpawnVFX,
            FoodType.Shrink => shrinkSpawnVFX,
            _               => normalSpawnVFX
        };
        SpawnAndDestroy(prefab, pos);
    }

    private void SpawnAndDestroy(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) { Debug.LogWarning("[FoodVFX] prefab is null!"); return; }
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        var ps = fx.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        float dur = ps != null ? (ps.main.duration + ps.main.startLifetime.constantMax) : 2f;
        Destroy(fx, dur + 0.2f);
    }
}
