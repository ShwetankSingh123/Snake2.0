using UnityEngine;

public enum FoodType
{
    Normal,
    Golden,
    Bomb,
    Shrink,
    Speed,   // Temporarily doubles snake speed, +1 growth
    Slow,    // Temporarily halves speed (easier to maneuver), +1 growth
    Ghost,   // Walk through self for 7 seconds, +1 growth
    Shield,  // Next death is blocked, +1 growth
}

public class Food : MonoBehaviour
{
    public FoodType type = FoodType.Normal;
    public float lifeTime = 0f;

    private float timer;

    void OnEnable()
    {
        timer = lifeTime;
        if (lifeTime > 0f)
        {
            Color c = GetTimerColor();
            UIManager.Instance?.StartSpecialTimer(lifeTime, c);
        }
    }

    void Update()
    {
        if (lifeTime > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                UIManager.Instance?.StopSpecialTimer();
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (lifeTime > 0f)
            UIManager.Instance?.StopSpecialTimer();

        var spawner = FindAnyObjectByType<FoodSpawner>();
        if (type != FoodType.Normal)
            spawner?.NotifySpecialDestroyed(gameObject);
    }

    private Color GetTimerColor() => type switch
    {
        FoodType.Golden => new Color(1f, 0.85f, 0f),
        FoodType.Bomb   => new Color(1f, 0.2f, 0.2f),
        FoodType.Shrink => new Color(0.6f, 0.2f, 1f),
        FoodType.Speed  => new Color(0f, 1f, 0.5f),
        FoodType.Slow   => new Color(0.2f, 0.6f, 1f),
        FoodType.Ghost  => new Color(0.8f, 0.8f, 1f),
        FoodType.Shield => new Color(0.3f, 0.8f, 1f),
        _               => Color.white,
    };

    public int GetScore() => type switch
    {
        FoodType.Golden => 5,
        FoodType.Bomb   => -2,
        FoodType.Shrink => 1,
        FoodType.Speed  => 3,
        FoodType.Slow   => 2,
        FoodType.Ghost  => 4,
        FoodType.Shield => 3,
        _               => 1,
    };

    public int GetGrowth() => type switch
    {
        FoodType.Golden => 3,
        FoodType.Bomb   => 0,
        FoodType.Shrink => -2,
        _               => 1,
    };
}
