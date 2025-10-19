using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Butonlar için bu kütüphane gerekli

public class JournalController : MonoBehaviour
{
    [Header("UI Elementleri")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private TMP_InputField notesInputField;
    [SerializeField] private TextMeshProUGUI pageText; // Yeni ekledik
    [SerializeField] private Button nextButton;       // Yeni ekledik
    [SerializeField] private Button backButton;       // Yeni ekledik

    [Header("Oyuncu Referanslarý")]
    [SerializeField] private ClassicPlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private bool isJournalOpen = false;
    private List<string> pages = new List<string>(); // Artýk notlarý sayfa sayfa tutuyoruz
    private int currentPage = 0;

    private void Start()
    {
        // Not defteri baþladýðýnda içinde boþ bir sayfa olsun
        pages.Add("");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleJournal();
        }
    }

    private void ToggleJournal()
    {
        isJournalOpen = !isJournalOpen;
        journalPanel.SetActive(isJournalOpen);

        if (isJournalOpen)
        {
            playerMovement.enabled = false;
            mouseLook.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateJournalDisplay(); // Defter açýldýðýnda mevcut sayfayý göster
        }
        else
        {
            // Defter kapanmadan önce mevcut sayfadaki yazýyý kaydet
            pages[currentPage] = notesInputField.text;

            playerMovement.enabled = true;
            mouseLook.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void NextPage()
    {
        // Önce mevcut sayfayý kaydet
        pages[currentPage] = notesInputField.text;

        // Eðer son sayfadaysak, yeni bir boþ sayfa oluþtur
        if (currentPage == pages.Count - 1)
        {
            pages.Add("");
        }

        currentPage++;
        UpdateJournalDisplay();
    }

    public void PreviousPage()
    {
        // Önce mevcut sayfayý kaydet
        pages[currentPage] = notesInputField.text;

        currentPage--;
        UpdateJournalDisplay();
    }

    private void UpdateJournalDisplay()
    {
        // Input alanýna mevcut sayfanýn metnini yükle
        notesInputField.text = pages[currentPage];

        // Sayfa numarasýný güncelle (currentPage 0'dan baþladýðý için +1 ekliyoruz)
        pageText.text = "Sayfa: " + (currentPage + 1) + " / " + pages.Count;

        // Eðer ilk sayfadaysak geri tuþunu, son sayfadaysak ileri tuþunu yönet
        backButton.interactable = (currentPage > 0);
        // Ýleri tuþunun her zaman aktif olmasýný saðlayabiliriz, yeni sayfa oluþturduðu için.
        // nextButton.interactable = (currentPage < pages.Count - 1); 
    }
}