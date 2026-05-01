using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EffectsManager — central hub for screen flash, freeze-frame, and time-slow effects.
/// Attach to a persistent GameObject; assign flashImage in Inspector (full-screen UI Image).
/// </summary>
public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    [Header("Flash")]
    [SerializeField] private Image flashImage;   // full-screen transparent UI Image

    [Header("Defaults")]
    [SerializeField] private float defaultFlashDuration = 0.12f;
    [SerializeField] private Color defaultFlashColor    = new Color(1f, 1f, 1f, 0.35f);

    private Coroutine flashRoutine;
    private Coroutine freezeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (flashImage != null) flashImage.color = Color.clear;
    }

    // ─── Screen Flash ─────────────────────────────────────────────
    public void Flash() => Flash(defaultFlashColor, defaultFlashDuration);

    public void Flash(Color color, float duration)
    {
        if (flashImage == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DoFlash(color, duration));
    }

    private IEnumerator DoFlash(Color color, float duration)
    {
        flashImage.color = color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            flashImage.color = Color.Lerp(color, Color.clear, t);
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    // ─── Freeze Frame ─────────────────────────────────────────────
    /// <summary>Brief time stop (great on death or combo pop).</summary>
    public void FreezeFrame(float duration = 0.05f)
    {
        if (freezeRoutine != null) StopCoroutine(freezeRoutine);
        freezeRoutine = StartCoroutine(DoFreeze(duration));
    }

    private IEnumerator DoFreeze(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    // ─── Time Slow ────────────────────────────────────────────────
    public void SlowTime(float scale, float duration)
    {
        StartCoroutine(DoSlowTime(scale, duration));
    }

    private IEnumerator DoSlowTime(float scale, float duration)
    {
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
