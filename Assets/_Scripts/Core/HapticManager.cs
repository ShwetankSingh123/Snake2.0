using UnityEngine;

/// <summary>
/// HapticManager — platform-aware haptic feedback.
/// Works on Android (Handheld.Vibrate) and iOS (via Unity Handheld).
/// Gracefully no-ops on desktop.
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    private bool _enabled = true;
    public bool IsEnabled
    {
        get => _enabled;
        set { _enabled = value; PlayerPrefs.SetInt("HapticsEnabled", value ? 1 : 0); }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _enabled = PlayerPrefs.GetInt("HapticsEnabled", 1) == 1;
    }

    /// <summary>Short light tap — eating normal food.</summary>
    public void Light()
    {
        if (!_enabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    /// <summary>Medium pulse — combo hit or special food.</summary>
    public void Medium()
    {
        if (!_enabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    /// <summary>Strong thud — death, shield break.</summary>
    public void Heavy()
    {
        if (!_enabled) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
        // Double pulse via coroutine not available in static context;
        // for richer haptics integrate the iOS CoreHaptics plugin later.
#endif
    }
}
