using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ClassicPlayerMovement : MonoBehaviour
{
    // --- TEMEL HAREKET DE���KENLER� ---
    [Header("Hareket Ayarlar�")]
    [SerializeField] private float walkingSpeed = 5.0f; // Y�r�me h�z� olarak de�i�tirdik
    [SerializeField] private float sprintSpeed = 8.0f;  // Ko�ma h�z� ekledik
    private float currentSpeed; // Anl�k olarak hangi h�z� kulland���m�z� tutacak

    // --- YER�EK�M� VE ZIPLAMA DE���KENLER� ---
    [Header("Z�plama ve Yer�ekimi Ayarlar�")]
    [SerializeField] private float jumpHeight = 1.5f;     // Ne kadar y�kse�e z�playaca�� (metre cinsinden)
    [SerializeField] private float gravityValue = -9.81f;
    private Vector3 playerVelocity;

    // --- E��LME (CROUCH) DE���KENLER� ---
    [Header("E�ilme Ayarlar�")]
    [SerializeField] private float crouchHeight = 0.9f;   // E�ilirken karakterin boyu
    [SerializeField] private float crouchSpeed = 2.0f;    // E�ilirkenki y�r�me h�z�
    private float standingHeight; // Karakterin normal, ayaktaki boyu

    // --- B�LE�EN REFERANSLARI ---
    private CharacterController controller;

    // Start fonksiyonu oyun ba�lad���nda bir kere �al���r.
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        // Oyun ba��nda karakterin normal boyunu kaydediyoruz ki aya�a kalkarken kullanabilelim.
        standingHeight = controller.height;
    }

    // Update fonksiyonu her bir karede bir kere �a�r�l�r.
    void Update()
    {
        // Karakterin yerde olup olmad���n� kontrol et
        bool isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -1.0f; // S�rekli artan yer�ekimini s�f�rla
        }

        // --- G�RD�LER� AL VE HIZI AYARLA ---
        HandleCrouch();
        HandleMovement();

        // --- HAREKET� UYGULA ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- ZIPLAMAYI KONTROL ET ---
        // "Jump" Unity'de varsay�lan olarak Space tu�una atanm��t�r.
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Fizik form�l�: Gerekli z�plama h�z�n� hesapla -> v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        // --- YER�EK�M�N� UYGULA ---
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // E�ilme mant���n� kontrol eden fonksiyon
    private void HandleCrouch()
    {
        // Sol CTRL tu�una bas�l� tutuluyorsa
        if (Input.GetKey(KeyCode.LeftControl))
        {
            controller.height = crouchHeight; // Karakterin boyunu k�salt
            currentSpeed = crouchSpeed;       // H�z�n� e�ilme h�z�na d���r
        }
        else // Bas�l� tutulmuyorsa
        {
            // �NEML� NOT: Burada normalde aya�a kalkmadan �nce �st�n�n bo� olup olmad���n�
            // kontrol eden bir kod (Raycast) gerekir. �imdilik basitle�tiriyoruz.
            controller.height = standingHeight; // Karakterin boyunu normale d�nd�r
        }
    }

    // Y�r�me ve Ko�ma mant���n� kontrol eden fonksiyon
    private void HandleMovement()
    {
        // E�er e�ilmiyorsak (boyumuz normal ise)
        if (controller.height == standingHeight)
        {
            // Sol Shift tu�una bas�l� tutuluyorsa ko�, yoksa y�r�
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = sprintSpeed; // H�z� ko�ma h�z�na ayarla
            }
            else
            {
                currentSpeed = walkingSpeed; // H�z� y�r�me h�z�na ayarla
            }
        }
    }
}