using UnityEngine;

public enum FoodType
{
    Normal,
    Golden,
    Bomb
}

public class Food : MonoBehaviour
{
    public FoodType type = FoodType.Normal;

    public float lifeTime = 0f;    // 0 = permanent, >0 = disappear after X seconds

    private float timer;

    void OnEnable()
    {
        timer = lifeTime;
    }

    void Update()
    {
        if (lifeTime > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Destroy(gameObject); // vanish, no respawn
        }
    }

    public int GetScore()
    {
        switch (type)
        {
            case FoodType.Golden: return 5;
            case FoodType.Bomb: return -1;
            default: return 1;
        }
    }

    public int GetGrowth()
    {
        switch (type)
        {
            case FoodType.Golden: return 3;
            case FoodType.Bomb: return 0;
            default: return 1;
        }
    }
}
