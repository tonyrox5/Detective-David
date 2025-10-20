using System.Collections; // Coroutine kullanmak i�in bu k�t�phane gerekli!
using UnityEngine;
using UnityEngine.UI; // UI elementlerine (Image) eri�im i�in bu gerekli!
using TMPro;

public class TimeManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static TimeManager instance;

    [Header("Zaman Ayarlar�")]
    [SerializeField, Range(0, 24)] private float currentTime = 8f;
    [SerializeField] private float timeMultiplier = 60f;
    private int dayNumber = 1;

    [Header("G�rsel ve UI Ayarlar�")]
    [SerializeField] private Light sunLight;
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Uyku Animasyon Ayarlar�")] // YEN� EKLENEN B�L�M
    [SerializeField] private Image fadeImage;          // Siyah ekran�m�z
    [SerializeField] private float fadeDuration = 1.5f; // Kararma/A��lma ne kadar s�recek?

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

    // --- ESK� Sleep() FONKS�YONUNU G�NCELLED�K ---
    // Art�k bu fonksiyon sadece Coroutine'i ba�lat�yor.
    public void Sleep()
    {
        // �al��an ba�ka bir uyku animasyonu varsa yenisini ba�latma.
        StopAllCoroutines();
        StartCoroutine(SleepSequence());
    }

    // --- YEN� EKLENEN AN�MASYON FONKS�YONU (COROUTINE) ---
    private IEnumerator SleepSequence()
    {
        // --- 1. Ad�m: Ekran� Karartma (Fade to Black) ---
        float timer = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(0, 0, 0, 1); // Siyah ve tamamen opak

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null; // Bir sonraki kareye kadar bekle
        }
        fadeImage.color = endColor; // Tam karard���ndan emin ol

        // --- 2. Ad�m: Zaman� �leri Sarma ---
        Debug.Log("Uykuya dal�nd�!");
        currentTime = 7f; // Sabah 7'ye ayarla
        dayNumber++;
        yield return new WaitForSeconds(0.5f); // Yar�m saniye siyah ekranda bekle (daha iyi hissettirir)

        // --- 3. Ad�m: Ekran� A�ma (Fade from Black) ---
        timer = 0f;
        startColor = fadeImage.color;
        endColor = new Color(0, 0, 0, 0); // Siyah ama tamamen saydam

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null; // Bir sonraki kareye kadar bekle
        }
        fadeImage.color = endColor; // Tam a��ld���ndan emin ol
    }


    // --- De�i�meyen Di�er Fonksiyonlar ---
    private void UpdateUI()
    {
        float hours = Mathf.Floor(currentTime);
        float minutes = Mathf.Floor((currentTime - hours) * 60);
        clockText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        dayText.text = "G�n: " + dayNumber;
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
    // --- YEN� EKLENEN FONKS�YON ---
    public int GetDayNumber()
    {
        return dayNumber;
    }
    // --- YEN� FONKS�YONUN SONU ---
}