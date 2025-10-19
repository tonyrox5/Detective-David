using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ClassicPlayerMovement : MonoBehaviour
{
    // --- TEMEL HAREKET DEÐÝÞKENLERÝ ---
    [Header("Hareket Ayarlarý")]
    [SerializeField] private float walkingSpeed = 5.0f; // Yürüme hýzý olarak deðiþtirdik
    [SerializeField] private float sprintSpeed = 8.0f;  // Koþma hýzý ekledik
    private float currentSpeed; // Anlýk olarak hangi hýzý kullandýðýmýzý tutacak

    // --- YERÇEKÝMÝ VE ZIPLAMA DEÐÝÞKENLERÝ ---
    [Header("Zýplama ve Yerçekimi Ayarlarý")]
    [SerializeField] private float jumpHeight = 1.5f;     // Ne kadar yükseðe zýplayacaðý (metre cinsinden)
    [SerializeField] private float gravityValue = -9.81f;
    private Vector3 playerVelocity;

    // --- EÐÝLME (CROUCH) DEÐÝÞKENLERÝ ---
    [Header("Eðilme Ayarlarý")]
    [SerializeField] private float crouchHeight = 0.9f;   // Eðilirken karakterin boyu
    [SerializeField] private float crouchSpeed = 2.0f;    // Eðilirkenki yürüme hýzý
    private float standingHeight; // Karakterin normal, ayaktaki boyu

    // --- BÝLEÞEN REFERANSLARI ---
    private CharacterController controller;

    // Start fonksiyonu oyun baþladýðýnda bir kere çalýþýr.
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        // Oyun baþýnda karakterin normal boyunu kaydediyoruz ki ayaða kalkarken kullanabilelim.
        standingHeight = controller.height;
    }

    // Update fonksiyonu her bir karede bir kere çaðrýlýr.
    void Update()
    {
        // Karakterin yerde olup olmadýðýný kontrol et
        bool isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -1.0f; // Sürekli artan yerçekimini sýfýrla
        }

        // --- GÝRDÝLERÝ AL VE HIZI AYARLA ---
        HandleCrouch();
        HandleMovement();

        // --- HAREKETÝ UYGULA ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- ZIPLAMAYI KONTROL ET ---
        // "Jump" Unity'de varsayýlan olarak Space tuþuna atanmýþtýr.
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Fizik formülü: Gerekli zýplama hýzýný hesapla -> v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        // --- YERÇEKÝMÝNÝ UYGULA ---
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // Eðilme mantýðýný kontrol eden fonksiyon
    private void HandleCrouch()
    {
        // Sol CTRL tuþuna basýlý tutuluyorsa
        if (Input.GetKey(KeyCode.LeftControl))
        {
            controller.height = crouchHeight; // Karakterin boyunu kýsalt
            currentSpeed = crouchSpeed;       // Hýzýný eðilme hýzýna düþür
        }
        else // Basýlý tutulmuyorsa
        {
            // ÖNEMLÝ NOT: Burada normalde ayaða kalkmadan önce üstünün boþ olup olmadýðýný
            // kontrol eden bir kod (Raycast) gerekir. Þimdilik basitleþtiriyoruz.
            controller.height = standingHeight; // Karakterin boyunu normale döndür
        }
    }

    // Yürüme ve Koþma mantýðýný kontrol eden fonksiyon
    private void HandleMovement()
    {
        // Eðer eðilmiyorsak (boyumuz normal ise)
        if (controller.height == standingHeight)
        {
            // Sol Shift tuþuna basýlý tutuluyorsa koþ, yoksa yürü
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = sprintSpeed; // Hýzý koþma hýzýna ayarla
            }
            else
            {
                currentSpeed = walkingSpeed; // Hýzý yürüme hýzýna ayarla
            }
        }
    }
}