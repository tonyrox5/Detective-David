using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    [Header("Zaman Ayarlarý")]
    [Tooltip("Gün içi saat (0–24)")]
    [SerializeField, Range(0f, 24f)] private float currentTime = 8f;
    [Tooltip("1 gerçek saniyenin kaç oyun dakikasýna denk geldiði. Örn: 60 = 1 sn -> 1 dk")]
    [SerializeField] private float timeMultiplier = 60f;
    [SerializeField] private int dayNumber = 1;

    [Header("Güneþ / Aydýnlatma")]
    [SerializeField] private Light sunLight;
    [Tooltip("Gündüz tam parlaklýk saati aralýðý (yaklaþýk)")]
    [SerializeField] private Vector2 dayLightHours = new Vector2(6f, 20f);
    [Tooltip("Gece güneþ þiddeti")]
    [SerializeField] private float nightSunIntensity = 0.2f;
    [Tooltip("Gündüz güneþ þiddeti")]
    [SerializeField] private float daySunIntensity = 1f;
    [Tooltip("Güneþin gökyüzündeki açý ofseti")]
    [SerializeField] private float sunYaw = -30f;

    [Header("UI (Opsiyonel)")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.25f;

    [Header("Uyandýrma Politikasý")]
    [Tooltip("Sabah uyanma saati (07:00)")]
    [SerializeField, Range(0f, 24f)] private float morningWakeHour = 7f;   // 07:00
    [Tooltip("Akþam uyanma saati (17:00)")]
    [SerializeField, Range(0f, 24f)] private float eveningWakeHour = 17f;  // 17:00

    [Header("Uyku Yasak Saatleri")]
    [Tooltip("Bu aralýkta uyku yasak: 07–09")]
    [SerializeField] private Vector2 morningNoSleep = new Vector2(7f, 9f);
    [Tooltip("Bu aralýkta uyku yasak: 17–19")]
    [SerializeField] private Vector2 eveningNoSleep = new Vector2(17f, 19f);

    // Durum
    private bool isSleeping = false;
    public bool IsSleeping => isSleeping;

    // Zaman atlama olayý (NPC/dünya sistemleri dinleyebilir)
    public event Action<float, float, int> OnTimeAdvanced; // fromHour, toHour, daysPassed

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

        currentTime += Time.deltaTime * (timeMultiplier / 3600f);
        if (currentTime >= 24f) { currentTime -= 24f; dayNumber += 1; }

        UpdateUI();
        UpdateSun();
    }

    // ------------------------------------------------------
    // KAMU API
    // ------------------------------------------------------

    /// <summary>
    /// Politika bazlý uyku (07–17 arasý 17:00; diðer saatler 07:00) — Uyku yasaðý (07–09, 17–19) uygulanýr.
    /// </summary>
    public void Sleep()
    {
        if (isSleeping) return;

        if (!CanSleepNow(out string reason))
        {
            // Buraya UI mesajý baðlayabilirsin (Toast/Popup). Þimdilik log.
            Debug.Log($"[TimeManager] Uyku engellendi: {reason}");
            return;
        }

        StartCoroutine(SleepToPolicy());
    }

    /// <summary>
    /// Eski çaðrýlar bozulmasýn diye politikaya yönlendirildi (parametre görmezden gelinir).
    /// </summary>
    public void Sleep(float _ignoredHours)
    {
        Sleep();
    }

    /// <summary>
    /// Þu an uyunabilir mi? (Uyku yasak pencereleri kontrol edilir)
    /// </summary>
    public bool CanSleepNow(out string reason)
    {
        // 07–09 ve 17–19 arasýnda uyku yasak
        if (InRange(currentTime, morningNoSleep.x, morningNoSleep.y))
        {
            reason = $"Saat {FormatHourRange(morningNoSleep)} arasýnda uyku yasak.";
            return false;
        }
        if (InRange(currentTime, eveningNoSleep.x, eveningNoSleep.y))
        {
            reason = $"Saat {FormatHourRange(eveningNoSleep)} arasýnda uyku yasak.";
            return false;
        }

        reason = null;
        return true;
    }

    public float GetCurrentTime() => currentTime;
    public int GetDayNumber() => dayNumber;

    // ------------------------------------------------------
    // ÇEKÝRDEK
    // ------------------------------------------------------

    private IEnumerator SleepToPolicy()
    {
        if (isSleeping) yield break;
        isSleeping = true;

        // Fade to black
        yield return StartCoroutine(FadeRoutine(1f));

        // Uyanma saatini belirle
        float fromHour = currentTime;
        ComputePolicyWake(out float wakeHour, out int dayInc);

        // Zamaný uygula
        dayNumber += dayInc;
        currentTime = Mathf.Repeat(wakeHour, 24f);

        // OLAYI YAYINLA ? NPC/kapý vb. sistemler yeni saate göre hizalansýn
        OnTimeAdvanced?.Invoke(fromHour, currentTime, dayInc);

        // Görsellik
        UpdateUI();
        UpdateSun();

        // Fade back
        yield return StartCoroutine(FadeRoutine(0f));

        isSleeping = false;
    }

    private void ComputePolicyWake(out float wakeHour, out int dayInc)
    {
        // 07:00 ? now < 17:00  -> bugün 17:00'de uyan
        // aksi halde            -> 07:00'de uyan (17:00 sonrasýysa ertesi gün 07:00)
        if (InRange(currentTime, morningWakeHour, eveningWakeHour))
        {
            wakeHour = eveningWakeHour;
            dayInc = 0;
        }
        else
        {
            if (currentTime < morningWakeHour)
            {
                wakeHour = morningWakeHour;
                dayInc = 0;
            }
            else
            {
                wakeHour = morningWakeHour;
                dayInc = 1;
            }
        }
    }

    // ------------------------------------------------------
    // Yardýmcýlar
    // ------------------------------------------------------

    // a ? x < b (wrap yok)
    private static bool InRange(float x, float aInclusive, float bExclusive)
    {
        return x >= aInclusive && x < bExclusive;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeImage == null || fadeDuration <= 0f)
            yield break;

        float startAlpha = fadeImage.color.a;
        if (Mathf.Approximately(startAlpha, targetAlpha))
            yield break;

        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            c.a = a;
            fadeImage.color = c;
            yield return null;
        }
        c.a = targetAlpha;
        fadeImage.color = c;
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

        // Güneþ açýsý
        float sunPitch = currentTime * 15f - 90f; // 24 saatte 360°, 1 saatte 15°
        sunLight.transform.localRotation = Quaternion.Euler(sunPitch, sunYaw, 0f);

        // Ýntensity
        bool isDay = currentTime >= dayLightHours.x && currentTime <= dayLightHours.y;
        sunLight.intensity = isDay ? daySunIntensity : nightSunIntensity;
    }

    private string FormatHourRange(Vector2 range)
    {
        int a = Mathf.RoundToInt(range.x);
        int b = Mathf.RoundToInt(range.y);
        return $"{a:00}:00–{b:00}:00";
    }
}
