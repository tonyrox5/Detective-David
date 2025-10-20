using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Genel Ayarlar")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;

    [Header("Etkile�im Ayarlar�")]
    [SerializeField] private float interactionDistance = 3f;

    private float xRotation = 0f;
    private bool isInspecting = false;
    private Inspectable currentlyInspectedObject;
    private ClassicPlayerMovement playerMovementScript;

    void Start()
    {
        if (Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; }
        playerMovementScript = GetComponentInParent<ClassicPlayerMovement>();
    }

    // --- BU FONKS�YON G�NCELLEND� ---
    void Update()
    {
        // YEN� KONTROL: E�er diyalog aktifse, oyuncu hi�bir �ey yapamaz.
        // Bu, konu�ma s�ras�nda kameran�n d�nmesini veya ba�ka �eylerle etkile�ime girmesini engeller.
        if (DialogueUIManager.instance != null && DialogueUIManager.instance.IsDialogueActive())
        {
            return;
        }
        // --- YEN� KONTROL�N SONU ---

        if (isInspecting)
        {
            if (Input.GetKeyDown(KeyCode.E) && !currentlyInspectedObject.IsTransitioning()) { EndInspection(); }
        }
        else
        {
            HandleMouseLook();
            HandleInteraction();
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    // --- BU FONKS�YON G�NCELLEND� ---
    private void HandleInteraction()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, interactionDistance))
        {
            // --- YEN� EKLENEN B�L�M: D�YALOG KONTROL� ---
            // Bakt���m�z obje konu�ulabilir bir NPC mi? (Bu kontrol� en ba�a al�yoruz)
            if (hit.collider.TryGetComponent<DialogueTrigger>(out DialogueTrigger dialogueTrigger))
            {
                // E�er 'T' tu�una bas�l�rsa
                if (Input.GetKeyDown(KeyCode.T))
                {
                    // Diyalog y�neticisini �a��r ve konu�may� ba�lat.
                    DialogueUIManager.instance.StartDialogue(dialogueTrigger.startingConversation);
                }
            }
            // --- YEN� B�L�M�N SONU ---

            // Di�er etkile�imler eskisi gibi devam ediyor
            else
            {
                float currentTime = TimeManager.instance.GetCurrentTime();
                bool isNight = (currentTime >= 19 || currentTime < 7);
                GameObject heldItem = HandSystem.instance.GetHeldItem();

                if (heldItem == null) // EL�M�Z BO� �SE
                {
                    if (hit.collider.TryGetComponent<PlacementPoint>(out PlacementPoint pointToTakeFrom) && pointToTakeFrom.itemInSpot != null)
                    {
                        if (pointToTakeFrom.itemInSpot.TryGetComponent<Inspectable>(out Inspectable item))
                        {
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                if (isNight && item.isTakableAtNight)
                                {
                                    HandSystem.instance.AddItem(pointToTakeFrom.TakeItem());
                                }
                                else
                                {
                                    StartInspection(item);
                                }
                            }
                        }
                    }
                    else if (hit.collider.CompareTag("Bed"))
                    {
                        if (Input.GetKeyDown(KeyCode.E)) { TimeManager.instance.Sleep(); }
                    }
                    else if (hit.collider.TryGetComponent<DoorController>(out DoorController door))
                    {
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            door.Interact();
                        }
                    }
                }
                else // EL�M�Z DOLU �SE
                {
                    if (hit.collider.TryGetComponent<DoorController>(out DoorController lockedDoor) && lockedDoor.IsLocked())
                    {
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            string heldItemID = heldItem.GetComponent<Inspectable>().itemID;
                            if (heldItemID == lockedDoor.GetRequiredItemID())
                            {
                                lockedDoor.Unlock();
                                lockedDoor.Interact();
                            }
                            else
                            {
                                Debug.Log("Yanl�� anahtar.");
                            }
                        }
                    }
                    else if (hit.collider.TryGetComponent<PlacementPoint>(out PlacementPoint pointToPlaceTo) && pointToPlaceTo.itemInSpot == null)
                    {
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            if (pointToPlaceTo.CanAcceptItem(heldItem))
                            {
                                HandSystem.instance.RemoveHeldItem();
                                pointToPlaceTo.PlaceItem(heldItem);
                            }
                        }
                    }
                }
            }
        }
    }

    void StartInspection(Inspectable objectToInspect)
    {
        isInspecting = true;
        currentlyInspectedObject = objectToInspect;
        playerMovementScript.enabled = false;
        currentlyInspectedObject.StartInspection();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EndInspection()
    {
        isInspecting = false;
        playerMovementScript.enabled = true;
        currentlyInspectedObject.StopInspection();
        currentlyInspectedObject = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}