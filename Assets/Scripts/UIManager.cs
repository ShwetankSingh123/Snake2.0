using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Special Food Timer UI")]
    public GameObject specialTimerPanel; // container panel
    public Image specialTimerFill;       // fill bar

    private float timerDuration;
    private float timerRemaining;
    private bool isActive = false;

    void Start()
    {
        specialTimerPanel.SetActive(false);
    }

    public void StartSpecialTimer(float duration, Color barColor)
    {
        timerDuration = duration;
        timerRemaining = duration;
        isActive = true;
        specialTimerPanel.SetActive(true);
        specialTimerFill.fillAmount = 1f;
        specialTimerFill.color = barColor;
    }

    public void StopSpecialTimer()
    {
        isActive = false;
        specialTimerPanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        timerRemaining -= Time.deltaTime;
        specialTimerFill.fillAmount = Mathf.Clamp01(timerRemaining / timerDuration);

        if (timerRemaining <= 0f)
        {
            StopSpecialTimer();
        }
    }
}
