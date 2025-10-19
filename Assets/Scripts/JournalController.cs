using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Butonlar i�in bu k�t�phane gerekli

public class JournalController : MonoBehaviour
{
    [Header("UI Elementleri")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private TMP_InputField notesInputField;
    [SerializeField] private TextMeshProUGUI pageText; // Yeni ekledik
    [SerializeField] private Button nextButton;       // Yeni ekledik
    [SerializeField] private Button backButton;       // Yeni ekledik

    [Header("Oyuncu Referanslar�")]
    [SerializeField] private ClassicPlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private bool isJournalOpen = false;
    private List<string> pages = new List<string>(); // Art�k notlar� sayfa sayfa tutuyoruz
    private int currentPage = 0;

    private void Start()
    {
        // Not defteri ba�lad���nda i�inde bo� bir sayfa olsun
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

            UpdateJournalDisplay(); // Defter a��ld���nda mevcut sayfay� g�ster
        }
        else
        {
            // Defter kapanmadan �nce mevcut sayfadaki yaz�y� kaydet
            pages[currentPage] = notesInputField.text;

            playerMovement.enabled = true;
            mouseLook.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void NextPage()
    {
        // �nce mevcut sayfay� kaydet
        pages[currentPage] = notesInputField.text;

        // E�er son sayfadaysak, yeni bir bo� sayfa olu�tur
        if (currentPage == pages.Count - 1)
        {
            pages.Add("");
        }

        currentPage++;
        UpdateJournalDisplay();
    }

    public void PreviousPage()
    {
        // �nce mevcut sayfay� kaydet
        pages[currentPage] = notesInputField.text;

        currentPage--;
        UpdateJournalDisplay();
    }

    private void UpdateJournalDisplay()
    {
        // Input alan�na mevcut sayfan�n metnini y�kle
        notesInputField.text = pages[currentPage];

        // Sayfa numaras�n� g�ncelle (currentPage 0'dan ba�lad��� i�in +1 ekliyoruz)
        pageText.text = "Sayfa: " + (currentPage + 1) + " / " + pages.Count;

        // E�er ilk sayfadaysak geri tu�unu, son sayfadaysak ileri tu�unu y�net
        backButton.interactable = (currentPage > 0);
        // �leri tu�unun her zaman aktif olmas�n� sa�layabiliriz, yeni sayfa olu�turdu�u i�in.
        // nextButton.interactable = (currentPage < pages.Count - 1); 
    }
}