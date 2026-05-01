using System.Collections;
using UnityEngine;

/// <summary>
/// ScreenShakeManager — camera shake using Cinemachine-free perlin offset.
/// Call Shake(duration, magnitude) from anywhere.
/// </summary>
public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration  = 0.15f;
    [SerializeField] private float defaultMagnitude = 0.08f;

    private Vector3 originalPos;
    private Coroutine shakeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        if (cameraTransform != null)
            originalPos = cameraTransform.localPosition;
    }

    public void Shake() => Shake(defaultDuration, defaultMagnitude);

    public void Shake(float duration, float magnitude)
    {
        if (cameraTransform == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float dampen   = 1f - Mathf.Clamp01(progress * 1.5f); // eases out
            float x = Random.Range(-1f, 1f) * magnitude * dampen;
            float y = Random.Range(-1f, 1f) * magnitude * dampen;
            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cameraTransform.localPosition = originalPos;
        shakeRoutine = null;
    }
}
