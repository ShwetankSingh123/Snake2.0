using System;
using UnityEngine;

[Serializable]
public class DifficultySettings
{
    // Difficulty index (matches Difficulty enum in GameManager by int value)
    public int difficultyIndex;

    // Snake movement rate (seconds per move). Smaller = faster.
    public float snakeMoveRate = 0.20f;

    // Spawn weights for each food type by index. Order must match FoodType enum ordering.
    public float[] spawnWeights = new float[8];

    // How long special foods remain on the board before disappearing
    public float specialFoodLifetime = 6f;

    // Duration of ghost effect when picked up
    public float ghostDuration = 7f;

    // Duration of shield effect (how long shield lasts after pickup)
    public float shieldDuration = 7f;

    public float GetSpawnWeight(int foodTypeIndex)
    {
        int idx = foodTypeIndex;
        if (spawnWeights == null || idx < 0 || idx >= spawnWeights.Length) return 0f;
        return spawnWeights[idx];
    }
}

public static class DifficultyPresets
{
    public static DifficultySettings Get(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0: return Easy();
            case 2: return Hard();
            case 3: return Extreme();
            case 1:
            default: return Normal();
        }
    }

    private static DifficultySettings Easy()
    {
        var s = new DifficultySettings();
        s.difficultyIndex = 0;
        s.snakeMoveRate = 0.28f; // slowest
        s.specialFoodLifetime = 12f; // linger longer
        s.ghostDuration = 12f;
        s.shieldDuration = 12f;
        // spawnWeights indices map to FoodType enum: Normal, Golden, Bomb, Shrink, Speed, Slow, Ghost, Shield
        s.spawnWeights = new float[] { 0f, 5f, 1f, 3f, 1f, 3f, 3f, 3f };
        return s;
    }

    private static DifficultySettings Normal()
    {
        var s = new DifficultySettings();
        s.difficultyIndex = 1;
        s.snakeMoveRate = 0.20f; // default
        s.specialFoodLifetime = 6f;
        s.ghostDuration = 7f;
        s.shieldDuration = 7f;
        s.spawnWeights = new float[] { 0f, 3f, 2f, 2f, 2f, 2f, 2f, 2f };
        return s;
    }

    private static DifficultySettings Hard()
    {
        var s = new DifficultySettings();
        s.difficultyIndex = 2;
        s.snakeMoveRate = 0.16f; // ~20% faster than normal
        s.specialFoodLifetime = 4f; // shorter
        s.ghostDuration = 4f;
        s.shieldDuration = 4f;
        s.spawnWeights = new float[] { 0f, 1f, 4f, 1f, 3f, 1f, 1f, 1f };
        return s;
    }

    private static DifficultySettings Extreme()
    {
        var s = new DifficultySettings();
        s.difficultyIndex = 3;
        s.snakeMoveRate = 0.13f; // ~35-40% faster than normal
        s.specialFoodLifetime = 2.5f; // very quick
        s.ghostDuration = 3f;
        s.shieldDuration = 3f;
        s.spawnWeights = new float[] { 0f, 1f, 6f, 1f, 5f, 1f, 1f, 1f };
        return s;
    }
}
