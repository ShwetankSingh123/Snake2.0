using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIManager uiManager;

    [Header("Combo Settings")]
    [SerializeField] private float comboDecayTime = 3f;
    [SerializeField] private int maxCombo = 10;

    public int CurrentScore { get; private set; }
    public int bestScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public float ComboMultiplier => 1f + (CurrentCombo * 0.1f); // +10% per combo

    private float comboTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentScore = 0;
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        RefreshUI();
    }

    void Update()
    {
        if (CurrentCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) ResetCombo();
        }
    }

    public void AddScore(int baseAmount)
    {
        int actual = Mathf.RoundToInt(baseAmount * ComboMultiplier);
        CurrentScore += actual;
        RefreshUI();
    }

    public void AddCombo()
    {
        CurrentCombo = Mathf.Min(CurrentCombo + 1, maxCombo);
        comboTimer = comboDecayTime;
        uiManager?.UpdateComboUI(CurrentCombo);
    }

    private void ResetCombo()
    {
        CurrentCombo = 0;
        uiManager?.UpdateComboUI(0);
    }

    public void SetScore(int value)
    {
        CurrentScore = value;
        CurrentCombo = 0;
        RefreshUI();
    }

    public void FinalizeScore()
    {
        if (CurrentScore > bestScore)
        {
            bestScore = CurrentScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }
    }

    private void RefreshUI()
    {
        uiManager?.UpdateScoreUI(CurrentScore);
        uiManager?.UpdateBestScore();
    }
}
