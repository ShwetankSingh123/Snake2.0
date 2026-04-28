using System.Collections;
using UnityEngine;

public class FoodVFX : MonoBehaviour
{
    // ── Singleton so FoodSpawner/SnakeController can call it easily ──
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

    void Awake()
    {
        Instance = this;
    }

    public void PlayEat(FoodType type, Vector3 pos)
    {
        GameObject prefab = type switch
        {
            FoodType.Golden => goldenEatVFX,
            FoodType.Bomb   => bombEatVFX,
            FoodType.Shrink => shrinkEatVFX,
            _               => normalEatVFX
        };
        SpawnAndDestroy(prefab, pos);
    }

    public void PlaySpawn(FoodType type, Vector3 pos)
    {
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
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        var ps = fx.GetComponent<ParticleSystem>();
        float dur = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        Destroy(fx, dur + 0.1f);
    }
}
