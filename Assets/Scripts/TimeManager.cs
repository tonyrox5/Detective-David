using System.Collections; // Coroutine kullanmak için bu kütüphane gerekli!
using UnityEngine;
using UnityEngine.UI; // UI elementlerine (Image) eriþim için bu gerekli!
using TMPro;

public class TimeManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static TimeManager instance;

    [Header("Zaman Ayarlarý")]
    [SerializeField, Range(0, 24)] private float currentTime = 8f;
    [SerializeField] private float timeMultiplier = 60f;
    private int dayNumber = 1;

    [Header("Görsel ve UI Ayarlarý")]
    [SerializeField] private Light sunLight;
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Uyku Animasyon Ayarlarý")] // YENÝ EKLENEN BÖLÜM
    [SerializeField] private Image fadeImage;          // Siyah ekranýmýz
    [SerializeField] private float fadeDuration = 1.5f; // Kararma/Açýlma ne kadar sürecek?

    private void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Update()
    {
        currentTime += Time.deltaTime * timeMultiplier / 3600;

        if (currentTime >= 24)
        {
            currentTime = 0;
            dayNumber++;
        }

        UpdateUI();
        UpdateSun();
    }

    // --- ESKÝ Sleep() FONKSÝYONUNU GÜNCELLEDÝK ---
    // Artýk bu fonksiyon sadece Coroutine'i baþlatýyor.
    public void Sleep()
    {
        // Çalýþan baþka bir uyku animasyonu varsa yenisini baþlatma.
        StopAllCoroutines();
        StartCoroutine(SleepSequence());
    }

    // --- YENÝ EKLENEN ANÝMASYON FONKSÝYONU (COROUTINE) ---
    private IEnumerator SleepSequence()
    {
        // --- 1. Adým: Ekraný Karartma (Fade to Black) ---
        float timer = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(0, 0, 0, 1); // Siyah ve tamamen opak

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null; // Bir sonraki kareye kadar bekle
        }
        fadeImage.color = endColor; // Tam karardýðýndan emin ol

        // --- 2. Adým: Zamaný Ýleri Sarma ---
        Debug.Log("Uykuya dalýndý!");
        currentTime = 7f; // Sabah 7'ye ayarla
        dayNumber++;
        yield return new WaitForSeconds(0.5f); // Yarým saniye siyah ekranda bekle (daha iyi hissettirir)

        // --- 3. Adým: Ekraný Açma (Fade from Black) ---
        timer = 0f;
        startColor = fadeImage.color;
        endColor = new Color(0, 0, 0, 0); // Siyah ama tamamen saydam

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null; // Bir sonraki kareye kadar bekle
        }
        fadeImage.color = endColor; // Tam açýldýðýndan emin ol
    }


    // --- Deðiþmeyen Diðer Fonksiyonlar ---
    private void UpdateUI()
    {
        float hours = Mathf.Floor(currentTime);
        float minutes = Mathf.Floor((currentTime - hours) * 60);
        clockText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        dayText.text = "Gün: " + dayNumber;
    }

    private void UpdateSun()
    {
        float sunAngle = currentTime * 15f - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(new Vector3(sunAngle, -30f, 0));
        if (currentTime < 6 || currentTime > 20) { sunLight.intensity = 0.2f; }
        else { sunLight.intensity = 1f; }
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }
    // --- YENÝ EKLENEN FONKSÝYON ---
    public int GetDayNumber()
    {
        return dayNumber;
    }
    // --- YENÝ FONKSÝYONUN SONU ---
}