using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    [Header("Time Settings")]
    [Tooltip("In-game hour (0–24)")]
    [SerializeField, Range(0f, 24f)] private float currentTime = 8f;
    [Tooltip("How many in-game minutes per real-time second (60 = 1s -> 1min)")]
    [SerializeField] private float timeMultiplier = 60f;
    [SerializeField] private int dayNumber = 1;

    [Header("Sun / Lighting")]
    [SerializeField] private Light sunLight;
    [Tooltip("Approx. daylight hours for intensity switch")]
    [SerializeField] private Vector2 dayLightHours = new Vector2(6f, 20f);
    [SerializeField] private float nightSunIntensity = 0.2f;
    [SerializeField] private float daySunIntensity = 1f;
    [SerializeField] private float sunYaw = -30f;

    [Header("UI (Optional)")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.1f;

    [Header("Sleep Policy")]
    [Tooltip("Allowed sleep window (wrap supported). Default: 19:00–05:00")]
    [SerializeField] private Vector2 sleepAllowedWindow = new Vector2(19f, 5f); // 19 -> 05 (wrap)
    [Tooltip("Fixed wake hour")]
    [SerializeField, Range(0f, 24f)] private float wakeHour = 7f; // always 07:00

    // State
    private bool isSleeping = false;
    public bool IsSleeping => isSleeping;

    // Broadcast when time skips (fromHour, toHour, daysPassed)
    public event Action<float, float, int> OnTimeAdvanced;

    // ------------------------------------------------------

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        UpdateUI();
        UpdateSun();
    }

    private void Update()
    {
        if (isSleeping) return;

        currentTime += Time.deltaTime * (timeMultiplier / 3600f); // hours
        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            dayNumber += 1;
        }

        UpdateUI();
        UpdateSun();
    }

    // ------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------

    /// <summary>
    /// Try to sleep. Only allowed between sleepAllowedWindow (wrap). Wakes at fixed wakeHour.
    /// </summary>
    public void Sleep()
    {
        if (isSleeping) return;

        if (!CanSleepNow(out string reason))
        {
            // Hook your UI/Toast here instead of Debug.Log if you want
            Debug.Log($"[TimeManager] Sleep denied: {reason}");
            return;
        }

        StartCoroutine(SleepToFixedWake());
    }

    /// <summary>
    /// Kept for backward compatibility (ignored param).
    /// </summary>
    public void Sleep(float _ignoredHours)
    {
        Sleep();
    }

    /// <summary>
    /// Check if we can sleep at current time (must be within allowed window).
    /// </summary>
    public bool CanSleepNow(out string reason)
    {
        if (!IsInWindowWrap(currentTime, sleepAllowedWindow.x, sleepAllowedWindow.y))
        {
            reason = $"You can only sleep between {FormatHour(sleepAllowedWindow.x)} and {FormatHour(sleepAllowedWindow.y)}.";
            return false;
        }
        reason = null;
        return true;
    }

    public float GetCurrentTime() => currentTime;
    public int GetDayNumber() => dayNumber;

    // ------------------------------------------------------
    // CORE
    // ------------------------------------------------------

    private IEnumerator SleepToFixedWake()
    {
        if (isSleeping) yield break;
        isSleeping = true;

        // Fade to black
        yield return StartCoroutine(FadeRoutine(1f));

        float fromHour = currentTime;

        // Decide day increment for wake
        // If currentTime < wakeHour -> wake same day at wakeHour
        // else -> wake next day at wakeHour
        int dayInc = currentTime < wakeHour ? 0 : 1;

        dayNumber += dayInc;
        currentTime = Mathf.Repeat(wakeHour, 24f);

        // Notify listeners while screen is black
        OnTimeAdvanced?.Invoke(fromHour, currentTime, dayInc);

        // Update visuals
        UpdateUI();
        UpdateSun();

        // Fade back
        yield return StartCoroutine(FadeRoutine(0f));

        isSleeping = false;
    }

    // ------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------

    // Wrap-aware window check:
    // Window [a,b) with wrap support. If a < b : a ? x < b
    // If a > b : x ? a or x < b  (e.g., 19?05)
    private static bool IsInWindowWrap(float x, float aInclusive, float bExclusive)
    {
        if (Mathf.Approximately(aInclusive, bExclusive)) return true; // full-day window
        if (aInclusive < bExclusive) return x >= aInclusive && x < bExclusive;
        return x >= aInclusive || x < bExclusive; // wrap
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeImage == null || fadeDuration <= 0f) yield break;

        float startAlpha = fadeImage.color.a;
        if (Mathf.Approximately(startAlpha, targetAlpha)) yield break;

        float t = 0f;
        Color col = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            col.a = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            fadeImage.color = col;
            yield return null;
        }
        col.a = targetAlpha;
        fadeImage.color = col;
    }

    private void UpdateUI()
    {
        if (clockText != null)
        {
            float h = Mathf.Floor(currentTime);
            float m = Mathf.Floor((currentTime - h) * 60f);
            clockText.text = $"{h:00}:{m:00}";
        }
        if (dayText != null)
        {
            dayText.text = $"Gün: {dayNumber}";
        }
    }

    private void UpdateSun()
    {
        if (sunLight == null) return;

        float sunPitch = currentTime * 15f - 90f; // 24h -> 360°, 1h -> 15°
        sunLight.transform.localRotation = Quaternion.Euler(sunPitch, sunYaw, 0f);

        bool isDay = currentTime >= dayLightHours.x && currentTime <= dayLightHours.y;
        sunLight.intensity = isDay ? daySunIntensity : nightSunIntensity;
    }

    private static string FormatHour(float hour)
    {
        int h = Mathf.FloorToInt(Mathf.Repeat(hour, 24f));
        return $"{h:00}:00";
    }
}
