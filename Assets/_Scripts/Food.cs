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
    public float lifeTime = 0f;

    private float timer;

    void OnEnable()
    {
        timer = lifeTime;

        if (lifeTime > 0f) // special food
        {
            var ui = GameManager.Instance.uiManager;
            if (ui != null)
            {
                Color c = (type == FoodType.Golden) ? Color.yellow : Color.red;
                ui.StartSpecialTimer(lifeTime, c);
            }
        }
    }

    void Update()
    {
        if (lifeTime > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                var ui = GameManager.Instance.uiManager;
                if (ui != null) ui.StopSpecialTimer();

                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (lifeTime > 0f)
        {
            var ui = GameManager.Instance.uiManager;
            if (ui != null) ui.StopSpecialTimer();
        }

        if (type == FoodType.Golden || type == FoodType.Bomb)
        {
            GameManager.Instance.spawner.NotifySpecialDestroyed(gameObject);
        }
    }

    public int GetScore() =>
        type == FoodType.Golden ? 5 :
        type == FoodType.Bomb ? -1 : 1;

    public int GetGrowth() =>
        type == FoodType.Golden ? 3 :
        type == FoodType.Bomb ? 0 : 1;
}
